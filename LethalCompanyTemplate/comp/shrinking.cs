using System;
using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;

using LCShrinkRay.patches;
using LCShrinkRay.Config;
using Unity.Netcode;
using System.Linq;
using Newtonsoft.Json;
using LCShrinkRay.helper;
using LethalLib.Modules;

namespace LCShrinkRay.comp
{
    internal class Shrinking
    {
        private static Shrinking instance = null;
        private static readonly object padlock = new object();

        public static Shrinking Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new Shrinking();

                    return instance;
                }
            }
        }

        public Transform helmetHudTransform;
        public static List<GameObject> grabbables = new List<GameObject>();
        public static List<GrabbableObject> alteredGrabbedItems = new List<GrabbableObject>();

        private string[] ScreenBlockingItems = {
            "Boombox(Clone)",
            "LungApparatus(Clone)",
            "FancyLamp(Clone)",
            "ChemicalJug(Clone)",
            "ExtensionLadderItem(Clone)",
            "BinFullOfBottles(Clone)",
            "TeaKettle(Clone)",
            "Painting(Clone)",
            "RobotToy(Clone)",
            "EnginePart(Clone)",
            "RedLocustHive(Clone)",
            "CashRegisterItem(Clone)",
            "Cog(Clone)",
            "Player"
        };

        public void setup()
        {
            // a list of itemnames to change
            //boombox
            //ladder
            //v-type engine
            //large Axle
            //bottles Done
            //chemical jug
            //apparatus(lung)
            //bee hive
            //cash register
            //robot
            //teapot
            //lamp
            //metal sheet     NOT ADDED
            //player(soon)

            Plugin.log("COUNT OF LIST IS: " + ScreenBlockingItems.Length);
            foreach (string item in ScreenBlockingItems)
                Plugin.log('\"' + item + '\"');
        }

        public void Update()
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            var players = PlayerHelper.getAllPlayers();
            if (players == null)
                return;

            foreach (GameObject player in players)
            {
                //TODO: REPLACE WITH OBJECT REFERENCE
                PlayerControllerB playerController = player.GetComponent<PlayerControllerB>();
                if (playerController == null)
                    Plugin.log("playerController is fucking null goddamnit", Plugin.LogType.Warning);

                if (!playerController.isHoldingObject)
                    continue;

                GrabbableObject heldObject = playerController.ItemSlots[playerController.currentItemSlot];
                if (heldObject == null)
                {
                    Plugin.log("HELD OBJECT IS NULL", Plugin.LogType.Warning);
                    continue;
                }

                bool isInList = ScreenBlockingItems.Where(item => item.Equals(heldObject.name)).Any();

                if (!hasIDInList(heldObject.itemProperties.itemId, alteredGrabbedItems) && isInList)
                {
                    alteredGrabbedItems.Add(heldObject);
                    //TODO: REPLACE WITH OBJECT REFERENCE
                    float scale = player.GetComponent<Transform>().localScale.x;
                    float y = 0f;
                    float z = 0f;
                    float x = 0f;
                    if (!player.gameObject.name.Contains(GameNetworkManager.Instance.localPlayerController.gameObject.name))
                    {
                        y = -0.42f * scale + 0.42f;
                        z = 0f;
                        x = 0.8f * scale - 0.8f;
                        Plugin.log("we is not da client");
                    }
                    else
                    {
                        y = 0.3f * scale - 0.3f;
                        z = -1.44f * scale + 1.44f;
                        x = 0.3f * scale - 0.3f;
                        Plugin.log("we IS da client");
                    }

                    //inverted even though my math was perfect but okay
                    Vector3 posOffsetVect = new Vector3(-x, -y, -z);

                    coroutines.TranslateRelativeOffset.StartRoutine(playerController.playerEye, heldObject, posOffsetVect);
                    //First person engine offset is -0.5099 0.7197 -0.1828 with these numbas

                    //Third person offset should be 0.2099 0.5197 -0.1828
                    //at least on the engine it should be...
                }
            }

            //Remove the item from the list of altered items and reset them if they're not being held. todo: move this to GrabItem / DiscardItem in GrabbablePlayerObject
            for(int i = alteredGrabbedItems.Count - 1; i >= 0; i--)
            {
                var obj = alteredGrabbedItems[i];
                if (!obj.isHeld)
                {
                    Plugin.log("removing held object!!! from the list!!!!!");
                    obj.itemProperties.positionOffset = new Vector3(0, 0, 0);
                    alteredGrabbedItems.Remove(obj);
                }
            }

            if (helmetHudTransform == null)
            {
                if (GameObject.Find("ScavengerHelmet") != null)
                {
                    helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                    helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                    Plugin.log("Player transform got!");
                }
            }
        }

        private static bool hasIDInList(int itemId, List<GrabbableObject> alteredGrabbedItems)
        {
            return alteredGrabbedItems.Where(item => item.itemProperties.itemId == itemId).Any();
        }

        
    }
}
