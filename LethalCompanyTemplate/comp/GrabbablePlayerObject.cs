using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {

        private ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
        
        public PlayerControllerB grabbedPlayer;
        int grabbedPlayerNum;
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
            //get our collider and save it for later use

            /*Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }*/
            
        }

        public override void LateUpdate()
        {
            base.LateUpdate();


            if (grabbedPlayer != null)
            {
                
                if(grabbedPlayer.gameObject.transform.localScale.x < 0.5)
                {
                    this.grabbable = true;
                }
                else
                {
                    this.grabbable = false;
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
            bool isNotHoldingPlayer = true;
            if (grabbedPlayer.isHoldingObject && grabbedPlayer.currentlyHeldObject != null)
            {
                Plugin.log("CHECKING IF HELD OBJECT IS PLAYER OBJECT");
                isNotHoldingPlayer = grabbedPlayer.currentlyHeldObject is not GrabbablePlayerObject;
            }
            if (grabbedPlayer != playerHeldBy && (isNotHoldingPlayer || ModConfig.Instance.values.friendlyFlight))
            {
                base.GrabItem();
                grabbedPlayer.playerCollider.enabled = false;
                this.propColliders[0].enabled = false;
                grabbedPlayer.playerRigidbody.detectCollisions = false;
                if (helmet != null)
                {
                    helmet.enabled = false;
                }
            }
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
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
        }

        public override void OnPlaceObject()
        {
            //base.OnPlaceObject();
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
                mls.LogMessage("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId + "]");
                this.grabbable = true;
            }
            else
            {
                mls.LogError("grabbedPlayer has no name!");
            }
            

        }

    }
}
