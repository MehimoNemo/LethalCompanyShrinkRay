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

        //Null player container and null itemProperties

        public override void Start()
        {
            GameObject itemPrefabInstance = Instantiate(itemProperties.spawnPrefab, transform.position, Quaternion.identity);
            itemPrefabInstance.transform.parent = transform;
            this.transform.localPosition = Vector3.zero;
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
                    grabbedPlayer.transform.position = this.transform.position;
                }
                else
                {
                    this.transform.position = grabbedPlayer.transform.position;
                }
            }
            else
            {
                mls.LogError("GRABBED PLAYER IS NULL IN UPDATE");
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
            //grabbedPlayer.gameObject.transform.parent = this.gameObject.transform;
            this.parentObject.parent = null;
            mls.LogMessage("parenting player number " + grabbedPlayer.playerClientId + " to grabbableObject");
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            grabbedPlayer.gameObject.transform.parent = playerContainer.transform;
            mls.LogMessage("Unparenting player number :[" + grabbedPlayer.playerClientId + "] from grabbableObject");
            this.gameObject.transform.parent = grabbedPlayer.gameObject.transform;
            mls.LogMessage("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId+"]");

        }

        public void CreateDebugCube()
        {
            GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(debugCube.GetComponent<Collider>());
            debugCube.transform.parent = transform;
            /*Material unlitMaterial = new Material(Shader.Find("HDRP/Unlit"));
            debugCube.GetComponent<Renderer>().material = unlitMaterial;
            debugCube.GetComponent<Renderer>().material.color = Color.red;*/
        }

        public void Initialize(PlayerControllerB pcb)
        {
            this.grabbedPlayer = pcb;
            this.propColliders[0] = grabbedPlayer.GetComponent<BoxCollider>();

            this.tag = "PhysicsProp";
            if (grabbedPlayer.name != null)
            {
                this.name = "grabbable" + grabbedPlayer.name;

                playerContainer = StartOfRound.Instance.playersContainer.gameObject;

                //this.transform.parent = playerContainer.transform;
                //this.transform.parent = grabbedPlayer.transform;
                mls.LogMessage("parenting grabbable object to player number :[" + grabbedPlayer.playerClientId + "]");
/*
                BoxCollider existingCollider = GetComponent<BoxCollider>();
                if (existingCollider == null)
                {
                    BoxCollider bc = this.gameObject.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                }*/
                //CreateDebugCube();
                this.grabbable = true;

                GameObject.Destroy(this.transform.parent.gameObject.GetComponentInChildren<CapsuleCollider>());
            }
            else
            {
                mls.LogError("grabbedPlayer has no name!");
            }
            
        }

    }
}
