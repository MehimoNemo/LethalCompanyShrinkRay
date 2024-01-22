using GameNetcodeStuff;
using LCShrinkRay.Config;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using LCShrinkRay.helper;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;
using Newtonsoft.Json;
using LCShrinkRay.coroutines;
using LethalLib.Modules;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        public PlayerControllerB grabbedPlayer { get; set; }
        private ulong grabbedPlayerID {  get; set; }
        MeshRenderer helmet;

        public static GameObject networkPrefab { get; set; }
        public bool IsFrozen { get; private set; }

        public static void LoadAsset(AssetBundle assetBundle)
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = assetBundle.LoadAsset<Item>("grabbablePlayerItem.asset");
            if (assetItem == null)
                Plugin.log("\n\nFUCK WHY IS IT NULL???\n\n");

            networkPrefab = assetItem.spawnPrefab;

            var component = networkPrefab.AddComponent<GrabbablePlayerObject>();

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            component.itemProperties = assetItem;
            if (component.itemProperties == null)
            {
                Plugin.log("\n\nSHIT HOW IS IT NULL???\n\n");
            }
            component.itemProperties.isConductiveMetal = false;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        //Null player container and null itemProperties
        //okay gonna do stuff good :)

        public override void Start()
        {
            base.Start();
            //GameObject itemPrefabInstance = Instantiate(itemProperties.spawnPrefab, transform.position, Quaternion.identity);
            //itemPrefabInstance.transform.parent = transform;
            //this.transform.localPosition = Vector3.zero;
            //this.gameObject.GetComponent<CapsuleCollider>().isTrigger = true;
            //this.gameObject.layer = 6;
            this.itemProperties.canBeGrabbedBeforeGameStart = true;
            this.itemProperties.positionOffset = new Vector3(-0.5f, 0.1f, 0f);
            this.grabbable = true;

            //get our collider and save it for later use

            /*Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }*/

        }

        public void AddNode()
        {
            //Plugin.log("adding scannode");
        }

        public void calculateScrapValue()
        {
            // todo: change scrap value when grabbed player grabs something
            int value = 5; // todo: find where that's set in code for deadBody

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
            Plugin.log("Scrap value: " + value);
        }

        public void setIsGrabbableToEnemies(bool isGrabbable = true)
        {
            if (!PlayerHelper.isCurrentPlayerShrunk())
                isGrabbable = false;

            this.grabbableToEnemies = isGrabbable;

            Plugin.log("GrabbablePlayer - Allow enemy grab: " + isGrabbable);

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

        // WIP - not working
        [ServerRpc(RequireOwnership = false)]
        public void DemandDropFromPlayerServerRpc(ulong holdingPlayerID, ulong heldPlayerID)
        {
            DemandDropFromPlayerClientRpc(holdingPlayerID, heldPlayerID);
        }

        [ClientRpc]
        public void DemandDropFromPlayerClientRpc(ulong holdingPlayerID, ulong heldPlayerID)
        {
            var currentPlayerID = PlayerHelper.currentPlayer().playerClientId;
            var whoDemandedIt = (currentPlayerID == heldPlayerID) ? "A player" : "You";
            if (currentPlayerID == holdingPlayerID)
            {
                Plugin.log("Player " + heldPlayerID + " demanded to be dropped from you .. so it shall be!");
                StartOfRound.Instance.localPlayerController.DiscardHeldObject();
            }
            else if (currentPlayerID == heldPlayerID)
                Plugin.log("You demanded to be dropped from player " + holdingPlayerID);
            else
                Plugin.log("Player " + heldPlayerID + " demanded to be dropped from player " + currentPlayerID + ".");
        }

        // GOOMBA
        [ServerRpc(RequireOwnership = false)]
        public void OnGoombaServerRpc(ulong playerID)
        {
            OnGoombaClientRpc(playerID);
        }

        [ClientRpc]
        public void OnGoombaClientRpc(ulong playerID)
        {
            var currentPlayer = PlayerHelper.currentPlayer();
            if(currentPlayer.playerClientId == playerID)
                Plugin.log("WE GETTING GOOMBAD");
            else
                Plugin.log("A goomba...... stompin' on player " + playerID);
            coroutines.GoombaStomp.StartRoutine(PlayerHelper.GetPlayerObject(playerID), () =>
            {
                if (playerID == currentPlayer.playerClientId)
                    isGoombaCoroutineRunning = false;
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

        private bool isGoombaCoroutineRunning = false;
        private void CheckForGoomba()
        {
            if (isGoombaCoroutineRunning)
                return; // Already running

            if (!ModConfig.Instance.values.jumpOnShrunkenPlayers)
                return;

            if (PlayerHelper.IsCurrentPlayerGrabbed())
            {
                //Plugin.log("Apes together strong! Goomba impossible.");
                return;
            }

            var playerAbove = GetPlayerAbove();
            if (playerAbove == null)
                return;

            var currentPlayer = PlayerHelper.currentPlayer();
            if (currentPlayer.gameObject.transform.localScale.x >= playerAbove.gameObject.transform.localScale.x)
            {
                //Plugin.log("2 Weak 2 Goomba c:");
                return;
            }

            isGoombaCoroutineRunning = true;

            OnGoombaServerRpc(currentPlayer.playerClientId);
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            if (grabbedPlayer != null)
            {
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

                if(!base.IsOwner)
                {
                    if (playerHeldBy != null && ModConfig.Instance.values.CanEscapeGrab && Keyboard.current.spaceKey.wasPressedThisFrame)
                        DemandDropFromPlayerServerRpc(playerHeldBy.playerClientId, grabbedPlayerID);
                }
            }
            else
            {
                Plugin.log("GRABBED PLAYER IS NULL IN UPDATE", Plugin.LogType.Error);
            }
        }

        public override void PocketItem()
        {
            //drop the player if we attempt to pocket them
            //base.PocketItem();
            this.DiscardItem();
        }

        private GrabbableObject grabbedPlayerCurrentItem()
        {
            if (grabbedPlayer == null)
            {
                Plugin.log("GrabbedPlayer is null?!", Plugin.LogType.Error);
                return null;
            }

            if (grabbedPlayer.isHoldingObject && grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot] != null)
                return grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot];

            return null;
        }

        public override void GrabItem()
        {
            Plugin.log("Okay, let's grab!");
            base.GrabItem();

            grabbedPlayer.playerCollider.enabled = false;
            this.propColliders[0].enabled = false;
            grabbedPlayer.playerRigidbody.detectCollisions = false;
            if (helmet != null)
                helmet.enabled = false;
            
            setIsGrabbableToEnemies(false);
            setControlTips();

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if(grabbedPlayer != player)
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

            if(!ModConfig.Instance.values.friendlyFlight)
                setHolderGrabbable(false);
        }

        private void setHolderGrabbable(bool isGrabbable = true)
        {
            var gpo = GrabbablePlayerList.findGrabbableObjectForPlayer(playerHeldBy.playerClientId);
            if (gpo != null)
                gpo.grabbable = isGrabbable;
        }

        private void setControlTips()
        {
            HUDManager.Instance.ClearControlTips();

            if (playerHeldBy == null)
                return;

            Plugin.log("setControlTips");
            if (base.IsOwner)
            {
                HUDManager.Instance.ChangeControlTipMultiple(["Throw player: LMB"], holdingItem: true, itemProperties);
            }
            else if (!ModConfig.Instance.values.CanEscapeGrab)
                return;
            else
            {
                var grabbedPlayerItem = grabbedPlayerCurrentItem();
                if (grabbedPlayerItem != null) // only case that's not working so far!
                {
                    var toolTips = grabbedPlayerItem.itemProperties.toolTips.ToList();
                    toolTips.Add("Ungrab : JUMP");
                    HUDManager.Instance.ChangeControlTipMultiple(toolTips.ToArray(), holdingItem: true, grabbedPlayerItem.itemProperties);
                }
                else
                    HUDManager.Instance.ChangeControlTipMultiple(["Ungrab: JUMP"], holdingItem: false, itemProperties);
            }
        }

        private void resetControlTips()
        {
            if (base.IsOwner)
            {
                Plugin.log("IsOwner");
                return; // happens automatically
            }

            HUDManager.Instance.ClearControlTips();

            Plugin.log("resetControlTips");

            var grabbedPlayerItem = grabbedPlayerCurrentItem();
            if (grabbedPlayerItem != null)
                HUDManager.Instance.ChangeControlTipMultiple(grabbedPlayerItem.itemProperties.toolTips, holdingItem: true, grabbedPlayerItem.itemProperties);
        }

        public override void OnPlaceObject()
        {
            base.OnPlaceObject();

            resetControlTips();
        }

        private Vector3 throwDestination()
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

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.log("Player yeet");
                base.ItemActivate(used, buttonDown);

                playerHeldBy.DiscardHeldObject(placeObject: true, null, throwDestination());
                grabbedPlayer.playerCollider.enabled = true;
                setIsGrabbableToEnemies(true);
            }
            catch (Exception e)
            {
                Plugin.log("Error while yeeting player: " + e.Message);
            }
        }

        public override void DiscardItem()
        {
            if (!ModConfig.Instance.values.friendlyFlight)
                setHolderGrabbable(true);

            base.DiscardItem();
            grabbedPlayer.playerCollider.enabled = true;
            this.propColliders[0].enabled = true;
            grabbedPlayer.playerRigidbody.detectCollisions = false;
            if (helmet != null)
            {
                helmet.enabled = true;
            }
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

            setIsGrabbableToEnemies(true);
            resetControlTips();
        }

        public void Initialize(PlayerControllerB pcb)
        {
            Plugin.log("GrabbablePlayerObject.Initialize");

            if (pcb == StartOfRound.Instance.localPlayerController)
            {
                Plugin.log("Finding helmet!");
                try
                {
                    helmet = Shrinking.Instance.helmetHudTransform.gameObject.GetComponent<MeshRenderer>();
                } catch { }
                if(helmet == null)
                {
                    Plugin.log("uhhh helmet is null...");
                }
                if (helmet != null)
                {
                    Plugin.log("Found helmet");
                }
            }
            this.grabbedPlayer = pcb;
            this.grabbedPlayerID = pcb.playerClientId;
            this.tag = "PhysicsProp";
            if (grabbedPlayer.name != null)
            {
                this.name = "grabbable_" + grabbedPlayer.name;
                Plugin.log("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId + "]");
                this.grabbable = true;
            }
            else
            {
                Plugin.log("grabbedPlayer has no name!", Plugin.LogType.Error);
            }

            calculateScrapValue();
            setIsGrabbableToEnemies(true);
        }

        public void Reinitialize()
        {
            if (this.grabbedPlayer == null)
            {
                Plugin.log("Reinitializing grabbable player object with ID: " + grabbedPlayerID);
                this.grabbedPlayer = PlayerHelper.GetPlayerController(grabbedPlayerID);
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
            if (helmet != null)
                helmet.enabled = false;
        }


        internal void Unfreeze()
        {
            UnfreezePlayerServerRPC(grabbedPlayer.playerClientId);
            if (helmet != null)
                helmet.enabled = true;
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
            //grabbedPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
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
    }
}
