using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerObject : GrabbableObject
    {
        private ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
        public PlayerControllerB grabbedPlayer;
        GameObject playerContainer;
        int grabbedPlayerNum;

        //Null player container and null itemProperties

        public override void Start()
        {
            base.Start();
            //GameObject itemPrefabInstance = Instantiate(itemProperties.spawnPrefab, transform.position, Quaternion.identity);
            //itemPrefabInstance.transform.parent = transform;
            this.transform.localPosition = Vector3.zero;
            this.gameObject.GetComponent<CapsuleCollider>().isTrigger = true;
            this.gameObject.layer = 6;
            this.itemProperties.canBeGrabbedBeforeGameStart = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        public override void Update()
        {
            if (grabbedPlayer != null)
            {
                /* commented out for testing
                if(grabbedPlayer.gameObject.transform.localScale.x < 0.5)
                {
                    this.grabbable = true;
                }
                else
                {
                    this.grabbable = false;
                }*/
                if (this.isHeld)
                {

                    //get the grabbed players camera position and transform the player a lil over to the side, additionally, either disable the player collider, or change the player
                    //from the player layer to the prop layer

                    //grabbedPlayer.transform.position = this.transform.position;
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
            base.PocketItem();
            this.DiscardItem();
        }

        public override void GrabItem()
        {
            base.GrabItem();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();

        }

        public void Initialize(PlayerControllerB pcb)
        {
            this.grabbedPlayer = pcb;
            this.tag = "PhysicsProp";
            if (grabbedPlayer.name != null)
            {
                this.name = "grabbable" + grabbedPlayer.name;
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
