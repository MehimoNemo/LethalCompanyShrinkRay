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

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
    public class ShrinkRay : GrabbableObject
    {
        #region Properties
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

        internal NetworkVariable<ModificationType> currentModificationType = new NetworkVariable<ModificationType>(ModificationType.Shrinking);
        internal NetworkVariable<Mode> currentMode = new NetworkVariable<Mode>(Mode.Default);
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
            shrinkRay.grabbable = true;
            shrinkRay.grabbableToEnemies = true;
            shrinkRay.fallTime = 0f;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            ScrapManagementFacade.RegisterItem(shrinkRay.itemProperties, false, true, -1, shrinkRay.name + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!");
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            LaserLight = transform.Find("LaserLight")?.GetComponent<Light>();
            LaserDot = transform.Find("LaserDot")?.GetComponent<Light>();
            LaserLine = transform.Find("LaserLine")?.GetComponent<LineRenderer>();

            if (!TryGetComponent(out audioSource)) // fallback that likely won't happen nowadays
            {
                Plugin.Log("AudioSource of " + gameObject.name + " was null. Adding a new one..", Plugin.LogType.Error);
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            DisableLaserForHolder();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (currentMode.Value != Mode.Default) return;

            Plugin.Log("Triggering " + name);
            base.ItemActivate(used, buttonDown);
            SwitchModificationTypeServerRpc((int)ModificationType.Shrinking);
            SwitchModeServerRpc((int)Mode.Loading);
        }

        public override void Update()
        {
            base.Update();

            if (LaserEnabled)
                UpdateLaser();

            if (isPocketed || currentMode.Value != Mode.Default)
                return;

            if (Mouse.current.middleButton.wasPressedThisFrame && IsOwner) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
            {
                SwitchModificationTypeServerRpc((int)ModificationType.Enlarging);
                SwitchModeServerRpc((int)Mode.Loading);
            }
        }

        public override void EquipItem()
        {
            if (IsOwner)
            {
                EnableLaserForHolder();
                if(grabSFX != null && audioSource != null)
                    audioSource.PlayOneShot(grabSFX);
            }

            base.EquipItem();
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
            Plugin.Log("IsOwner of ShrinkRay: " + IsOwner);
            EnableLaserForHolder();
            base.GrabItem();
        }
        #endregion

        #region Mode Control
        [ServerRpc(RequireOwnership = false)]
        internal void SwitchModificationTypeServerRpc(int newType)
        {
            Plugin.Log(currentModificationType.ToString());
            Plugin.Log(currentModificationType.Value.ToString());
            Plugin.Log("ShrinkRay modificationType switched to " + (ModificationType)newType);
            currentModificationType.Value = (ModificationType)newType;
        }

        [ServerRpc(RequireOwnership = false)]
        internal void SwitchModeServerRpc(int newMode)
        {
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
                    ShootRayBeam();
                    break;
                case Mode.Unloading:
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

        internal void EnableLaserForHolder(bool enable = true)
        {
            if (!IsOwner || LaserLine == null || LaserDot == null || LaserLight == null || playerHeldBy == null)
                enable = false;

            LaserEnabled = enable;
            if (LaserLine != null) LaserLine.enabled = enable;
            if (LaserLight != null) LaserLight.enabled = enable;
            if (LaserDot != null) LaserDot.enabled = enable;
        }
        internal void DisableLaserForHolder() => EnableLaserForHolder(false);

        internal void UpdateLaser()
        {
            if(LaserEnabled && isPocketed) // Fallback -> todo: find main reason why laser is still active sometimes when pocketed
            {
                DisableLaserForHolder();
                return;
            }

            if(!LaserEnabled || ModConfig.Instance.values.shrinkRayTargetHighlighting == ModConfig.ShrinkRayTargetHighlighting.Off) return;

            if(currentMode.Value != Mode.Loading && ModConfig.Instance.values.shrinkRayTargetHighlighting == ModConfig.ShrinkRayTargetHighlighting.OnLoading) return;

            if(currentMode.Value == Mode.Loading && targetObject != null)
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

            //var layerMask = ToInt([Mask.Player, Mask.Props, Mask.InteractableObject, Mask.Enemies, Mask.EnemiesNotRendered]);
            var layerMask = ToInt([Mask.Player, Mask.Props, Mask.Enemies]);
            if (Physics.Raycast(startPoint, direction, out RaycastHit hit, beamSearchDistance, layerMask))
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

        public void ChangeTarget(GameObject newTarget)
        {
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

            if(targetObject != null)
                targetObject.AddComponent<TargetCircle>();
        }

        public GameObject IdentifyTarget(GameObject target)
        {
            if (target == null) return null;

            //Plugin.Log("Target to identify: " + target.name + " [layer " + target.layer + "]");

            switch((Mask)target.layer)
            {
                case Mask.Player: /*case Mask.DecalStickableSurface:*/
                    var targetPlayer = target.GetComponentInParent<PlayerControllerB>();
                    if (targetPlayer != null && targetPlayer.playerClientId != PlayerInfo.CurrentPlayerID)
                        return targetPlayer.gameObject;
                    break;

                case Mask.Props: case Mask.InteractableObject:
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
            if (playerHeldBy == null || targetObject?.GetComponent<NetworkObject>() == null || playerHeldBy.isClimbingLadder)
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
            switch ((Mask)targetObject.layer)
            {
                case Mask.Player:
                    {
                        if (!targetObject.TryGetComponent(out PlayerControllerB targetPlayer))
                            return false;

                        if(IsOwner)
                            Plugin.Log("Ray has hit a PLAYER -> " + targetPlayer.name);
                        if(targetPlayer.playerClientId == playerHeldBy.playerClientId || !PlayerModification.CanApplyModificationTo(targetPlayer, currentModificationType.Value, playerHeldBy))
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

                        if(item is GrabbablePlayerObject)
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an ITEM -> " + item.name);

                        if (!ObjectModification.CanApplyModificationTo(item, currentModificationType.Value, playerHeldBy))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnObjectModificationServerRpc(item.NetworkObjectId, playerHeldBy.playerClientId);
                        return true;
                    }
                case Mask.InteractableObject:
                    {
                        if (!targetObject.TryGetComponent(out Item item))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an INTERACTABLE OBJECT -> " + item.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                case Mask.Enemies:
                    {
                        if (!targetObject.TryGetComponent(out EnemyAI enemyAI))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an ENEMY -> " + enemyAI.enemyType.name);

                        if(!EnemyModification.CanApplyModificationTo(enemyAI, currentModificationType.Value, playerHeldBy))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnEnemyModificationServerRpc(enemyAI.NetworkObjectId, playerHeldBy.playerClientId);
                        return true;
                    }
                default:
                    if (IsOwner)
                        Plugin.Log("Ray has hit an unhandled object named \"" + targetObject.name + "\" [Layer " + targetObject.layer + "]");
                    return false;
            };

        }

        internal void ShootRayBeam()
        {
            if (!transform.TryGetComponent(out ShrinkRayFX shrinkRayFX) || shrinkRayFX == null || playerHeldBy == null)
            {
                Plugin.Log("Unable to shoot ray beam.", Plugin.LogType.Error);
                SwitchModeClientRpc((int)Mode.Unloading);
                return;
            }

            shrinkRayFX.RenderRayBeam(playerHeldBy.gameplayCamera.transform, targetObject.transform, currentModificationType.Value, audioSource, () =>
            {
                Plugin.Log("Ray beam, has finished.");
                SwitchModeClientRpc((int)Mode.Unloading);
            });
        }
        #endregion

        #region PlayerTargeting
        // ------ Ray hitting Player ------
        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerModificationServerRpc(ulong targetPlayerID, ulong playerHeldByID)
        {
            Plugin.Log("OnPlayerModification");
            OnPlayerModificationClientRpc(targetPlayerID, playerHeldByID);
        }

        [ClientRpc]
        public void OnPlayerModificationClientRpc(ulong targetPlayerID, ulong playerHeldByID)
        {
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
            PlayerModification.ApplyModificationTo(targetPlayer, currentModificationType.Value, playerHeldBy, () =>
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
            Plugin.Log("OnObjectModification");
            OnObjectModificationClientRpc(targetObjectNetworkID, playerHeldByID);
        }

        [ClientRpc]
        public void OnObjectModificationClientRpc(ulong targetObjectNetworkID, ulong playerHeldByID)
        {
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            if (!TryGetObjectByNetworkID(targetObjectNetworkID, out targetObject))
            {
                Plugin.Log("OnObjectModification: Object not found", Plugin.LogType.Error);
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Ray has hit " + targetObject.name + "!");
            ObjectModification.ApplyModificationTo(targetObject.GetComponentInParent<GrabbableObject>(), currentModificationType.Value, playerHeldBy, () =>
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
            Plugin.Log("OnEnemyModification");
            OnEnemyModificationClientRpc(targetEnemyNetworkID, playerHeldByID);
        }

        [ClientRpc]
        public void OnEnemyModificationClientRpc(ulong targetEnemyNetworkID, ulong playerHeldByID)
        {
            playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);

            if (!TryGetObjectByNetworkID(targetEnemyNetworkID, out targetObject))
            {
                Plugin.Log("OnEnemyModification: Enemy not found", Plugin.LogType.Error);
                if (IsOwner)
                    SwitchModeServerRpc((int)Mode.Missing);
                return;
            }

            Plugin.Log("Ray has hit " + targetObject.name + "!");
            EnemyModification.ApplyModificationTo(targetObject.GetComponentInParent<EnemyAI>(), currentModificationType.Value, playerHeldBy, () =>
            {
                Plugin.Log("Finished enemy modification with type: " + currentModificationType.Value.ToString());
            });

            if (IsOwner)
                SwitchModeServerRpc((int)Mode.Shooting);
        }
        #endregion
    }
}
