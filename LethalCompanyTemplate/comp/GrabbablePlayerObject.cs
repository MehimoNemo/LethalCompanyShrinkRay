using GameNetcodeStuff;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using LC_API.Networking;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        private int customGrabTextIndex = -1;

        public PlayerControllerB grabbedPlayer;

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

            setIsGrabbableToEnemies(true);

            //get our collider and save it for later use

            /*Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }*/

        }

        private void calculateScrapValue()
        {
            int value = 5; // todo: find where that's set in code for deadBody

            if (grabbedPlayer != null && grabbedPlayer.ItemSlots != null)
            {
                foreach (var item in grabbedPlayer.ItemSlots)
                    if (item != null)
                        value += item.scrapValue;
            }
            SetScrapValue(value);
        }

        private void setIsGrabbableToEnemies(bool isGrabbable = true)
        {
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
                this.grabbable = Shrinking.isShrunk(grabbedPlayer.gameObject);

                if (this.isHeld)
                {
                    if(customGrabTextIndex == -1)
                        setControlTipText();

                    //this looks like trash unfortunately
                    grabbedPlayer.transform.position = this.transform.position;
                    //change this
                    Vector3 targetPosition = playerHeldBy.localItemHolder.transform.position;
                    Vector3 targetUp = -(grabbedPlayer.transform.position - targetPosition).normalized;
                    Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp) * grabbedPlayer.transform.rotation;
                    //Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp);
                    grabbedPlayer.transform.rotation = Quaternion.Slerp(grabbedPlayer.transform.rotation, targetRotation, 50 * Time.deltaTime);
                    grabbedPlayer.playerCollider.enabled = false;

                    if (Keyboard.current.spaceKey.wasPressedThisFrame && playerHeldBy != null)
                    {
                        Plugin.log("Player demands to be dropped!");
                        Network.Broadcast("DemandDropFromPlayer", playerHeldBy.playerClientId.ToString());
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

        public override void GrabItem()
        {
            if (grabbedPlayer != playerHeldBy &&
                ModConfig.Instance.values.friendlyFlight || (grabbedPlayer.currentlyHeldObject != null && grabbedPlayer.currentlyHeldObject.GetType() != typeof(GrabbablePlayerObject)))
            {
                base.GrabItem();
                grabbedPlayer.playerCollider.enabled = false;
                this.propColliders[0].enabled = false;
                grabbedPlayer.playerRigidbody.detectCollisions = false;
            }
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

            setIsGrabbableToEnemies(false);
            setControlTipText();
        }

        public void removeControlTipText()
        {
            if (HUDManager.Instance == null || HUDManager.Instance.controlTipLines == null)
            {
                Plugin.log("Unable to set control tooltip for ungrabbing. No HUDManager instance", Plugin.LogType.Error);
                return;
            }

            if (customGrabTextIndex == -1)
                return; // Nothing to remove

            HUDManager.Instance.ChangeControlTip(customGrabTextIndex, "");

            // remove at index
            var tmpList = new List<TextMeshProUGUI>(HUDManager.Instance.controlTipLines);
            tmpList.RemoveAt(customGrabTextIndex);
            HUDManager.Instance.controlTipLines = tmpList.ToArray();

            customGrabTextIndex = -1;
        }

        public void setControlTipText()
        {
            if (HUDManager.Instance == null || HUDManager.Instance.controlTipLines == null)
            {
                Plugin.log("Unable to set control tooltip for ungrabbing. No HUDManager instance", Plugin.LogType.Error);
                return;
            }

            if (customGrabTextIndex == -1) // No text yet
            {
                var ungrabTextElement = new TextMeshProUGUI();
                HUDManager.Instance.controlTipLines.Append(ungrabTextElement);
                customGrabTextIndex = HUDManager.Instance.controlTipLines.Length - 1;
            }

            if(base.isHeld)
                HUDManager.Instance.ChangeControlTip(customGrabTextIndex, "Ungrab: JUMP");
            else
                HUDManager.Instance.ChangeControlTip(customGrabTextIndex, "Yeet player: LMB");
        }

        public override void OnPlaceObject()
        {
            base.OnPlaceObject();
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
                removeControlTipText();
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
            removeControlTipText();
        }

        public void Initialize(PlayerControllerB pcb)
        {
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
        }

    }
}
