using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using LittleCompany.Config;
using LittleCompany.helper;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System.Collections;

using LittleCompany.modifications;
using static LittleCompany.helper.LayerMasks;
using static LittleCompany.modifications.Modification;
using LittleCompany.dependency;
using HarmonyLib;

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
    public class ShrinkRay : GrabbableObject
    {
        #region Properties
        internal static readonly string OverheatedHeaderText = "Overheated ShrinkRay";
        internal static readonly string OverheatedSubText = "Became unusable and can't be recharged.";

        internal static string BaseAssetPath = Path.Combine(AssetLoader.BaseAssetPath, "Shrink");

        private const float beamSearchDistance = 10f;

        public static GameObject networkPrefab { get; set; }

        internal AudioSource audioSource;

        internal static AudioClip grabSFX;
        internal static AudioClip dropSFX;

        internal static AudioClip loadSFX;
        internal static AudioClip unloadSFX;
        internal static AudioClip noTargetSFX;

        internal bool LaserEnabled = false;
        internal Light LaserLight = null;
        internal LineRenderer LaserLine = null;
        internal Light LaserDot = null;

        internal GameObject targetObject = null;
        internal List<Material> targetMaterials = new List<Material>();

        internal GameObject burningEffect = null;
        internal bool IsBurning => burningEffect != null;

        internal int ShotsLeft => Mathf.RoundToInt(ModConfig.Instance.values.shrinkRayShotsPerCharge * insertedBattery.charge);

        internal bool EmptyBattery => RequiresBattery && (insertedBattery.empty || ShotsLeft == 0);
        internal static bool RequiresBattery => ModConfig.Instance.values.shrinkRayShotsPerCharge > 0;
        internal int initialBattery = 100;

        internal NetworkVariable<ModificationType> currentModificationType = new NetworkVariable<ModificationType>(ModificationType.Shrinking);
        internal NetworkVariable<Mode> currentMode = new NetworkVariable<Mode>(Mode.Default);
        internal NetworkVariable<bool> isOverheated = new NetworkVariable<bool>(false);
        internal float timeSinceDefaultMode = 0f;
        internal enum Mode
        {
            Default,
            Loading,
            Shooting,
            Unloading,
            Missing
        }
        #endregion

        #region Networking
        public static void LoadAsset()
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = AssetLoader.littleCompanyAsset?.LoadAsset<Item>(Path.Combine(BaseAssetPath, "ShrinkRayItem.asset"));
            if(assetItem == null )
            {
                Plugin.Log("ShrinkRayItem.asset not found!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;
            ScrapManagementFacade.FixMixerGroups(networkPrefab);
            assetItem.creditsWorth = ModConfig.Instance.values.shrinkRayCost;
            assetItem.weight = 1.05f;
            assetItem.canBeGrabbedBeforeGameStart = true;
            networkPrefab.transform.localScale = new Vector3(1f, 1f, 1f);

            ShrinkRay shrinkRay = networkPrefab.AddComponent<ShrinkRay>();
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayGrab.wav", (item) => grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayDrop.wav", (item) => dropSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayLoad.wav", (item) => loadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayUnload.wav", (item) => unloadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayNoTarget.wav", (item) => noTargetSFX = item));

            // Add the FX component for controlling beam fx
            ShrinkRayFX shrinkRayFX = networkPrefab.AddComponent<ShrinkRayFX>();
            // Customize the ShrinkRayFX (I just found some good settings by tweaking in game. Easier done here than in the prefab, which is why I made properties on the script)
            shrinkRayFX.noiseSpeed = 5;
            shrinkRayFX.noisePower = 0.1f;
            shrinkRayFX.sparksSize = 1f;
            shrinkRayFX.thickness = 0.1f;

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayBeam.wav", (item) => ShrinkRayFX.beamSFX = item));

            Destroy(networkPrefab.GetComponent<PhysicsProp>()); // todo: make this not needed

            shrinkRay.itemProperties = assetItem;
            shrinkRay.itemProperties.toolTips = ["Shrink: LMB", "Enlarge: MMB"];
            shrinkRay.itemProperties.minValue = 0;
            shrinkRay.itemProperties.maxValue = 0;
            shrinkRay.itemProperties.saveItemVariable = true;
            shrinkRay.grabbable = true;
            shrinkRay.grabbableToEnemies = true;
            shrinkRay.fallTime = 0f;

            if (RequiresBattery)
            {
                shrinkRay.itemProperties.requiresBattery = true;
                shrinkRay.itemProperties.batteryUsage = ModConfig.Instance.values.shrinkRayShotsPerCharge * ShrinkRayFX.DefaultBeamDuration;
                shrinkRay.insertedBattery = new Battery(false, 1f);
            }
            else
                shrinkRay.itemProperties.requiresBattery = false;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            ScrapManagementFacade.RegisterItem(shrinkRay.itemProperties, false, true, -1, shrinkRay.name + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!");
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            //insertedBattery = new Battery(isEmpty: false, 1f);
            LaserLight = transform.Find("LaserLight")?.GetComponent<Light>();
            LaserDot = transform.Find("LaserDot")?.GetComponent<Light>();
            LaserLine = transform.Find("LaserLine")?.GetComponent<LineRenderer>();

            if (!TryGetComponent(out audioSource)) // fallback that likely won't happen nowadays
            {
                Plugin.Log("AudioSource of " + gameObject.name + " was null. Adding a new one..", Plugin.LogType.Error);
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (itemProperties.requiresBattery && PlayerInfo.IsHost)
                SyncBatteryServerRpc(initialBattery);

            if (isOverheated.Value)
            {
                AddBurningEffect();
                Overheat();
            }
            else
                EnableLaserForHolder();
        }

        public override int GetItemDataToSave()
        {
            base.GetItemDataToSave();
            return RequiresBattery ? ShotsLeft : -1;
        }

        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            if(RequiresBattery && saveData != -1) // -1 = previously no battery required. initialBattery default of 100 counts
            {
                initialBattery = (int)Mathf.Min(100f / ModConfig.Instance.values.shrinkRayShotsPerCharge * saveData, 100f);
                if (PlayerInfo.IsHost && ModConfig.Instance.values.shrinkRayNoRecharge && saveData == 0)
                    isOverheated.Value = true;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (currentMode.Value != Mode.Default || EmptyBattery || isOverheated.Value) return;

            base.ItemActivate(used, buttonDown);
            SwitchModificationTypeServerRpc((int)ModificationType.Shrinking);
            SwitchModeServerRpc((int)Mode.Loading);
        }

        public override void Update()
        {
            if (StartOfRound.Instance == null) return;
			
            base.Update();

            // Mode fallback
            if (PlayerInfo.IsHost && currentMode.Value != Mode.Default)
            {
                timeSinceDefaultMode += Time.deltaTime;

                if (timeSinceDefaultMode > ShrinkRayFX.DefaultBeamDuration * 3)
                {
                    // Likely an error occured. Reset to default
                    timeSinceDefaultMode = 0f;
                    SwitchModeServerRpc((int)Mode.Default);
                }
            }

            if (LaserEnabled)
                UpdateLaser();

            if (!isHeld || playerHeldBy != PlayerInfo.CurrentPlayer || isPocketed || currentMode.Value != Mode.Default || EmptyBattery || isOverheated.Value)
                return;

            if (Mouse.current.middleButton.wasPressedThisFrame) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
            {
                SwitchModificationTypeServerRpc((int)ModificationType.Enlarging);
                SwitchModeServerRpc((int)Mode.Loading);
            }
        }

        public override void EquipItem()
        {
            Plugin.Log("ShrinkRay.EquipItem");
            if (IsOwner && grabSFX != null && audioSource != null)
                    audioSource.PlayOneShot(grabSFX);

            base.EquipItem();

            EnableLaserForHolder();
        }

        public override void PocketItem()
        {
            DisableLaserForHolder();
			if (targetObject != null)
                ChangeTarget(null);

            base.PocketItem();
        }

        public override void DiscardItem()
        {
            if (IsOwner && dropSFX != null && audioSource != null)
                audioSource.PlayOneShot(dropSFX);

            DisableLaserForHolder();
            if(targetObject != null)
                ChangeTarget(null);

            base.DiscardItem();
        }

        public override void GrabItem()
        {
            Plugin.Log("ShrinkRay.GrabItem. Is Owner: " + IsOwner);
            EnableLaserForHolder();
            base.GrabItem();
        }

        public override void ChargeBatteries()
        {
            base.ChargeBatteries();
            Plugin.Log("ChargeBatteries");
            EnableLaserForHolder();
        }
        #endregion

        #region Mode Control
        [ServerRpc(RequireOwnership = false)]
        internal void SwitchModificationTypeServerRpc(int newType)
        {
            Plugin.Log("ShrinkRay modificationType switched to " + (ModificationType)newType);
            currentModificationType.Value = (ModificationType)newType;
        }

        [ServerRpc(RequireOwnership = false)]
        internal void SwitchModeServerRpc(int newMode)
        {
            if(RequiresBattery)
                SyncBatteryServerRpc((int)(100f / ModConfig.Instance.values.shrinkRayShotsPerCharge * ShotsLeft));

            currentMode.Value = (Mode)newMode;

            SwitchModeClientRpc(newMode);
        }

        [ClientRpc]
        internal void SwitchModeClientRpc(int newMode)
        {
            Plugin.Log("ShrinkRay mode switched to " + (Mode)newMode);
            switch ((Mode)newMode)
            {
                case Mode.Loading:
                    StartCoroutine(LoadRay());
                    break;
                case Mode.Shooting:
                    isBeingUsed = true;
                    ShootRayBeam();
                    break;
                case Mode.Unloading:
                    isBeingUsed = false;
                    StartCoroutine(UnloadRay());
                    break;
                case Mode.Missing:
                    StartCoroutine(UnloadRay(false));
                    break;
                default: break;
            }
        }
        #endregion

        #region Targeting

        internal static bool TryGetObjectByNetworkID(ulong networkID, out GameObject gameObject)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(networkID))
            {
                gameObject = null;
                return false;
            }

            gameObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkID]?.gameObject;
            return gameObject != null;
        }

        internal bool HasLaserComponents => LaserLine != null && LaserDot != null && LaserLight != null;
        internal bool IsHolder => IsOwner && playerHeldBy == PlayerInfo.CurrentPlayer;
        internal bool CanEnableLaser => IsHolder && !EmptyBattery && !isOverheated.Value && !isPocketed;

        internal void EnableLaserForHolder(bool enable = true)
        {
            if (!HasLaserComponents) return;

            if (!CanEnableLaser)
                enable = false;

            LaserEnabled = enable;
            LaserLine.enabled = enable;
            LaserLight.enabled = enable;
            LaserDot.enabled = enable;
            if (!enable && ((IsHolder && currentMode.Value == Mode.Default) || !isHeld))
                ChangeTarget(null);
        }
        internal void DisableLaserForHolder() => EnableLaserForHolder(false);

        internal void UpdateLaser()
        {
            if(!LaserEnabled) return;

            if (isPocketed || playerHeldBy != PlayerInfo.CurrentPlayer) // Fallback -> todo: find main reason why laser is still active sometimes
            {
                DisableLaserForHolder();
                return;
            }

            if (currentMode.Value == Mode.Loading && targetObject != null)
            {
                var distance = Vector3.Distance(transform.position, targetObject.transform.position);
                if(distance < beamSearchDistance)
                {
                    var targetDirection = LaserLight.transform.InverseTransformPoint(targetObject.transform.position);
                    LaserLine.SetPosition(1, targetDirection);
                    return;
                }
            }

            var startPoint = LaserLight.transform.position;
            var direction = LaserLight.transform.forward;
            var endPoint = Vector3.zero;

            if (Physics.Raycast(startPoint, direction, out RaycastHit hit, beamSearchDistance, LayerMask))
            {
                var distance = Vector3.Distance(hit.point, startPoint);
                endPoint.z = distance;
                if (LaserDot != null)
                {
                    LaserDot.spotAngle = 1.5f;
                    if (distance < 3f)
                        LaserDot.spotAngle += (3f - distance) / 2;
                    LaserDot.innerSpotAngle = LaserDot.spotAngle / 3;
                }

                if (targetObject == null || targetObject != hit.collider.gameObject)
                    ChangeTarget(hit.collider.gameObject);
            }
            else
            {
                endPoint.z = 10f;
                LaserDot.spotAngle = 0f;
                LaserDot.innerSpotAngle = 0f;

                ChangeTarget(null);
            }
            LaserLine.SetPosition(1, endPoint);
        }

        internal int LayerMask
        {
            get
            {
                var layerMasks = new List<Mask>() { Mask.Player };
                if (ModConfig.Instance.values.itemSizeChangeStep > Mathf.Epsilon)
                    layerMasks.Add(Mask.Props);
                if (ModConfig.Instance.values.enemySizeChangeStep > Mathf.Epsilon)
                    layerMasks.Add(Mask.Enemies);
                if (ModConfig.Instance.values.shipObjectSizeChangeStep > Mathf.Epsilon)
                    layerMasks.Add(Mask.PlaceableShipObjects);
                return ToInt(layerMasks.ToArray());
            }
        }


        //private string oldTargetObject = ""; // DEBUG
        public void ChangeTarget(GameObject newTarget)
        {
            /*if (newTarget != null && newTarget.name != oldTargetObject)
            {
                oldTargetObject = newTarget.name;
                Plugin.Log("New target: " + newTarget.name + " [layer " + newTarget.layer + "]");
            }*/

            var identifiedTarget = IdentifyTarget(newTarget);
            if (identifiedTarget == targetObject) return;
#if DEBUG
            if (identifiedTarget != null)
                Plugin.Log("New target: " + identifiedTarget.name + " [layer " + identifiedTarget.layer + "]");
            else
                Plugin.Log("Lost track of target.");
#endif
            if (targetObject != null && targetObject.TryGetComponent(out TargetCircle circle))
                Destroy(circle);

            targetObject = identifiedTarget; // Change target object

            if (targetObject != null)
                targetObject.AddComponent<TargetCircle>();
        }

        public GameObject IdentifyTarget(GameObject target)
        {
            if (target == null) return null;

            //Plugin.Log("Target to identify: " + target.name + " [layer " + target.layer + "]");

            switch ((Mask)target.layer)
            {
                case Mask.Player:
                    var targetPlayer = target.GetComponentInParent<PlayerControllerB>();
                    if (targetPlayer != null && targetPlayer.playerClientId != PlayerInfo.CurrentPlayerID && !PlayerModification.IsGettingScaled(targetPlayer))
                        return targetPlayer.gameObject;
                    break;

                case Mask.Props:
                    var targetPlayerObject = target.GetComponentInParent<GrabbablePlayerObject>();
                    if (targetPlayerObject != null)
                        return targetPlayerObject.grabbedPlayer.gameObject;

                    var targetObject = target.GetComponentInParent<GrabbableObject>();
                    if (targetObject != null && !ObjectModification.UnscalableObjects.Contains(targetObject.itemProperties.itemName))
                        return targetObject.gameObject;
                    break;

                case Mask.Enemies: case Mask.EnemiesNotRendered:
                    var targetEnemy = target.GetComponentInParent<EnemyAI>();
                    if (targetEnemy != null)
                        return targetEnemy.gameObject;
                    break;

                case Mask.PlaceableShipObjects:
                    var placeableShipObject = target.GetComponentInParent<PlaceableShipObject>();
                    if (placeableShipObject != null)
                        return placeableShipObject.parentObject.gameObject;
                    break;
            }
            return null;
        }
        #endregion

        #region Shooting
        private IEnumerator LoadRay()
        {
            Plugin.Log("LoadRay");
            if (loadSFX != null && audioSource != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(loadSFX);
                yield return new WaitWhile(() => audioSource.isPlaying);
            }
            else
                Plugin.Log("AudioSource or AudioClip for loading ShrinkRay was null!", Plugin.LogType.Warning);

            if (IsOwner)
                ShootRayOnClient();
        }

        private IEnumerator UnloadRay(bool hasHitTarget = true)
        {
            Plugin.Log("UnloadRay");

            if (IsBurning)
            {
                if (hasHitTarget)
                {
                    Plugin.Log("ShrinkRay went BOOM ._.'");
                    Landmine.SpawnExplosion(transform.position, true, 0.25f, 1f, 20);
                    Overheat();
                    if (playerHeldBy != null && playerHeldBy == PlayerInfo.CurrentPlayer)
                        playerHeldBy.DiscardHeldObject();
                }
                else
                    DestroyImmediate(burningEffect); // Ray beam failed, remove burning effect
            }

            if (unloadSFX != null && audioSource != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(hasHitTarget ? unloadSFX : noTargetSFX);
                yield return new WaitWhile(() => audioSource.isPlaying);
            }
            else
                Plugin.Log("AudioSource or AudioClip for unloading ShrinkRay was null!", Plugin.LogType.Warning);

            yield return new WaitForSeconds(useCooldown);

            EnableLaserForHolder();

            if (PlayerInfo.IsHost)
                currentMode.Value = Mode.Default;
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRayOnClient()
        {
            if (playerHeldBy == null || targetObject.GetComponent<NetworkObject>() == null || playerHeldBy.isClimbingLadder)
            {
                Plugin.Log("ShootRayOnClient: Missing");
                SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Shooting ray gun!");

            bool rayHasHit = ShootRayOnClientAtTarget();

            if (rayHasHit)
                DisableLaserForHolder();
            else
            {
                Plugin.Log("ShootRayOnClient: Missing as no ray hit");
                SwitchModeServerRpc((int)Mode.Missing);
            }
        }
        
        private bool ShootRayOnClientAtTarget()
        {
            var scalingMultiplier = ObjectModification.ScalingOf(this).RelativeScale;
            switch (TargetMask)
            {
                case Mask.Player:
                    {
                        if (!targetObject.TryGetComponent(out PlayerControllerB targetPlayer))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit a player -> " + targetPlayer.name);
                        if (targetPlayer.playerClientId == playerHeldBy.playerClientId || !PlayerModification.CanApplyModificationTo(targetPlayer, currentModificationType.Value, playerHeldBy, scalingMultiplier))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnPlayerModificationServerRpc(targetPlayer.playerClientId, playerHeldBy.playerClientId);
                        return true;
                    }

                case Mask.Props:
                    {
                        if (!targetObject.TryGetComponent(out GrabbableObject item))
                            return false;

                        if (item is GrabbablePlayerObject)
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an item -> " + item.name);

                        if (!ObjectModification.CanApplyModificationTo(item, currentModificationType.Value, playerHeldBy, scalingMultiplier))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnObjectModificationServerRpc(item.NetworkObjectId, playerHeldBy.playerClientId);
                        return true;
                    }

                case Mask.Enemies:
                    {
                        if (!targetObject.TryGetComponent(out EnemyAI enemyAI))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an enemy -> " + enemyAI.enemyType.name);

                        if (!EnemyModification.CanApplyModificationTo(enemyAI, currentModificationType.Value, playerHeldBy, scalingMultiplier))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnEnemyModificationServerRpc(enemyAI.NetworkObjectId, playerHeldBy.playerClientId);
                        return true;
                    }

                default:
                    var shipObject = targetObject.GetComponentInChildren<PlaceableShipObject>();
                    if (shipObject != null)
                    {
                        if (IsOwner)
                            Plugin.Log("Ray has hit a ship object -> " + shipObject.name);

                        if (!ShipObjectModification.CanApplyModificationTo(shipObject, currentModificationType.Value, playerHeldBy, scalingMultiplier))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        if (!targetObject.TryGetComponent(out NetworkObject networkObject))
                        {
                            if (IsOwner)
                                Plugin.Log("But it has no network object.");
                            return false;
                        }

                        OnShipObjectModificationServerRpc(networkObject.NetworkObjectId, playerHeldBy.playerClientId);
                        return true;
                    }

                    if (IsOwner)
                        Plugin.Log("Ray has hit an unhandled object named \"" + targetObject.name + "\" [Layer " + targetObject.layer + "]");
                    return false;
            };
        }

        internal Mask TargetMask
        {
            get
            {
                if (targetObject == null) return Mask.Default;

                var layerMask = (Mask)targetObject.layer;
                if (layerMask != Mask.Default) return layerMask;

                if (targetObject.TryGetComponent(out EnemyAI _))
                    return Mask.Enemies;

                if (targetObject.TryGetComponent(out GrabbableObject _))
                    return targetObject.TryGetComponent(out GrabbablePlayerObject _) ? Mask.Player : Mask.Props;

                if (targetObject.TryGetComponent(out Item _))
                    return Mask.InteractableObject;

                if (targetObject.TryGetComponent(out PlayerControllerB _))
                    return Mask.Player;

                if (targetObject.GetComponentInChildren<PlaceableShipObject>() != null)
                    return Mask.PlaceableShipObjects;

                return Mask.Default;
            }
        }

        internal void ShootRayBeam()
        {
            if (playerHeldBy == null || targetObject == null || !transform.TryGetComponent(out ShrinkRayFX shrinkRayFX) || shrinkRayFX == null)
            {
                string reason;
                if (playerHeldBy == null) reason = "Not held by a player.";
                else if (targetObject == null) reason = "No target found.";
                else reason = "Unable to create ray beam visuals.";

                Plugin.Log("Unable to shoot ray beam. Reason: " + reason, Plugin.LogType.Error);
                SwitchModeClientRpc((int)Mode.Unloading);
                return;
            }

            if (ModConfig.Instance.values.shrinkRayNoRecharge && RequiresBattery)
            {
                var singleShot = 1f / ModConfig.Instance.values.shrinkRayShotsPerCharge;
                if (insertedBattery.charge < (singleShot * 1.5f))
                    AddBurningEffect();
            }

            shrinkRayFX.RenderRayBeam(playerHeldBy.gameplayCamera.transform, targetObject.transform, currentModificationType.Value, audioSource, () =>
            {
                // Complete
                Plugin.Log("Ray beam has finished.");
                SwitchModeClientRpc((int)Mode.Unloading);
            }, () =>
            {
                // Failure
                Plugin.Log("Ray beam failed.");
                SwitchModeClientRpc((int)Mode.Missing);
            });
        }

        internal void AddBurningEffect()
        {
            if (IsBurning) return;

            burningEffect = Effects.BurningEffect;
            var relativeScale = ObjectModification.ScalingOf(this).RelativeScale;
            burningEffect.transform.localScale = Vector3.one * 0.2f * relativeScale;
            burningEffect.transform.position = transform.position;
            burningEffect.transform.SetParent(transform, true);
            burningEffect.transform.localPosition = new Vector3(0f, 0.2f, 0.3f);
        }

        internal void Overheat()
        {
            // Materials
            Materials.ReplaceAllMaterialsWith(gameObject, (mat) => Materials.BurntMaterial);

            // Scan node
            var scanNode = GetComponentInChildren<ScanNodeProperties>();
            if (scanNode != null)
            {
                scanNode.headerText = OverheatedHeaderText;
                scanNode.subText = OverheatedSubText;
            }

            if (PlayerInfo.IsHost)
                isOverheated.Value = true;
        }
        #endregion

        #region PlayerTargeting
        // ------ Ray hitting Player ------
        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerModificationServerRpc(ulong targetPlayerID, ulong playerHeldByID)
        {
            OnPlayerModificationClientRpc(targetPlayerID, playerHeldByID);
        }

        [ClientRpc]
        public void OnPlayerModificationClientRpc(ulong targetPlayerID, ulong playerHeldByID)
        {
            Plugin.Log("OnPlayerModificationClientRpc");
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            var targetPlayer = PlayerInfo.ControllerFromID(targetPlayerID);
            if (targetPlayer?.gameObject?.transform == null)
            {
                Plugin.Log("Ay.. that's not a valid player somehow..");
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            targetObject = targetPlayer.gameObject;

            // For other clients
            var targetingUs = targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID;

            Plugin.Log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");
            PlayerModification.ApplyModificationTo(targetPlayer, currentModificationType.Value, playerHeldBy, ObjectModification.ScalingOf(this).RelativeScale, () =>
            {
                Plugin.Log("Finished player modification with type: " + currentModificationType.Value.ToString());
            });

            if(IsOwner)
                SwitchModeServerRpc((int)Mode.Shooting);
        }
        #endregion

        #region ObjectTargeting
        // ------ Ray hitting Object ------
        [ServerRpc(RequireOwnership = false)]
        public void OnObjectModificationServerRpc(ulong targetObjectNetworkID, ulong playerHeldByID)
        {
            OnObjectModificationClientRpc(targetObjectNetworkID, playerHeldByID);
        }

        [ClientRpc]
        public void OnObjectModificationClientRpc(ulong targetObjectNetworkID, ulong playerHeldByID)
        {
            Plugin.Log("OnObjectModificationClientRpc");
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            if (!TryGetObjectByNetworkID(targetObjectNetworkID, out targetObject))
            {
                Plugin.Log("OnObjectModification: Object not found", Plugin.LogType.Error);
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Ray has hit " + targetObject.name + "!");
            ObjectModification.ApplyModificationTo(targetObject.GetComponentInParent<GrabbableObject>(), currentModificationType.Value, playerHeldBy, ObjectModification.ScalingOf(this).RelativeScale, () =>
            {
                Plugin.Log("Finished object modification with type: " + currentModificationType.Value.ToString());
            });

            if (IsOwner)
                SwitchModeServerRpc((int)Mode.Shooting);
        }
        #endregion

        #region ShipObjectTargeting
        // ------ Ray hitting Object ------
        [ServerRpc(RequireOwnership = false)]
        public void OnShipObjectModificationServerRpc(ulong targetObjectNetworkID, ulong playerHeldByID)
        {
            OnShipObjectModificationClientRpc(targetObjectNetworkID, playerHeldByID);
        }

        [ClientRpc]
        public void OnShipObjectModificationClientRpc(ulong targetObjectNetworkID, ulong playerHeldByID)
        {
            Plugin.Log("OnShipObjectModificationClientRpc");
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            if (!TryGetObjectByNetworkID(targetObjectNetworkID, out targetObject))
            {
                Plugin.Log("OnShipObjectModification: Object not found", Plugin.LogType.Error);
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Ray has hit " + targetObject.name + "!");
            PlaceableShipObject placeableShipObject = targetObject.GetComponentInChildren<PlaceableShipObject>();
            ShipObjectModification.ApplyModificationTo(placeableShipObject, currentModificationType.Value, playerHeldBy, ObjectModification.ScalingOf(this).RelativeScale, () =>
            {
                Plugin.Log("Finished object modification with type: " + currentModificationType.Value.ToString());
            });

            if (IsOwner)
                SwitchModeServerRpc((int)Mode.Shooting);
        }
        #endregion

        #region EnemyTargeting
        // ------ Ray hitting Enemy ------
        [ServerRpc(RequireOwnership = false)]
        public void OnEnemyModificationServerRpc(ulong targetEnemyNetworkID, ulong playerHeldByID)
        {
            OnEnemyModificationClientRpc(targetEnemyNetworkID, playerHeldByID);
        }

        [ClientRpc]
        public void OnEnemyModificationClientRpc(ulong targetEnemyNetworkID, ulong playerHeldByID)
        {
            Plugin.Log("OnEnemyModificationClientRpc");
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            if (!TryGetObjectByNetworkID(targetEnemyNetworkID, out targetObject))
            {
                Plugin.Log("OnEnemyModification: Enemy not found", Plugin.LogType.Error);
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Ray has hit " + targetObject.name + "!");
            EnemyModification.ApplyModificationTo(targetObject.GetComponentInParent<EnemyAI>(), currentModificationType.Value, playerHeldBy, ObjectModification.ScalingOf(this).RelativeScale,() =>
            {
                Plugin.Log("Finished enemy modification with type: " + currentModificationType.Value.ToString());
            });

            if (IsOwner)
                SwitchModeServerRpc((int)Mode.Shooting);
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(ItemCharger), "Update")]
        [HarmonyPostfix]
        public static void ItemChargerUpdate(ItemCharger __instance)
        {
            if (!__instance.triggerScript.interactable || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer == null) return;

            var shrinkRay = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer as ShrinkRay;
            if(shrinkRay == null) return;

            __instance.triggerScript.interactable = !shrinkRay.isOverheated.Value;
        }
        #endregion
    }
}
