using GameNetcodeStuff;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using LC_API.Networking;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using LCShrinkRay.helper;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        public PlayerControllerB grabbedPlayer { get; set; }
        MeshRenderer helmet;

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
            this.grabbable = false;

            calculateScrapValue();
            //itemProperties.weight = PlayerHelper.calculatePlayerWeightFor(grabbedPlayer);

            setIsGrabbableToEnemies(true);

            //get our collider and save it for later use

            /*Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }*/

        }

        public void AddNode()
        {
            /*Plugin.log("adding scannode");
        }

        private void calculateScrapValue()
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

        private void setIsGrabbableToEnemies(bool isGrabbable = true)
        {
            if (!PlayerHelper.isCurrentPlayerShrunk())
                isGrabbable = false;

            this.grabbableToEnemies = isGrabbable;

            Plugin.log("GrabbablePlayer - Allow enemy grab: " + isGrabbable);

            if (ModConfig.Instance.values.hoardingBugSteal)
            {
                if(isGrabbable)
                {
                    if (HoarderBugAI.grabbableObjectsInMap != null && !HoarderBugAI.grabbableObjectsInMap.Contains(base.gameObject))
                        HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
                }
                else
                {
                    if (HoarderBugAI.grabbableObjectsInMap != null && HoarderBugAI.grabbableObjectsInMap.Contains(base.gameObject))
                        HoarderBugAI.grabbableObjectsInMap.Remove(base.gameObject);
                }
            }
        }

        [NetworkMessage("DemandDropFromPlayer")]
        public static void DemandDropFromPlayer(ulong sender, string playerID)
        {
            Plugin.log("A player demands to be dropped from player " + playerID);
            if (StartOfRound.Instance.localPlayerController.playerClientId == ulong.Parse(playerID)) // I have to drop him...
            {
                Plugin.log("I have to drop them... sadly!", Plugin.LogType.Warning);
                StartOfRound.Instance.localPlayerController.DiscardHeldObject();
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            if (grabbedPlayer != null)
            {
                this.grabbable = PlayerHelper.isShrunk(grabbedPlayer.gameObject);

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

                    if (playerHeldBy != null && ModConfig.Instance.values.CanEscapeGrab && Keyboard.current.spaceKey.wasPressedThisFrame)
                    {
                        Plugin.log("Player demands to be dropped!");
                        Network.Broadcast("DemandDropFromPlayer", playerHeldBy.playerClientId.ToString()); // PlayerControllerBPatch
                    }
                }
                else
                {
                    this.transform.position = grabbedPlayer.transform.position;
                }
            }
            else
            {
                //mls.LogError("GRABBED PLAYER IS NULL IN UPDATE");
            }
                //base.Update();
        }

        public override void PocketItem()
        {
            //drop the player if we attempt to pocket them
            //base.PocketItem();
            this.DiscardItem();
        }

        private GrabbableObject grabbedPlayerCurrentItem()
        {
            if (grabbedPlayer.isHoldingObject && grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot] != null)
                return grabbedPlayer.ItemSlots[grabbedPlayer.currentItemSlot];

            return null;
        }

        private bool isHoldingPlayer()
        {
            Plugin.log("CHECKING IF HELD OBJECT IS PLAYER OBJECT");
            var item = grabbedPlayerCurrentItem();
            return item != null && item is GrabbablePlayerObject;
        }

        public override void GrabItem()
        {
            var holdingPlayer = isHoldingPlayer();
            if (grabbedPlayer == playerHeldBy || (!ModConfig.Instance.values.friendlyFlight && holdingPlayer))
            {
                Plugin.log("Unable to grab player " + grabbedPlayer.ToString());
                playerHeldBy.DiscardHeldObject();
                DiscardItem();
                return;
            }

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
        }

        private void setControlTips()
        {
            HUDManager.Instance.ClearControlTips();

            Plugin.log("setControlTips");
            if (base.IsOwner)
            {
                Plugin.log("IsOwner");
                string[] toolTips = { "Throw player: LMB" };
                HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, itemProperties);
            }
            else if (!ModConfig.Instance.values.CanEscapeGrab)
                return;
            else
            {
                var grabbedPlayerItem = grabbedPlayerCurrentItem();
                if (grabbedPlayerItem != null) // only case that's not working so far!
                {
                    string[] toolTips = grabbedPlayerItem.itemProperties.toolTips;
                    /*string test = "toolTips: ";
                    for (int i = 0; i < toolTips.Length; i++)
                        test += "\n" + toolTips[i];
                    Plugin.log(test);*/

                    toolTips.Append("Ungrab: JUMP");
                    HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, grabbedPlayerItem.itemProperties);
                }
                else
                {
                    string[] toolTips = { "Ungrab: JUMP" };
                    HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: false, itemProperties);
                }
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
            {
                string[] toolTips = grabbedPlayerItem.itemProperties.toolTips;
                Plugin.log("tooltips: " + toolTips.ToString());
                HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, grabbedPlayerItem.itemProperties);
            }
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
    }
}
