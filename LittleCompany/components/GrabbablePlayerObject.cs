using GameNetcodeStuff;
using LittleCompany.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LittleCompany.helper;
using Unity.Netcode;
using LittleCompany.patches.EnemyBehaviours;
using System.IO;
using System.Collections;
using static LittleCompany.helper.Moons;
using static LittleCompany.helper.LayerMasks;

namespace LittleCompany.components
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        
        #region Properties
        internal static string BaseAssetPath = Path.Combine(AssetLoader.BaseAssetPath, "grabbable");

        public NetworkVariable<ulong> grabbedPlayerID = new NetworkVariable<ulong>(ulong.MaxValue);
        private PlayerControllerB grabbedPlayerController { get; set; }
        public PlayerControllerB grabbedPlayer
        {
            get
            {
                if (grabbedPlayerController == null)
                {
                    if (PlayerInfo.CurrentPlayer == null) return null; // Needs a few more frames to connect playerController

                    if (grabbedPlayerID == null)
                    {
                        Plugin.Log("Unable to get grabbedPlayer.");
                        return null;
                    }

                    Initialize();
                }

                return grabbedPlayerController;
            }
        }

        private static GameObject networkPrefab { get; set; }

        private int frameCounter = 1;
        public bool IsCurrentPlayer { get; set; }
        public NetworkVariable<bool> IsOnSellCounter = new NetworkVariable<bool>(false);

        private bool IsGoombaCoroutineRunning = false;

        private EnemyAI enemyHeldBy = null;
        public HoarderBugAI lastHoarderBugGrabbedBy = null;
        public NetworkVariable<bool> InLastHoardingBugNestRange = new NetworkVariable<bool>(false);
        private bool Thrown = false;

        internal float previousCarryWeight = 1f;

        internal bool DeleteNextFrame = false;

        public enum TargetPlayer
        {
            GrabbedPlayer = 0,
            Holder
        }

        internal static AudioSource audioSource;
        internal static AudioClip grabSFX;
        internal static AudioClip dropSFX;
        internal static AudioClip throwSFX;
        internal static Sprite Icon = AssetLoader.LoadIcon("GrabbablePlayerIcon.png");

        internal static float BaseWeight = 0.05f;
        #endregion

        #region Networking
        public static void LoadAsset()
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = AssetLoader.littleCompanyAsset?.LoadAsset<Item>(Path.Combine(BaseAssetPath, "grabbablePlayerItem.asset"));
            if(assetItem == null)
            {
                Plugin.Log("Unable to load GrabbablePlayer asset!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;

            var component = networkPrefab.AddComponent<GrabbablePlayerObject>();

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerGrab.wav", (item) => grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerDrop.wav", (item) => dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerThrow.wav", (item) => throwSFX = item));

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            component.itemProperties = assetItem;
            component.itemProperties.isConductiveMetal = false;
            component.itemProperties.itemIcon = Icon;
            component.itemProperties.canBeGrabbedBeforeGameStart = true;
            component.itemProperties.positionOffset = new Vector3(-0.5f, 0.1f, 0f);

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        public override void OnNetworkDespawn()
        {
            Plugin.Log("Despawning gpo for player: " + grabbedPlayerID.Value);
            CleanUp();
            base.OnNetworkDespawn();
        }

        public override void OnNetworkSpawn()
        {
            Plugin.Log("Spawning gpo for player: " + grabbedPlayerID.Value);
            base.OnNetworkDespawn();
        }

        public static NetworkObject Instantiate(ulong playerID)
        {
            var obj = Instantiate(networkPrefab);
            DontDestroyOnLoad(obj);
            var gpo = obj.GetComponent<GrabbablePlayerObject>();
            gpo.Initialize(playerID); // todo: remove warning (can't simply move below .Spawn() as other players won't get grabbedPlayerID then somehow!)

            var networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn();

            return networkObj;
        }
#endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            if(!TryGetComponent(out audioSource))
                audioSource = gameObject.AddComponent<AudioSource>();

            if (grabbedPlayer == null)
                return;

            EnableInteractTrigger();
        }

        public override void Update()
        {
            base.Update();

            if (grabbedPlayer == null)
                return;

            if (grabbedPlayer.carryWeight != previousCarryWeight)
                WeightChangedBy(grabbedPlayer.carryWeight - previousCarryWeight);

            if (IsOnSellCounter.Value || enemyHeldBy != null || this.isHeld)
            {
                grabbedPlayer.transform.position = this.transform.position;

                if(this.isHeld)
                {
                    //this looks like trash unfortunately .. change this
                    Vector3 targetPosition = playerHeldBy.localItemHolder.transform.position;
                    Vector3 targetUp = -(grabbedPlayer.transform.position - targetPosition).normalized;
                    Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp) * grabbedPlayer.transform.rotation;
                    grabbedPlayer.transform.rotation = Quaternion.Slerp(grabbedPlayer.transform.rotation, targetRotation, 50 * Time.deltaTime);
                    grabbedPlayer.playerCollider.enabled = false;
                    grabbedPlayer.ResetFallGravity();
                }
            }
            else
            {
                transform.position = grabbedPlayer.transform.position;
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            if (grabbedPlayer == null)
                return;

            // Do several things only every x frames to save performance
            frameCounter++;
            if (frameCounter >= 1000) frameCounter = 0;

            if (IsCurrentPlayer)
            {
                if (frameCounter % 5 == 1)
                {
                    CheckForGoomba();

                    if (lastHoarderBugGrabbedBy != null)
                        HoarderBugAIPatch.HoarderBugEscapeRoutineForGrabbablePlayer(this);
                }

                if (playerHeldBy != null && ModConfig.Instance.values.CanEscapeGrab && Keyboard.current.spaceKey.wasPressedThisFrame)
                    DemandDropFromPlayerServerRpc(playerHeldBy.playerClientId, grabbedPlayer.playerClientId);
            }
            else if(playerHeldBy != null && playerHeldBy.isPlayerDead && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
            {
                Plugin.Log("Fallback triggered, where holder didn't drop held player upon death.");
                playerHeldBy.DiscardHeldObject();
            }

            if(DeleteNextFrame && PlayerInfo.IsHost)
            {
                DeleteNextFrame = false;
                GrabbablePlayerList.DespawnGrabbablePlayer(grabbedPlayerID.Value);
            }
        }
        
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                var direction = playerHeldBy.gameplayCamera.transform.forward;

                if (ModConfig.Instance.values.throwablePlayers)
                {
                    Plugin.Log("Throw grabbed player");
                    ThrowPlayerServerRpc(direction);
                }
            }
            catch (Exception e)
            {
                Plugin.Log("Error while throwing player: " + e.Message);
            }
        }

        public override void GrabItem()
        {
            if (PlayerInfo.IsHost && enemyHeldBy != null && enemyHeldBy is HoarderBugAI)
                HoarderBugAIPatch.DropHeldItem(enemyHeldBy as HoarderBugAI);

            Plugin.Log("Okay, let's grab " + name);
            base.GrabItem();

            grabbedPlayer.playerCollider.enabled = false;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            SetIsGrabbableToEnemies(false);
            SetControlTips();

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (grabbedPlayer != player)
                    IgnoreColliderWith(player.playerCollider);
            }

            if (IsCurrentPlayer && !ModConfig.Instance.values.friendlyFlight)
                SetHolderGrabbable(false);

            if (IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor(false);
        }

        public override void DiscardItem()
        {
            if (Thrown)
            {
                Thrown = false;
                if(throwSFX != null)
                    audioSource?.PlayOneShot(throwSFX);
            }
            else if(dropSFX != null)
                audioSource?.PlayOneShot(dropSFX);

            if (!ModConfig.Instance.values.friendlyFlight)
                SetHolderGrabbable(true);

            grabbedPlayer.ResetFallGravity();

            base.DiscardItem();
            grabbedPlayer.playerCollider.enabled = true;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            if(IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor();

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (grabbedPlayer != player)
                    IgnoreColliderWith(player.playerCollider, false);
            }

            SetIsGrabbableToEnemies(true);
            ResetControlTips();
        }

        public override void GrabItemFromEnemy(EnemyAI enemyAI)
        {
            Plugin.Log("Player " + grabbedPlayerID.Value + " got grabbed by enemy " + enemyAI.name);
            enemyHeldBy = enemyAI;

            if(grabSFX != null)
                audioSource?.PlayOneShot(grabSFX);

            if (IsCurrentPlayer)
            {
                grabbedPlayer.DropAllHeldItemsAndSync();
                PlayerInfo.EnableCameraVisor(false);
            }

            foreach (var collider in enemyHeldBy.gameObject.GetComponentsInChildren<Collider>())
                IgnoreColliderWith(collider, false);
        }

        public override void DiscardItemFromEnemy()
        {
            if(enemyHeldBy == null)
            {
                Plugin.Log("Lost enemyHeldBy on grabbable player " + grabbedPlayerID.Value, Plugin.LogType.Warning);
                return;
            }

            if (dropSFX != null)
                audioSource?.PlayOneShot(dropSFX);

            Plugin.Log("Player " + grabbedPlayerID.Value + " got dropped by enemy " + enemyHeldBy.name);

            foreach(var collider in enemyHeldBy.gameObject.GetComponentsInChildren<Collider>())
                IgnoreColliderWith(collider);

            if (enemyHeldBy is HoarderBugAI)
            {
                lastHoarderBugGrabbedBy = enemyHeldBy as HoarderBugAI;
                if (PlayerInfo.IsHost)
                    InLastHoardingBugNestRange.Value = true;

                grabbedPlayer.TeleportPlayer(lastHoarderBugGrabbedBy.transform.position); // To avoid glitching through walls
            }

            grabbedPlayer.ResetFallGravity();

            enemyHeldBy = null;

            if (IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor();
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if(grabSFX != null)
                audioSource?.PlayOneShot(grabSFX);
        }

        public override void PocketItem()
        {
            //drop the player if we attempt to pocket them
            //base.PocketItem();
            this.DiscardItem();
        }
        
        public override void OnPlaceObject()
        {
            grabbedPlayer.ResetFallGravity();

            base.OnPlaceObject();

            ResetControlTips();
        }
        #endregion

        #region Methods
        public void Initialize(ulong? playerID = null)
        {
            if(playerID != null)
                grabbedPlayerID.Value = playerID.Value;

            grabbedPlayerController = PlayerInfo.ControllerFromID(grabbedPlayerID.Value);
            if (grabbedPlayerController == null)
            {
                Plugin.Log("grabbedPlayer is null");
                return;
            }

            this.tag = "PhysicsProp";
            this.name = "grabbable_player" + grabbedPlayerID.Value;
            IsCurrentPlayer = PlayerInfo.CurrentPlayerID == grabbedPlayerID.Value;
            Plugin.Log("Is current player: " + IsCurrentPlayer);
            this.grabbable = true;

            EnableInteractTrigger();
            CalculateScrapValue();

            itemProperties.weight = grabbedPlayer.carryWeight + BaseWeight;
            grabbedPlayer.carryWeight = 1f + (grabbedPlayer.carryWeight - 1f) * ModConfig.Instance.values.weightMultiplier;
            previousCarryWeight = grabbedPlayer.carryWeight;
            Plugin.Log("gpo weight: " + itemProperties.weight);

            SetIsGrabbableToEnemies(true);
        }

        public void CleanUp()
        {
            Plugin.Log("Clean Up");
            try
            {
                SetIsGrabbableToEnemies(false);

                grabbedPlayer.carryWeight = 1f + (grabbedPlayer.carryWeight - 1f) / ModConfig.Instance.values.weightMultiplier;
                previousCarryWeight = grabbedPlayer.carryWeight;

                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Stop();

                if (PlayerInfo.IsHost && enemyHeldBy != null && enemyHeldBy is HoarderBugAI)
                    HoarderBugAIPatch.DropHeldItem(enemyHeldBy as HoarderBugAI);

                if (playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                    playerHeldBy.DiscardHeldObject(); // Can lead to problems
            }
            catch { };
        }

        public void UpdateScanNodeVisibility()
        {
            if (RoundManager.Instance?.currentLevel == null)
            {
                EnableScanNode(false);
                return;
            }

            bool IsCompanyMoon = Enum.TryParse(RoundManager.Instance.currentLevel.levelID.ToString(), out Moon level) && level == Moon.CompanyBuilding;
            if (IsCompanyMoon)
                Plugin.Log("UpdateScanNodeVisibility: We're on company moon!");
            if (IsCurrentPlayer)
                Plugin.Log("UpdateScanNodeVisibility: That's us!");
            EnableScanNode(IsCompanyMoon && !IsCurrentPlayer);
        }

        public void EnableScanNode(bool enable = true)
        {
            Plugin.Log("EnableScanNode: " + enable);
            var scanNode = GetComponentInChildren<ScanNodeProperties>();
            if (scanNode == null)
            {
                Plugin.Log("No scan node for " + name, Plugin.LogType.Warning);
                return;
            }

            scanNode.enabled = enable;

            if (scanNode.TryGetComponent(out BoxCollider collider))
            {
                Plugin.Log("Found BoxCollider for scanNode");
                collider.enabled = enable;
            }
        }

        public void IgnoreColliderWith(Collider otherCollider, bool ignore = true)
        {
            if (otherCollider == null) return;

            //Plugin.Log((ignore ? "Ignoring" : "Allowing") + " collide with " + otherCollider.name);

            Collider thisPlayerCollider = grabbedPlayer.playerCollider;
            Collider thisCollider = propColliders[0];

            Physics.IgnoreCollision(thisPlayerCollider, otherCollider, ignore);
            Physics.IgnoreCollision(thisCollider, otherCollider, ignore);
        }

        public void EnableInteractTrigger(bool enable = true)
        {
            EnablePhysics(enable && !IsCurrentPlayer);
            //EnableItemMeshes(enable && !IsCurrentPlayer);
            UpdateScanNodeVisibility(); // also a collider that gets enabled through EnablePhysics()
        }

        public void CalculateScrapValue()
        {
            // todo: change scrap value when grabbed player grabs something
            int value = 5; // todo: find where the player scrap value set in code for deadBody

            if (grabbedPlayer != null && grabbedPlayer.ItemSlots != null)
            {
                foreach (var item in grabbedPlayer.ItemSlots)
                    if (item != null)
                        value += item.scrapValue;
            }

            var scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
            if (scanNode == null)
                return;

            SetScrapValue(value);
            Plugin.Log("Scrap value: " + value);
        }

        public void WeightChangedBy(float diff)
        {
            var modifiedValue = diff * (ModConfig.Instance.values.weightMultiplier - 1f);

            itemProperties.weight += diff;

            grabbedPlayer.carryWeight += modifiedValue;

            Plugin.Log("Weight of " + name + " changed by " + (diff + modifiedValue) + " (originally " + diff + ") from " + previousCarryWeight + " to " + grabbedPlayer.carryWeight, Plugin.LogType.Warning);
            previousCarryWeight = grabbedPlayer.carryWeight;

            CalculateScrapValue(); // When our weight changed it's likely that our value did too
        }

        public void UpdateWeightAfterDropping(GrabbableObject droppedObject)
        {
            var realObjectWeight = droppedObject.itemProperties.weight - 1f;

            itemProperties.weight -= realObjectWeight;
            grabbedPlayer.carryWeight -= realObjectWeight * (ModConfig.Instance.values.weightMultiplier - 1f);

            if (playerHeldBy != null) // Update holder weight
                playerHeldBy.carryWeight -= realObjectWeight;

            Plugin.Log("Dropped " + droppedObject.name + " -> Weight: " + realObjectWeight + ", new gpo weight: " + itemProperties.weight
                 + ", new player weight: " + grabbedPlayer.carryWeight + ", new holder weight: " + (playerHeldBy != null ? playerHeldBy.carryWeight : "no holder"));
        }

        public void SetIsGrabbableToEnemies(bool isGrabbable = true)
        {
            if(grabbedPlayer == null)
            {
                Plugin.Log("SetIsGrabbableToEnemies: Grabbed player is null.", Plugin.LogType.Error);
                return;
            }

            if (!PlayerInfo.IsShrunk(grabbedPlayer))
                isGrabbable = false;

            grabbableToEnemies = isGrabbable;

            Plugin.Log("GrabbablePlayer - Allow enemy grab: " + isGrabbable);

            if (ModConfig.Instance.values.hoardingBugBehaviour != ModConfig.HoardingBugBehaviour.NoGrab)
                HoarderBugAI.RefreshGrabbableObjectsInMapList();
        }

        private PlayerControllerB GetPlayerAbove()
        {
            // Cast a ray upwards to check for the player above
            if (Physics.Raycast(grabbedPlayer.gameplayCamera.transform.position, grabbedPlayer.gameObject.transform.up, out RaycastHit hit, 0.5f, ToInt([Mask.Player]), QueryTriggerInteraction.Ignore))
                return hit.collider.gameObject.GetComponent<PlayerControllerB>();

            return null;
        }

        private void CheckForGoomba()
        {
            if (IsGoombaCoroutineRunning)
                return; // Already running

            if (!ModConfig.Instance.values.jumpOnShrunkenPlayers)
                return;

            if (isHeld)
            {
                //Plugin.log("Apes together strong! Goomba impossible.");
                return;
            }

            var playerAbove = GetPlayerAbove();
            if (playerAbove == null)
                return;

            if (PlayerInfo.SizeOf(PlayerInfo.CurrentPlayer) >= PlayerInfo.SizeOf(playerAbove))
                return; // 2 Weak 2 Goomba c:

            IsGoombaCoroutineRunning = true;

            OnGoombaServerRpc(PlayerInfo.CurrentPlayer.playerClientId);
        }

        private GrabbableObject GrabbedPlayerCurrentItem()
        {
            if (grabbedPlayer == null)
            {
                Plugin.Log("GrabbedPlayer is null?!", Plugin.LogType.Error);
                return null;
            }

            if (grabbedPlayer.isHoldingObject && grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot] != null)
                return grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot];

            return null;
        }

        private void SetHolderGrabbable(bool isGrabbable = true)
        {
            if (!IsCurrentPlayer || playerHeldBy == null) return; // Only do this from the perspective of the currently held player, not the holder himself

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(playerHeldBy.playerClientId, out GrabbablePlayerObject gpo))
                gpo.EnableInteractTrigger(isGrabbable);
        }

        private void SetControlTips()
        {
            HUDManager.Instance.ClearControlTips();

            if (playerHeldBy == null)
                return;

            Plugin.Log("setControlTips");
            if (base.IsOwner)
            {
                string[] tips = { "Throw player: LMB" };
                HUDManager.Instance.ChangeControlTipMultiple(
                    tips,
                    holdingItem: true,
                    itemProperties);
            }
            else if (!ModConfig.Instance.values.CanEscapeGrab)
                return;
            else
            {
                var grabbedPlayerItem = GrabbedPlayerCurrentItem();
                if (grabbedPlayerItem != null) // only case that's not working so far!
                {
                    var toolTips = grabbedPlayerItem.itemProperties.toolTips.ToList();
                    toolTips.Add("Ungrab : JUMP");
                    HUDManager.Instance.ChangeControlTipMultiple(toolTips.ToArray(), holdingItem: true, grabbedPlayerItem.itemProperties);
                }
                else
                {
                    string[] tips = { "Ungrab: JUMP" };
                    HUDManager.Instance.ChangeControlTipMultiple(tips, holdingItem: false, itemProperties);
                }
            }
        }

        private void ResetControlTips()
        {
            if (base.IsOwner)
            {
                Plugin.Log("IsOwner");
                return; // happens automatically
            }

            HUDManager.Instance.ClearControlTips();

            Plugin.Log("resetControlTips");

            var grabbedPlayerItem = GrabbedPlayerCurrentItem();
            if (grabbedPlayerItem != null)
                HUDManager.Instance.ChangeControlTipMultiple(grabbedPlayerItem.itemProperties.toolTips, holdingItem: true, grabbedPlayerItem.itemProperties);
        }

        internal bool CanSellKill()
        {
            return IsOnSellCounter.Value && IsCurrentPlayer;
        }

        #endregion

        #region RPCs
        [ServerRpc(RequireOwnership = false)]
        public void ThrowPlayerServerRpc(Vector3 direction)
        {
            ThrowPlayerClientRpc(direction);
        }

        [ClientRpc]
        public void ThrowPlayerClientRpc(Vector3 direction)
        {
            Plugin.Log("ThrowPlayerClientRpc");
            Thrown = true;

            if (playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                playerHeldBy.DiscardHeldObject();

            if (grabbedPlayer == null) return;

            if(throwSFX != null)
                grabbedPlayer.movementAudio?.PlayOneShot(throwSFX);

            if (grabbedPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
            {
                Plugin.Log("We got thrown!");
                coroutines.PlayerThrowAnimation.StartRoutine(grabbedPlayer, direction, 10f);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DemandDropFromPlayerServerRpc(ulong holdingPlayerID, ulong heldPlayerID)
        {
            DemandDropFromPlayerClientRpc(holdingPlayerID, heldPlayerID);
        }

        [ClientRpc]
        public void DemandDropFromPlayerClientRpc(ulong holdingPlayerID, ulong heldPlayerID)
        {
            var currentPlayerID = PlayerInfo.CurrentPlayerID;
            if (currentPlayerID == holdingPlayerID)
            {
                Plugin.Log("Player " + heldPlayerID + " demanded to be dropped from you .. so it shall be!");
                StartOfRound.Instance.localPlayerController.DiscardHeldObject();
            }
            else if (currentPlayerID == heldPlayerID)
                Plugin.Log("You demanded to be dropped from player " + holdingPlayerID);
            else
                Plugin.Log("Player " + heldPlayerID + " demanded to be dropped from player " + currentPlayerID + ".");
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnGoombaServerRpc(ulong playerID)
        {
            if (IsGoombaCoroutineRunning) return;
            OnGoombaClientRpc(playerID);
        }

        [ClientRpc]
        public void OnGoombaClientRpc(ulong playerID)
        {
            var currentPlayer = PlayerInfo.CurrentPlayer;
            if(currentPlayer.playerClientId == playerID)
                Plugin.Log("WE GETTING GOOMBAD");
            else
                Plugin.Log("A goomba on player " + playerID);
            coroutines.GoombaStomp.StartRoutine(PlayerInfo.ControllerFromID(playerID).gameObject, () =>
            {
                if (playerID == currentPlayer.playerClientId)
                    IsGoombaCoroutineRunning = false;
            });
        }

        [ServerRpc(RequireOwnership = false)]
        internal void PlaceOnSellCounterServerRpc()
        {
            IsOnSellCounter.Value = true;
            if (IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor(false);
            PlaceOnSellCounterClientRpc();
        }

        [ClientRpc]
        internal void PlaceOnSellCounterClientRpc()
        {
            itemProperties.isScrap = true; // todo: check if this can be generally set to true, to remove this ClientRpc
        }

        [ServerRpc(RequireOwnership = false)]
        internal void RemoveFromSellCounterServerRpc()
        {
            IsOnSellCounter.Value = false;

            if (IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor();
        }
        #endregion

        #region TeleportFix
        internal IEnumerator UpdateRegionAfterTeleportEnsured(TargetPlayer teleportingPlayer)
        {
            if (grabbedPlayer == null || playerHeldBy == null)
                yield break;

            yield return null; // Wait for next frame
            UpdateRegionAfterTeleport(teleportingPlayer); // Let the other person of this holder/grabbed connection know that they teleported with us
        }

        private void UpdateRegionAfterTeleport(TargetPlayer teleportingPlayer)
        {
            Plugin.Log("UpdateRegionAfterTeleport -> " + teleportingPlayer.ToString());

            if (grabbedPlayer == null || playerHeldBy == null)
            {
                Plugin.Log("Tried syncing region info between holder and grabbed player, but one was null.", Plugin.LogType.Error);
                return;
            }

            if(playerHeldBy.isInsideFactory == grabbedPlayer.isInsideFactory)
            {
                Plugin.Log("Syncing of region info between holder and grabbed player not required. Both at same region.");
                return;
            }

            if (teleportingPlayer == TargetPlayer.GrabbedPlayer)
            {
                Plugin.Log("Syncing for holder. isInsideFactory changing from " + playerHeldBy.isInsideFactory + " to " + grabbedPlayer.isInsideFactory);
                UpdateLocationDataFromPlayerTo(grabbedPlayer, playerHeldBy);
            }
            else
            {
                Plugin.Log("Syncing for grabbed player. isInsideFactory changing from " + grabbedPlayer.isInsideFactory + " to " + playerHeldBy.isInsideFactory);
                UpdateLocationDataFromPlayerTo(playerHeldBy, grabbedPlayer);
            }
        }

        internal void UpdateLocationDataFromPlayerTo(PlayerControllerB playerFrom, PlayerControllerB playerTo)
        {
            bool locationChanged = playerTo.isInsideFactory != playerFrom.isInsideFactory;
            if (!locationChanged) return;

            playerTo.isInsideFactory = playerFrom.isInsideFactory;
            playerTo.isInElevator = playerFrom.isInElevator;
            playerTo.isInHangarShipRoom = playerFrom.isInHangarShipRoom;

            foreach(var item in playerTo.ItemSlots)
            {
                if(item == null) continue;
                item.isInFactory = playerTo.isInsideFactory;
            }

            PlayerInfo.UpdateWeatherForPlayer(playerTo);
        }
        #endregion

        #region HoardingBugGrab
        [ServerRpc(RequireOwnership = false)]
        internal void HoardingBugTargetUsServerRpc(ulong networkObjectID)
        {
            var hoardingBugs = FindObjectsOfType<HoarderBugAI>();
            foreach (var hoardingBug in hoardingBugs)
            {
                if (hoardingBug.NetworkObjectId == networkObjectID)
                {
                    HoarderBugAIPatch.HoardingBugTargetUs(hoardingBug, this);
                    return;
                }
            }

            Plugin.Log("Unable to find hoarder bug that should target us");
        }

        [ServerRpc(RequireOwnership = false)]
        internal void MovedOutOfHoardingBugNestRangeServerRpc(bool hoardingBugDied = false)
        {
            Plugin.Log("MovedOutOfHoardingBugNestRangeServerRpc");
            InLastHoardingBugNestRange.Value = false;

            if (lastHoarderBugGrabbedBy == null)
            {
                Plugin.Log("lastHoarderBugGrabbedBy is null, but shouldn't be", Plugin.LogType.Warning);
                MovedOutOfHoardingBugNestRangeClientRpc();
                return;
            }

            if (playerHeldBy == null)
                HoarderBugAIPatch.AddToGrabbables(this);

            if (hoardingBugDied)
                Plugin.Log("The hoarder bug who grabbed us died or lost their nest. Poor bug.");
            else
                HoarderBugAIPatch.MovedOutOfHoardingBugNestRange(this);

            MovedOutOfHoardingBugNestRangeClientRpc();
        }

        [ClientRpc]
        internal void MovedOutOfHoardingBugNestRangeClientRpc()
        {
            lastHoarderBugGrabbedBy = null;
        }
        #endregion
    }
}
