﻿using GameNetcodeStuff;
using LCShrinkRay.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LCShrinkRay.helper;
using Unity.Netcode;
using LCShrinkRay.patches;
using System.IO;
using System.Reflection;
using System.Collections;
using System.ComponentModel;

namespace LCShrinkRay.comp
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
        private bool IsCurrentPlayer { get; set; }
        public NetworkVariable<bool> IsOnSellCounter = new NetworkVariable<bool>(false);

        private bool IsGoombaCoroutineRunning = false;

        private EnemyAI enemyHeldBy = null;
        public HoarderBugAI lastHoarderBugGrabbedBy = null;
        public NetworkVariable<bool> InLastHoardingBugNestRange = new NetworkVariable<bool>(false);
        private bool Thrown = false;

        public enum TargetPlayer
        {
            GrabbedPlayer = 0,
            Holder
        }

        internal static AudioClip grabSFX;
        internal static AudioClip dropSFX;
        internal static AudioClip throwSFX;
        internal static Sprite Icon = AssetLoader.LoadIcon("GrabbablePlayerIcon.png");
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
            SetIsGrabbableToEnemies(false);
            if (PlayerInfo.IsHost && enemyHeldBy != null && enemyHeldBy is HoarderBugAI)
            {
                var hoarderBug = enemyHeldBy as HoarderBugAI;
                hoarderBug.heldItem = null;
                hoarderBug.targetItem = null;
                hoarderBug.SwitchToBehaviourState(0);
            }

            if(playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                playerHeldBy.DiscardHeldObject();

            Plugin.Log("Despawning gpo for player: " + grabbedPlayerID.Value);
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
            gpo.Initialize(playerID);

            var networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn();

            return networkObj;
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            itemProperties.grabSFX = grabSFX;
        }

        public override void Update()
        {
            base.Update();

            if (grabbedPlayer == null)
                return;

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

            if (!IsCurrentPlayer) return;

            if (grabbedPlayer == null)
                return;

            // Do several things only every x frames to save performance
            frameCounter++;
            if (frameCounter >= 1000) frameCounter = 0;

            if (frameCounter % 5 == 1)
            {
                CheckForGoomba();

                if (lastHoarderBugGrabbedBy != null)
                    HoarderBugAIPatch.HoarderBugEscapeRoutineForGrabbablePlayer(this);
            }

            if (playerHeldBy != null && ModConfig.Instance.values.CanEscapeGrab && Keyboard.current.spaceKey.wasPressedThisFrame)
                DemandDropFromPlayerServerRpc(playerHeldBy.playerClientId, grabbedPlayer.playerClientId);
        }
        
        public override void PocketItem()
        {
            if (!IsCurrentPlayer) return;

            //drop the player if we attempt to pocket them
            //base.PocketItem();
            this.DiscardItem();
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

        [ServerRpc(RequireOwnership = false)]
        public void ThrowPlayerServerRpc(Vector3 direction)
        {
            ThrowPlayerClientRpc(direction);
        }

        [ClientRpc]
        public void ThrowPlayerClientRpc(Vector3 direction)
        {
            Thrown = true;

            if (playerHeldBy != null && playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                playerHeldBy.DiscardHeldObject();

            if (grabbedPlayer == null) return;

            grabbedPlayer.movementAudio.PlayOneShot(throwSFX);

            if (grabbedPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
            {
                Plugin.Log("We got thrown!");
                coroutines.PlayerThrowAnimation.StartRoutine(grabbedPlayer, direction, 10f);
            }
        }

        public override void DiscardItem()
        {
            if (Thrown)
            {
                Thrown = false;
                grabbedPlayer.itemAudio.PlayOneShot(throwSFX);
            }
            else
                grabbedPlayer.itemAudio.PlayOneShot(dropSFX);

            if (!ModConfig.Instance.values.friendlyFlight)
                SetHolderGrabbable(true);

            grabbedPlayer.ResetFallGravity();

            base.DiscardItem();
            grabbedPlayer.playerCollider.enabled = true;
            this.propColliders[0].enabled = true;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            if (PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = true;

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

            if(IsCurrentPlayer)
                grabbedPlayer.DropAllHeldItemsAndSync();

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

            PlayerInfo.AdjustArmScale(grabbedPlayer);
            PlayerInfo.AdjustMaskScale(grabbedPlayer);
            PlayerInfo.AdjustMaskPos(grabbedPlayer);

            grabbedPlayer.ResetFallGravity();

            enemyHeldBy = null;
        }

        public override void GrabItem()
        {
            Plugin.Log("Okay, let's grab!");
            base.GrabItem();

            grabbedPlayer.playerCollider.enabled = false;
            this.propColliders[0].enabled = false;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            if (PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = false;

            SetIsGrabbableToEnemies(false);
            SetControlTips();

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (grabbedPlayer != player)
                    IgnoreColliderWith(player.playerCollider);
            }

            /*
            if(grabbedPlayer.playerClientId == PlayerHelper.currentPlayer().playerClientId)
                calculateWeight();
            
            if(!holdingPlayer && playerHeldBy != null)
            {
                var gpo = GrabbablePlayerList.findGrabbableObjectForPlayer(playerHeldBy);
                if(gpo != null)
                    gpo.calculateWeight();
            }*/

            if (!ModConfig.Instance.values.friendlyFlight)
                SetHolderGrabbable(false);
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
            Plugin.Log("GrabbablePlayerObject.Initialize");

            if(playerID != null)
                grabbedPlayerID.Value = playerID.Value;

            grabbedPlayerController = PlayerInfo.ControllerFromID(grabbedPlayerID.Value);
            if (grabbedPlayer == null)
            {
                Plugin.Log("grabbedPlayer is null");
                return;
            }

            this.tag = "PhysicsProp";
            this.name = "grabbable_player" + grabbedPlayerID.Value;
            Plugin.Log("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId + "]");
            IsCurrentPlayer = PlayerInfo.CurrentPlayerID == grabbedPlayerID.Value;
            Plugin.Log("Is current player: " + IsCurrentPlayer);
            this.grabbable = true;
            EnableInteractTrigger(!IsCurrentPlayer);

            CalculateScrapValue();
            SetIsGrabbableToEnemies(true);

            gameObject.layer = (int)LayerMasks.Mask.Props;
        }

        public void IgnoreColliderWith(Collider otherCollider, bool ignore = true)
        {
            if (otherCollider == null) return;

            //Plugin.Log((ignore ? "Ignoring" : "Allowing") + " collide with " + otherCollider.name);

            Collider thisPlayerCollider = grabbedPlayer.playerCollider;
            Collider thisCollider = this.propColliders[0];

            Physics.IgnoreCollision(thisPlayerCollider, otherCollider, ignore);
            Physics.IgnoreCollision(thisCollider, otherCollider, ignore);
        }

        public void EnableInteractTrigger(bool enable = true)
        {
            Plugin.Log((enable ? "Enabling" : "Disabling") + " trigger for grabbable player " + grabbedPlayerID.Value);
            tag = enable ? "PhysicsProp" : "InteractTrigger"; // Bit wacky code, but it is what it is. Easier than adding custom InteractTrigger as of now
            gameObject.layer = (int)(enable ? LayerMasks.Mask.Props : LayerMasks.Mask.Room);
        }

        public void DisableInteractTrigger(bool disable = true)
        {
            EnableInteractTrigger(!disable);
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
            OnGoombaClientRpc(playerID);
        }

        [ClientRpc]
        public void OnGoombaClientRpc(ulong playerID)
        {
            var currentPlayer = PlayerInfo.CurrentPlayer;
            if(currentPlayer.playerClientId == playerID)
                Plugin.Log("WE GETTING GOOMBAD");
            else
                Plugin.Log("A goomba...... stompin' on player " + playerID);
            coroutines.GoombaStomp.StartRoutine(PlayerInfo.ControllerFromID(playerID).gameObject, () =>
            {
                if (playerID == currentPlayer.playerClientId)
                    IsGoombaCoroutineRunning = false;
            });
        }
        
        private PlayerControllerB GetPlayerAbove()
        {
            // Cast a ray upwards to check for the player above
            RaycastHit hit;
            if (Physics.Raycast(StartOfRound.Instance.localPlayerController.gameplayCamera.transform.position, StartOfRound.Instance.localPlayerController.gameObject.transform.up, out hit, 1f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
            {
                // todo: check if getting held by that player to avoid eternal stomping
                return hit.collider.gameObject.GetComponent<PlayerControllerB>();
            }

            return null;
        }

        private void CheckForGoomba()
        {
            if (IsGoombaCoroutineRunning)
                return; // Already running

            if (!ModConfig.Instance.values.jumpOnShrunkenPlayers)
                return;

            if (PlayerInfo.IsCurrentPlayerGrabbed())
            {
                //Plugin.log("Apes together strong! Goomba impossible.");
                return;
            }

            var playerAbove = GetPlayerAbove();
            if (playerAbove == null)
                return;

            var currentPlayer = PlayerInfo.CurrentPlayer;
            if (currentPlayer.gameObject.transform.localScale.x >= playerAbove.gameObject.transform.localScale.x)
            {
                //Plugin.log("2 Weak 2 Goomba c:");
                return;
            }

            IsGoombaCoroutineRunning = true;

            OnGoombaServerRpc(currentPlayer.playerClientId);
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
            if (!IsCurrentPlayer) return; // Only do this from the perspective of the currently held player, not the holder himself

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(playerHeldBy.playerClientId, out GrabbablePlayerObject gpo ))
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
                    string[] tips = {"Ungrab: JUMP"};
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

        [ServerRpc(RequireOwnership = false)]
        internal void PlaceOnSellCounterServerRpc()
        {
            IsOnSellCounter.Value = true;
            if (IsCurrentPlayer && PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = false;
        }

        [ServerRpc(RequireOwnership = false)]
        internal void RemoveFromSellCounterServerRpc()
        {
            IsOnSellCounter.Value = false;

            if (IsCurrentPlayer && PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = true;
        }

        internal bool CanSellKill()
        {
            return IsOnSellCounter.Value && IsCurrentPlayer;
        }
        #endregion

        #region EntranceTeleportFix
        [ServerRpc(RequireOwnership = false)]
        private void UpdateAfterTeleportServerRpc(TargetPlayer teleportingPlayer)
        {
            UpdateAfterTeleportClientRpc(teleportingPlayer);
        }

        internal IEnumerator UpdateAfterTeleportEnsured(TargetPlayer teleportingPlayer)
        {
            if (grabbedPlayer == null || playerHeldBy == null)
                yield break;

            yield return null; // Wait for next frame
            UpdateAfterTeleportServerRpc(teleportingPlayer); // Let the other person of this holder/grabbed connection know that they teleported with us
        }

        [ClientRpc]
        private void UpdateAfterTeleportClientRpc(TargetPlayer teleportingPlayer)
        {
            Plugin.Log("UpdateRegionClientRpc -> " + teleportingPlayer.ToString());

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
