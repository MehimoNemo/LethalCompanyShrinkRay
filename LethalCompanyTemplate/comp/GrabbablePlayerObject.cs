using GameNetcodeStuff;
using LCShrinkRay.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LCShrinkRay.helper;
using Unity.Netcode;
using LCShrinkRay.patches;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        #region Properties
        public PlayerControllerB grabbedPlayer { get; set; }
        private ulong grabbedPlayerID {  get; set; }

        private static GameObject networkPrefab { get; set; }
        public bool IsFrozen { get; private set; }

        private bool IsGoombaCoroutineRunning = false;
        #endregion

        #region Networking
        public static void LoadAsset(AssetBundle assetBundle)
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = assetBundle.LoadAsset<Item>("grabbablePlayerItem.asset");
            if (assetItem == null)
                Plugin.Log("\n\nFUCK WHY IS IT NULL???\n\n");

            networkPrefab = assetItem.spawnPrefab;

            var component = networkPrefab.AddComponent<GrabbablePlayerObject>();

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            component.itemProperties = assetItem;
            if (component.itemProperties == null)
            {
                Plugin.Log("\n\nSHIT HOW IS IT NULL???\n\n");
            }
            component.itemProperties.isConductiveMetal = false;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        public static NetworkObject Instantiate()
        {
            var obj = Instantiate(networkPrefab);
            DontDestroyOnLoad(obj);
            var networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn();
            obj.GetComponent<GrabbablePlayerObject>();
            return networkObj;
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();
            itemProperties.canBeGrabbedBeforeGameStart = true;
            itemProperties.positionOffset = new Vector3(-0.5f, 0.1f, 0f);
        }
        
        public override void LateUpdate()
        {
            base.LateUpdate();
            if (grabbedPlayer == null)
            {
                Plugin.Log("grabbedPlayer was null in LateUpdate");
                return;
            }

            if (this.isHeld)
            {
                //this looks like trash unfortunately
                grabbedPlayer.transform.position = this.transform.position;
                //change this
                Vector3 targetPosition = playerHeldBy.localItemHolder.transform.position;
                Vector3 targetUp = -(grabbedPlayer.transform.position - targetPosition).normalized;
                Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp) * grabbedPlayer.transform.rotation;
                //Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp);
                grabbedPlayer.transform.rotation = Quaternion.Slerp(grabbedPlayer.transform.rotation, targetRotation, 50 * Time.deltaTime);
                grabbedPlayer.playerCollider.enabled = false;
            }
            else if (IsFrozen)
            {
                grabbedPlayer.transform.position = this.transform.position;
            }
            else
            {
                this.transform.position = grabbedPlayer.transform.position;
                CheckForGoomba();
            }

            if (!base.IsOwner)
            {
                if (playerHeldBy != null && ModConfig.Instance.values.CanEscapeGrab && Keyboard.current.spaceKey.wasPressedThisFrame)
                    DemandDropFromPlayerServerRpc(playerHeldBy.playerClientId, grabbedPlayerID);
            }
        }
        
        public override void PocketItem()
        {
            //drop the player if we attempt to pocket them
            //base.PocketItem();
            this.DiscardItem();
        }
        
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.Log("Player yeet");
                base.ItemActivate(used, buttonDown);

                playerHeldBy.DiscardHeldObject(placeObject: true, null, ThrowDestination());
                grabbedPlayer.playerCollider.enabled = true;
                SetIsGrabbableToEnemies(true);
            }
            catch (Exception e)
            {
                Plugin.Log("Error while yeeting player: " + e.Message);
            }
        }
        
        public override void DiscardItem()
        {
            if (!ModConfig.Instance.values.friendlyFlight)
                SetHolderGrabbable(true);

            base.DiscardItem();
            grabbedPlayer.playerCollider.enabled = true;
            this.propColliders[0].enabled = true;
            grabbedPlayer.playerRigidbody.detectCollisions = false;

            if (PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = true;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (grabbedPlayer != player)
                {
                    Collider thisPlayerCollider = grabbedPlayer.playerCollider;
                    Collider thisCollider = this.propColliders[0];
                    Collider thatCollider = player.playerCollider;
                    Physics.IgnoreCollision(thisPlayerCollider, thatCollider, false);
                    Physics.IgnoreCollision(thisCollider, thatCollider, false);
                }
            }

            SetIsGrabbableToEnemies(true);
            ResetControlTips();
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
                {
                    Collider thisPlayerCollider = grabbedPlayer.playerCollider;
                    Collider thisCollider = this.propColliders[0];
                    Collider thatCollider = player.playerCollider;
                    Physics.IgnoreCollision(thisPlayerCollider, thatCollider);
                    Physics.IgnoreCollision(thisCollider, thatCollider);
                }
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
            base.OnPlaceObject();

            ResetControlTips();
        }
        #endregion

        #region Methods
        public void Initialize(PlayerControllerB pcb, bool isCurrentPlayer = false)
        {
            if(PlayerInfo.CurrentPlayerID == pcb.playerClientId)
                Plugin.Log("This is us: " + pcb.name);

            this.grabbable = !isCurrentPlayer;

            Plugin.Log("GrabbablePlayerObject.Initialize");
            Plugin.Log("Is current player: " + isCurrentPlayer);

            this.grabbedPlayer = pcb;
            this.grabbedPlayerID = pcb.playerClientId;
            this.tag = "PhysicsProp";
            if (grabbedPlayer.name != null)
            {
                this.name = "grabbable_" + grabbedPlayer.name;
                Plugin.Log("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId + "]");
                this.grabbable = true;
            }
            else
            {
                Plugin.Log("grabbedPlayer has no name!", Plugin.LogType.Error);
            }

            CalculateScrapValue();
            SetIsGrabbableToEnemies(true);
        }

        public void AddNode() {} // WIP

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

            var scanNode = base.gameObject.GetComponentInChildren<ScanNodeProperties>();
            if (scanNode == null)
                AddNode();

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

            this.grabbableToEnemies = isGrabbable;

            Plugin.Log("GrabbablePlayer - Allow enemy grab: " + isGrabbable);

            if (ModConfig.Instance.values.hoardingBugSteal)
            {
                if(isGrabbable)
                {
                    if (HoarderBugAI.grabbableObjectsInMap != null && !HoarderBugAI.grabbableObjectsInMap.Contains(grabbedPlayer.gameObject))
                        HoarderBugAI.grabbableObjectsInMap.Add(grabbedPlayer.gameObject);
                }
                else
                {
                    if (HoarderBugAI.grabbableObjectsInMap != null && HoarderBugAI.grabbableObjectsInMap.Contains(grabbedPlayer.gameObject))
                        HoarderBugAI.grabbableObjectsInMap.Remove(grabbedPlayer.gameObject);
                }
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
            var gpo = GrabbablePlayerList.FindGrabbableObjectForPlayer(playerHeldBy.playerClientId);
            if (gpo != null)
                gpo.grabbable = isGrabbable;
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

        private Vector3 ThrowDestination()
        {
            Vector3 position = transform.position;
            var playerThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            RaycastHit playerHit = default(RaycastHit);
            position = ((!Physics.Raycast(playerThrowRay, out playerHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) ? playerThrowRay.GetPoint(10f) : playerThrowRay.GetPoint(playerHit.distance - 0.05f));
            playerThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(playerThrowRay, out playerHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                return playerHit.point + Vector3.up * 0.05f;
            }
            return playerThrowRay.GetPoint(30f);
        }

        public void Reinitialize()
        {
            if (this.grabbedPlayer == null)
            {
                Plugin.Log("Reinitializing grabbable player object with ID: " + grabbedPlayerID);
                this.grabbedPlayer = PlayerInfo.ControllerFromID(grabbedPlayerID);
            }
        }

        internal void Freeze()
        {
            FreezePlayerServerRPC(grabbedPlayer.playerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void FreezePlayerServerRPC(ulong playerID)
        {
            FreezePlayerClientRpc(playerID);
        }

        [ClientRpc]
        public void FreezePlayerClientRpc(ulong playerID)
        {
            this.IsFrozen = true;
            if (PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = false;
        }

        internal void Unfreeze()
        {
            UnfreezePlayerServerRPC(grabbedPlayer.playerClientId);
            if (PlayerModificationPatch.helmetRenderer != null)
                PlayerModificationPatch.helmetRenderer.enabled = true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnfreezePlayerServerRPC(ulong playerID)
        {
            UnfreezePlayerClientRpc(playerID);
        }

        [ClientRpc]
        public void UnfreezePlayerClientRpc(ulong playerID)
        {
            this.IsFrozen = false;
        }

        internal void SellKill()
        {
            SellKillServerRPC(grabbedPlayer.playerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SellKillServerRPC(ulong playerID)
        {
            SellKillClientRpc(playerID);
        }

        [ClientRpc]
        public void SellKillClientRpc(ulong playerID)
        {
            grabbedPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
        }
        #endregion
    }
}
