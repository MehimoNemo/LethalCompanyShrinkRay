using GameNetcodeStuff;
using LittleCompany.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LittleCompany.helper;
using Unity.Netcode;
using System.IO;
using System.Collections;

using LittleCompany.patches.EnemyBehaviours;
using static LittleCompany.helper.Moons;
using static LittleCompany.helper.LayerMasks;
using LittleCompany.dependency;
using LittleCompany.coroutines;

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
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

        private EnemyAI enemyHeldBy = null;
        public HoarderBugAI lastHoarderBugGrabbedBy = null;
        public NetworkVariable<bool> InLastHoardingBugNestRange = new NetworkVariable<bool>(false);
        private bool Thrown = false;

        internal float previousCarryWeight = 1f;

        internal bool DeleteNextFrame = false;
        internal bool PlayerControlled = true;
        internal Transform GrabbedPlayerParent = null;

        public enum TargetPlayer
        {
            GrabbedPlayer = 0,
            Holder
        }

        internal AudioSource audioSource;
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
            ScrapManagementFacade.FixMixerGroups(networkPrefab);

            var component = networkPrefab.AddComponent<GrabbablePlayerObject>();

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerGrab.wav", (item) => grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerDrop.wav", (item) => dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerThrow.wav", (item) => throwSFX = item));

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            component.itemProperties = assetItem;
            component.itemProperties.isConductiveMetal = false;
            component.itemProperties.itemIcon = Icon;
            component.itemProperties.canBeGrabbedBeforeGameStart = true;
            //component.itemProperties.positionOffset = new Vector3(-0.5f, 0.1f, 0f);

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        public override void OnNetworkDespawn()
        {
            GrabbablePlayerList.GrabbablePlayerObjects.Remove(grabbedPlayerID.Value);
            Plugin.Log("Despawning gpo for player: " + grabbedPlayerID.Value + ". " + GrabbablePlayerList.GrabbablePlayerObjects.Count + " grabbable players now.");
            CleanUp();

            base.OnNetworkDespawn();
        }

        public override void OnNetworkSpawn()
        {
            GrabbablePlayerList.GrabbablePlayerObjects.Add(grabbedPlayerID.Value, this);
            Plugin.Log("Spawning gpo for player: " + grabbedPlayerID.Value + ". " + GrabbablePlayerList.GrabbablePlayerObjects.Count + " grabbable players now.");

            base.OnNetworkDespawn();
        }

        public static GrabbablePlayerObject Instantiate(ulong playerID)
        {
            var obj = Instantiate(networkPrefab);
            DontDestroyOnLoad(obj);
            var gpo = obj.GetComponent<GrabbablePlayerObject>();
            gpo.Initialize(playerID); // todo: remove warning (can't simply move below .Spawn() as other players won't get grabbedPlayerID then somehow!)

            var networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn();

            return gpo;
        }
#endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            if (grabbedPlayer == null)
                return;
            
            if (!TryGetComponent(out audioSource)) // fallback that likely won't happen nowadays
            {
                Plugin.Log("AudioSource of " + gameObject.name + " was null. Adding a new one..", Plugin.LogType.Error);
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            UpdateInteractTrigger();
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

                grabbedPlayer.ResetFallGravity();

                if (this.isHeld)
                    grabbedPlayer.playerCollider.enabled = false;
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

            if(DeleteNextFrame)
            {
                if(PlayerInfo.IsHost && GrabbablePlayerList.RemovePlayerGrabbable(this))
                    DeleteNextFrame = false;
            }
        }
        
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                var direction = playerHeldBy.gameplayCamera.transform.forward;

                if (ModConfig.Instance.values.throwablePlayers)
                {
                    var sizeDifference = Mathf.Abs(PlayerInfo.SizeOf(playerHeldBy) - PlayerInfo.SizeOf(grabbedPlayer));
                    var force = Mathf.Max(sizeDifference * 15f, 10f);
                    ThrowPlayerServerRpc(direction, force);
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

            float size = PlayerInfo.SizeOf(grabbedPlayerController);
            itemProperties.positionOffset = new Vector3(-0.75f * size, 0.15f, 0.05f);

            grabbedPlayer.playerCollider.enabled = false;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            SetIsGrabbableToEnemies(false);
            SetControlTips();

            foreach (var player in PlayerInfo.AlivePlayers)
            {
                if (grabbedPlayer != player)
                    IgnoreColliderWith(player.playerCollider);
            }

            UpdateInteractTrigger();

            if (IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor(false);
        }

        public override void DiscardItem()
        {
            if (Thrown)
            {
                Thrown = false;
                if(throwSFX != null && audioSource != null)
                    audioSource.PlayOneShot(throwSFX);
            }
            else if(dropSFX != null && audioSource != null)
                audioSource.PlayOneShot(dropSFX);

            grabbedPlayer.ResetFallGravity();

            base.DiscardItem();
            grabbedPlayer.playerCollider.enabled = true;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            if(IsCurrentPlayer)
                PlayerInfo.EnableCameraVisor();

            foreach (var player in PlayerInfo.AlivePlayers)
            {
                if (grabbedPlayer != player)
                    IgnoreColliderWith(player.playerCollider, false);
            }

            SetIsGrabbableToEnemies(true);
            ResetControlTips();

            UpdateInteractTrigger();
        }

        public override void GrabItemFromEnemy(EnemyAI enemyAI)
        {
            base.GrabItemFromEnemy(enemyAI);
            Plugin.Log("Player " + grabbedPlayerID.Value + " got grabbed by enemy " + enemyAI.name);
            enemyHeldBy = enemyAI;

            if(grabSFX != null && audioSource != null)
                audioSource.PlayOneShot(grabSFX);

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
            base.DiscardItemFromEnemy();
            if (enemyHeldBy == null)
            {
                Plugin.Log("Lost enemyHeldBy on grabbable player " + grabbedPlayerID.Value, Plugin.LogType.Warning);
                return;
            }

            if (dropSFX != null && audioSource != null)
                audioSource.PlayOneShot(dropSFX);

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
            if(grabSFX != null && audioSource != null)
                audioSource.PlayOneShot(grabSFX);
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
        int InitializeAttempts = 0;
        bool AboutToBeRemoved = false;
        public void Initialize(ulong? playerID = null)
        {
            if (playerID != null)
                grabbedPlayerID.Value = playerID.Value;

            if (InitializeAttempts > 1000) // Really can't get that one, don't we?
            {
                if(!AboutToBeRemoved)
                {
                    Plugin.Log("Removed grabbable object from player " + grabbedPlayerID.Value + " as they're unable to be found.. Please contact the mod dev if possible.", Plugin.LogType.Error);
                    ReInitializeServerRpc();
                    AboutToBeRemoved = true;
                }
                return;
            }
            InitializeAttempts++;

            grabbedPlayerController = PlayerInfo.ControllerFromID(grabbedPlayerID.Value);
            if (grabbedPlayerController == null)
            {
                if(InitializeAttempts % 100 == 1)
                    Plugin.Log("Player with ID " + grabbedPlayerID.Value + " not found. Trying again later.");
                return;
            }

            this.tag = "PhysicsProp";
            this.name = "grabbable_player" + grabbedPlayerID.Value;
            IsCurrentPlayer = PlayerInfo.CurrentPlayerID == grabbedPlayerID.Value;
            Plugin.Log("Is current player: " + IsCurrentPlayer);
            this.grabbable = true;
            itemProperties = Instantiate(itemProperties);

            UpdateInteractTrigger();
            CalculateScrapValue();

            itemProperties.weight = grabbedPlayer.carryWeight + BaseWeight;
            grabbedPlayer.carryWeight = 1f + (grabbedPlayer.carryWeight - 1f) * ModConfig.Instance.values.weightMultiplier;
            previousCarryWeight = grabbedPlayer.carryWeight;
            Plugin.Log("gpo weight: " + itemProperties.weight);

            SetIsGrabbableToEnemies(true);
            //SetPlayerControlled(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReInitializeServerRpc()
        {
            GrabbablePlayerList.ReInitializePlayerGrabbable(grabbedPlayerID.Value);
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
                enemyHeldBy = null;

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
                collider.enabled = enable;
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

        public void UpdateInteractTrigger()
        {
            Plugin.Log("UpdateInteractTrigger");
            if (propColliders.Length == 0) return;

            var enable = true;
            if (IsCurrentPlayer ||                                                                                  // This is our gpo
                (playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayer.playerClientId) || // We're the holder
                PlayerInfo.SizeOf(grabbedPlayer) >= PlayerInfo.CurrentPlayerScale ||								// We're smaller than the player of this grabbableObject
				grabbedPlayer.isClimbingLadder || grabbedPlayer.inSpecialInteractAnimation)                         // Player is in an animation
                    enable = false;

            EnableInteractTrigger(enable);
        }

        // Makes the player grabbable / ungrabbable
        public void EnableInteractTrigger(bool enable = true)
        {
            Plugin.Log("EnableInteractTrigger: " + enable);
            propColliders[0].enabled = enable;
            UpdateScanNodeVisibility(); // also a collider that gets enabled through EnablePhysics()
        }

        public void CalculateScrapValue()
        {
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

            //Plugin.Log("Weight of " + name + " changed by " + (diff + modifiedValue) + " from " + previousCarryWeight + " to " + grabbedPlayer.carryWeight + ". New gpo weight: " + itemProperties.weight, Plugin.LogType.Warning);
            previousCarryWeight = grabbedPlayer.carryWeight;

            CalculateScrapValue(); // When our weight changed it's likely that our value did too
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
            if (Physics.Raycast(grabbedPlayer.gameplayCamera.transform.position, Vector3.up, out RaycastHit hit, 1f, ToInt([Mask.Player]), QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlayerControllerB>();

            return null;
        }

        private void CheckForGoomba()
        {
            if (!ModConfig.Instance.values.jumpOnShrunkenPlayers)
                return;

            if (isHeld)
            {
                //Plugin.log("Apes together strong! Goomba impossible.");
                return;
            }

            if (GoombaStomp.IsGettingGoombad(PlayerInfo.CurrentPlayer))
                return; // Already running

            var playerAbove = GetPlayerAbove();
            if (playerAbove == null)
                return;

            if (PlayerInfo.SizeOf(PlayerInfo.CurrentPlayer) >= PlayerInfo.SizeOf(playerAbove))
                return; // 2 Weak 2 Goomba c:

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
            else if(IsCurrentPlayer && ModConfig.Instance.values.CanEscapeGrab)
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
                return; // happens automatically

            if(IsCurrentPlayer)
            {
                HUDManager.Instance.ClearControlTips();

                Plugin.Log("resetControlTips");

                var grabbedPlayerItem = GrabbedPlayerCurrentItem();
                if (grabbedPlayerItem != null)
                    HUDManager.Instance.ChangeControlTipMultiple(grabbedPlayerItem.itemProperties.toolTips, holdingItem: true, grabbedPlayerItem.itemProperties);
            }
        }

        internal bool CanSellKill()
        {
            return IsOnSellCounter.Value && IsCurrentPlayer;
        }

        #endregion

        #region RPCs
        [ServerRpc(RequireOwnership = false)]
        public void ThrowPlayerServerRpc(Vector3 direction, float force)
        {
            ThrowPlayerClientRpc(direction, force);
        }

        [ClientRpc]
        public void ThrowPlayerClientRpc(Vector3 direction, float force)
        {
            Plugin.Log("ThrowPlayerClientRpc");
            Thrown = true;

            if (playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                playerHeldBy.DiscardHeldObject();

            if (grabbedPlayer == null) return;

            if(throwSFX != null && grabbedPlayer.movementAudio != null)
                grabbedPlayer.movementAudio.PlayOneShot(throwSFX);

            if (grabbedPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
            {
                Plugin.Log("We got thrown with a force of " + force);
                coroutines.PlayerThrowAnimation.StartRoutine(grabbedPlayer, direction, force);
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
            var currentPlayer = PlayerInfo.CurrentPlayer;
            if (currentPlayer == null)
            {
                Plugin.Log("DemandDropFromPlayerClientRpc: Local player controller not found.", Plugin.LogType.Error);
                return;
            }

            var currentPlayerID = currentPlayer.playerClientId;
            if (currentPlayerID == holdingPlayerID)
            {
                Plugin.Log("Player " + heldPlayerID + " demanded to be dropped from you .. so it shall be!");
                if(currentPlayer.currentlyHeldObjectServer == null)
                {
                    Plugin.Log("Player demanded to be dropped from us, but we aren't holding anyone.", Plugin.LogType.Warning);
                    return;
                }

                currentPlayer.DiscardHeldObject();
            }
            else if (currentPlayerID == heldPlayerID)
                Plugin.Log("You demanded to be dropped from player " + holdingPlayerID);
            else
                Plugin.Log("Player " + heldPlayerID + " demanded to be dropped from player " + currentPlayerID + ".");
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnGoombaServerRpc(ulong playerID)
        {
            var targetPlayer = PlayerInfo.ControllerFromID(playerID);
            if (targetPlayer != null && GoombaStomp.IsGettingGoombad(targetPlayer)) return;

            OnGoombaClientRpc(playerID);
        }

        [ClientRpc]
        public void OnGoombaClientRpc(ulong playerID)
        {
            var targetPlayer = PlayerInfo.ControllerFromID(playerID);
            if (targetPlayer == null)
                return;

            if(targetPlayer == PlayerInfo.CurrentPlayer)
                Plugin.Log("WE GETTING GOOMBAD");
            else
                Plugin.Log("A goomba on player " + playerID);
            GoombaStomp.GoombaPlayer(targetPlayer);
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

        [ServerRpc(RequireOwnership = false)]
        public void DamageGrabbedPlayerServerRpc(int damageNumber, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        {
            DamageGrabbedPlayerClientRpc(damageNumber, causeOfDeath, deathAnimation, fallDamage);
        }

        [ClientRpc]
        public void DamageGrabbedPlayerClientRpc(int damageNumber, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false)
        {
            if (!IsCurrentPlayer) return;

            grabbedPlayer.DamagePlayer(damageNumber, false, false, causeOfDeath, deathAnimation, fallDamage, default);
        }
        #endregion
    }
}
