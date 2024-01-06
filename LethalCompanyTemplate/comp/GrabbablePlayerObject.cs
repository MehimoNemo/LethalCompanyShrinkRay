using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
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
            if (grabbedPlayer != playerHeldBy)
            {
                base.GrabItem();
                grabbedPlayer.playerCollider.enabled = false;
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
        }

        public void Initialize(PlayerControllerB pcb)
        {
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
