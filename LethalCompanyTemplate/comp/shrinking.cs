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

        public Transform playerTransform, player1Transform; // needed?

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


        // Multiplayer Networking

        

        private static bool isGoombaCoroutineRunning = false;

        [ServerRpc(RequireOwnership = false)]
        public static void OnGoombaServerRpc(ulong playerID)
        {
            Plugin.log("WE GETTING GOOMBAD");
            OnGoombaClientRpc(playerID);
        }

        [ClientRpc]
        public static void OnGoombaClientRpc(ulong playerID)
        {
            Plugin.log("A goomba...... stompin' on player " + playerID);
            coroutines.GoombaStomp.StartRoutine(PlayerHelper.GetPlayerObject(playerID));
        }

        public void OnGoombaCoroutineComplete()
        {
            isGoombaCoroutineRunning = false;
        }

        private static PlayerControllerB GetPlayerAbove()
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

        private static void CheckForGoomba()
        {
            if (!ModConfig.Instance.values.jumpOnShrunkenPlayers || !PlayerHelper.isCurrentPlayerShrunk())
                return;

            if (isGoombaCoroutineRunning)
                return;

            if(PlayerHelper.IsCurrentPlayerGrabbed())
            {
                //Plugin.log("Apes together strong! Goomba impossible.");
                return;
            }

            var playerAbove = GetPlayerAbove();
            if (playerAbove == null)
                return;

            if(PlayerHelper.isShrunk(playerAbove.gameObject))
            {
                //Plugin.log("2 Weak 2 Goomba c:");
                return;
            }

            OnGoombaServerRpc(PlayerHelper.currentPlayer().playerClientId);
            isGoombaCoroutineRunning = true;
        }

        public void SetPlayerPitch(float pitch, ulong playerID)
        {
            coroutines.SetPlayerPitch.StartRoutine(playerID, pitch);
        }

        public void Update()
        {
            var players = PlayerHelper.getAllPlayers();
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController || players == null)
                return;

            CheckForGoomba();

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

            //Remove the item from the list of altered items and reset them if they're not being held
            foreach (GrabbableObject obj in alteredGrabbedItems)
            {
                if (!obj.isHeld)
                {
                    Plugin.log("removing held object!!! from the list!!!!!");
                    obj.itemProperties.positionOffset = new Vector3(0, 0, 0);
                    alteredGrabbedItems.Remove(obj);
                }
            }

            if (playerTransform == null) // needed?
            {
                try
                {
                    //TODO: REPLACE WITH STORED REFERENCE
                    var player = GameObject.Find("Player");
                    if (player != null)
                        playerTransform = player.GetComponent<Transform>();

                    //TODO: REPLACE WITH STORED REFERENCE
                    if (GameObject.Find("ScavengerHelmet") != null)
                    {
                        //TODO: REPLACE WITH STORED REFERENCE
                        helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                        helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                        Plugin.log("Player transform got!");
                    }
                    var player1Object = GameObject.Find("Player (1)");
                    if (player1Object != null)
                        player1Transform = player1Object.GetComponent<Transform>();
                }
                catch (Exception e)
                {
                    Plugin.log("Error in Update(): " + e.Message);
                }
            }
        }

        public void updatePitch()
        {
            foreach ( var pcb in StartOfRound.Instance.allPlayerScripts.Where(p => p != null))
            {
                Plugin.log("Altering player voice pitches");
                SetPlayerPitch(1f, pcb.playerClientId);
            }
        }

        private static bool hasIDInList(int itemId, List<GrabbableObject> alteredGrabbedItems)
        {
            return alteredGrabbedItems.Where(item => item.itemProperties.itemId == itemId).Any();
        }

        
    }
}
