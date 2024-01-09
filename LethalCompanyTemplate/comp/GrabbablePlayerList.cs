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

            var playerObj = PlayerHelper.GetPlayerObject(ulong.Parse(playerID));
            if (playerObj == null)
            {
                Plugin.log("Unable to add grabbable player. Player with ID " + playerID + " not found!", Plugin.LogType.Error);
                return;
            }

            setPlayerGrabbable(playerObj);
        }

        [NetworkMessage("OnListTransmit")]
        public static void OnListTransmit(ulong sender, string data)
        {
            Plugin.log("Oh hey!! There's that list I needed!!!!", Plugin.LogType.Error);
            Plugin.log("\t" + data);
            List<(ulong, ulong)> tuple = StringToTupleList(data);
            GrabbablePlayerList.UpdateGrabbablePlayerObjectsClient(tuple);
        }

        // ---- Helper ----
        static string TupleListToString(List<(ulong, ulong)> tupleList)
        {
            return JsonConvert.SerializeObject(tupleList);
        }

        static List<(ulong, ulong)> StringToTupleList(string jsonString)
        {
            return JsonConvert.DeserializeObject<List<(ulong, ulong)>>(jsonString);
        }

        static GrabbablePlayerObject findGrabbableObjectForPlayer(ulong playerID) // untested!
        {
            return GameObject.FindObjectsOfType<GrabbablePlayerObject>().FirstOrDefault(gpo => gpo.grabbedPlayer.playerClientId == playerID);
        }

        // ---- Methods ----

        //broadcasts the clientID associated with each GrabblablePlayerObject
        public static void BroadcastGrabbedPlayerObjectsList()
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
                string strMap = TupleListToString(networkClientMap);
                Network.Broadcast("OnListTransmit", strMap);
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

        //Runs whenever player count changes,
        //if you're the host, then delete all grabbablePlayerObjects and make new ones(should maybe just delete the one that leaves or make the one that joins)
        //if you're a client, then delete all grabbablePlayerObjects and make new ones, then ask the host to help it initialize properly
        public static void UpdateGrabbablePlayerObjects()
        {
            clearGrabbablePlayerObjects();

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                //for each shrunken player, create and initialize a GrabbablePlayerObject, these will be networked, and should spawn for other players
                foreach (GameObject player in PlayerHelper.getAllPlayers())
                {
                    if (PlayerHelper.isShrunk(player))
                        setPlayerGrabbable(player);
                }
            }
        }

        public static void setPlayerGrabbable(GameObject playerObject)
        {
            Plugin.log("Adding grabbable player: " + playerObject.ToString());
            var newObject = UnityEngine.Object.Instantiate(ShrinkRay.grabbablePlayerPrefab);
            newObject.GetComponent<NetworkObject>().Spawn();
            GrabbablePlayerObject gpo = newObject.GetComponent<GrabbablePlayerObject>();
            var pcb = playerObject.GetComponent<PlayerControllerB>();
            gpo.Initialize(pcb);
            grabbablePlayerObjects.Add(newObject);

            // Let everyone know
            BroadcastGrabbedPlayerObjectsList();
        }
    }
}
