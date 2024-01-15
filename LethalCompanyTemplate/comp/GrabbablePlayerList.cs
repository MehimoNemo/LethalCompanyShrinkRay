using GameNetcodeStuff;
using LC_API.Networking;
using LCShrinkRay.helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerList
    {
        public static List<GameObject> grabbablePlayerObjects = new List<GameObject>();


        // ---- Networking ----
        [NetworkMessage("AddGrabbablePlayer")]
        public static void AddGrabbablePlayer(ulong sender, string playerID)
        {
            Plugin.log("AddGrabbablePlayer -> sender: " + sender.ToString() + " / playerID: " + playerID);
            if (!NetworkManager.Singleton.IsServer)
                return;

            var pcb = PlayerHelper.GetPlayerController(sender);
            if (pcb == null)
            {
                Plugin.log("Unable to add grabbable player. Player with ID " + playerID + " not found!", Plugin.LogType.Error);
                return;
            }

            SetPlayerGrabbable(pcb);
        }

        internal class GrabbablePlayerListSyncData
        {
            public List<(ulong, ulong)> grabbablePlayerIDS {  get; set; }
            public ulong? singleReceiver { get; set; } = null;
        }

        [NetworkMessage("GrabbablePlayerListSync")]
        public static void GrabbablePlayerListSync(ulong sender, GrabbablePlayerListSyncData data)
        {
            if (data.singleReceiver != null && PlayerHelper.currentPlayer().playerClientId != data.singleReceiver)
                return; // not meant for us

            Plugin.log("Oh hey!! There's that list I needed!!!!", Plugin.LogType.Error);
            Plugin.log("\t" + TupleListToString(data.grabbablePlayerIDS));
            GrabbablePlayerList.UpdateGrabbablePlayerObjectsClient(data.grabbablePlayerIDS);
        }

        // ---- Helper ----
        private static string TupleListToString(List<(ulong, ulong)> tupleList)
        {
            return JsonConvert.SerializeObject(tupleList);
        }

        private static List<(ulong, ulong)> StringToTupleList(string jsonString)
        {
            return JsonConvert.DeserializeObject<List<(ulong, ulong)>>(jsonString);
        }

        public static GrabbablePlayerObject findGrabbableObjectForPlayer(PlayerControllerB pcb)
        {
            return findGrabbableObjectForPlayer(pcb.playerClientId);
        }

        public static GrabbablePlayerObject findGrabbableObjectForPlayer(ulong playerID)
        {
            return GameObject.FindObjectsOfType<GrabbablePlayerObject>().FirstOrDefault(gpo => gpo.grabbedPlayer.playerClientId == playerID);
        }

        // ---- Methods ----

        //broadcasts the clientID associated with each GrabblablePlayerObject
        public static void BroadcastGrabbedPlayerObjectsList(ulong? singleReceiver = null) // todo: no broadcast if only one receiver
        {
            var networkClientMap = new List<(ulong networkId, ulong client)>();
            if (grabbablePlayerObjects!.Count > 0)
            {
                foreach (GameObject obj in grabbablePlayerObjects)
                {
                    ulong networkId = obj.GetComponent<NetworkObject>().NetworkObjectId;
                    ulong clientId = obj.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
                    networkClientMap.Add((networkId, clientId));
                }
                Network.Broadcast("GrabbablePlayerListSync", new GrabbablePlayerListSyncData() { grabbablePlayerIDS = networkClientMap, singleReceiver = singleReceiver });
            }
        }

        //Initializes uninitialized GrabbablePlayerObjects for clients
        public static void UpdateGrabbablePlayerObjectsClient(List<(ulong networkId, ulong clientId)> tuple)
        {
            var filteredObjects = GameObject.FindObjectsOfType<GrabbablePlayerObject>().Select(gpo => gpo.gameObject).ToList();
            if (filteredObjects.Count == 0)
                Plugin.log("ZERO?????", Plugin.LogType.Error);

            foreach (GameObject gpo in filteredObjects)
            {
                ulong gpoNetworkObjId = gpo.GetComponent<NetworkObject>().NetworkObjectId;
                foreach ((ulong networkId, ulong clientId) item in tuple)
                {
                    if (item.networkId == gpoNetworkObjId)
                    {
                        Plugin.log("\t" + item.networkId + ", " + item.clientId);
                        PlayerControllerB pcb = PlayerHelper.GetPlayerObject(item.clientId).GetComponent<PlayerControllerB>();
                        gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb);
                    }
                }
            }
        }

        public static void clearGrabbablePlayerObjects()
        {
            foreach (GameObject player in grabbablePlayerObjects)
                player.GetComponent<NetworkObject>().Despawn();

            grabbablePlayerObjects.Clear();
        }

        public static void SetPlayerGrabbable(PlayerControllerB pcb)
        {
            Plugin.log("Adding grabbable player: " + pcb.gameObject.ToString());
            var newObject = UnityEngine.Object.Instantiate(ShrinkRay.grabbablePlayerPrefab);
            newObject.GetComponent<NetworkObject>().Spawn();
            GrabbablePlayerObject gpo = newObject.GetComponent<GrabbablePlayerObject>();
            
            gpo.Initialize(pcb);
            grabbablePlayerObjects.Add(newObject);

            // Let everyone know
            BroadcastGrabbedPlayerObjectsList();
        }

        public static void RemoveAllPlayerGrabbables()
        {
            for (int i = grabbablePlayerObjects.Count - 1; i >= 0; i--)
                RemovePlayerGrabbable(grabbablePlayerObjects[i]);
        }

        public static void RemovePlayerGrabbableIfExists(PlayerControllerB pcb)
        {
            var index = grabbablePlayerObjects.FindIndex(0, bindingObject =>
            {
                var hasGPO = bindingObject.TryGetComponent(out GrabbablePlayerObject gpo);
                return (hasGPO && gpo != null && gpo.grabbedPlayer.playerClientId == pcb.playerClientId);
            });

            if (index != -1)
            {
                RemovePlayerGrabbable(grabbablePlayerObjects[index]);
                try
                {
                    grabbablePlayerObjects.RemoveAt(index);
                }
                catch(Exception e)
                {
                    Plugin.log("I knew this would happen... Anyways, here's the error: " + e.Message);
                }
            }
        }

        public static void RemovePlayerGrabbable(GameObject bindingObject)
        {
            if(bindingObject == null) return;

            var gpo = bindingObject.GetComponent<GrabbablePlayerObject>();
            if (gpo != null)
                UnityEngine.Object.Destroy(gpo);

            if (bindingObject != null)
                UnityEngine.Object.Destroy(bindingObject);

            // Let everyone know
            BroadcastGrabbedPlayerObjectsList();
        }
    }
}
