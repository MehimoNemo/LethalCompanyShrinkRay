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
            if (!PlayerHelper.isHost())
                return;

            var pcb = PlayerHelper.GetPlayerController(sender);
            if (pcb == null)
            {
                Plugin.log("Unable to add grabbable player. Player with ID " + playerID + " not found!", Plugin.LogType.Error);
                return;
            }

            SetPlayerGrabbable(pcb);
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
            foreach(var gpo in GameObject.FindObjectsOfType<GrabbablePlayerObject>())
            {
                if (gpo == null || gpo.grabbedPlayer == null)
                    continue;

                if (gpo.grabbedPlayer.playerClientId == playerID)
                    return gpo;
            }
            return null;
        }

		// Networking

        internal class GrabbablePlayerListSyncData
        {
            public List<(ulong, ulong)> grabbablePlayerIDS { get; set; }
            public ulong? singleReceiver { get; set; } = null;
        }

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

        [NetworkMessage("GrabbablePlayerListSync")]
        public static void GrabbablePlayerListSync(ulong sender, GrabbablePlayerListSyncData data)
        {
            if (data.singleReceiver != null && PlayerHelper.currentPlayer().playerClientId != data.singleReceiver)
                return; // not meant for us

            Plugin.log("Oh hey!! There's that list I needed!!!!", Plugin.LogType.Error);
            Plugin.log("\t" + TupleListToString(data.grabbablePlayerIDS));

            var filteredObjects = GameObject.FindObjectsOfType<GrabbablePlayerObject>().Select(gpo => gpo.gameObject).ToList();
            if (filteredObjects.Count == 0)
                Plugin.log("ZERO?????", Plugin.LogType.Error);

            foreach (GameObject gpo in filteredObjects)
            {
                ulong gpoNetworkObjId = gpo.GetComponent<NetworkObject>().NetworkObjectId;
                foreach ((ulong networkId, ulong clientId) item in data.grabbablePlayerIDS)
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

        // Added grabbable player
        public static void BroadcastGrabbedPlayerObjectAdded(GameObject bindingObject, ulong? singleReceiver = null) // todo: no broadcast if only one receiver
        {
            Plugin.log("BroadcastGrabbedPlayerObjectAdded");
            ulong clientId = bindingObject.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
            ulong networkId = bindingObject.GetComponent<NetworkObject>().NetworkObjectId;

            Network.Broadcast("AddedGrabbablePlayerSync", clientId.ToString());
        }

        [NetworkMessage("AddedGrabbablePlayerSync")]
        public static void AddedGrabbablePlayerSync(ulong sender, string playerID)
        {
            Plugin.log("AddedGrabbablePlayerSync");
            SetPlayerGrabbable(PlayerHelper.GetPlayerController(ulong.Parse(playerID)));
        }

        // Removed grabbable player
        public static void BroadcastGrabbedPlayerObjectRemoved(GameObject bindingObject, ulong? singleReceiver = null) // todo: no broadcast if only one receiver
        {
            Plugin.log("BroadcastGrabbedPlayerObjectRemoved");
            ulong clientId = bindingObject.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
            ulong networkId = bindingObject.GetComponent<NetworkObject>().NetworkObjectId;

            Network.Broadcast("RemovedGrabbablePlayerSync", clientId.ToString());
        }

        [NetworkMessage("RemovedGrabbablePlayerSync")]
        public static void RemovedGrabbablePlayerSync(ulong sender, string playerID)
        {
            Plugin.log("RemovedGrabbablePlayerSync");
            RemovePlayerGrabbableIfExists(PlayerHelper.GetPlayerController(ulong.Parse(playerID)));
        }

        // Methods to add/remove/change grabbable players
        public static void clearGrabbablePlayerObjects()
        {
            foreach (GameObject player in grabbablePlayerObjects)
                player.GetComponent<NetworkObject>().Despawn();

            grabbablePlayerObjects.Clear();
        }

        public static void SetPlayerGrabbable(PlayerControllerB pcb, bool onlyLocal = false)
        {
            if (!PlayerHelper.isHost())
                return;

            Plugin.log("Adding grabbable player: " + pcb.gameObject.ToString());
            var newObject = UnityEngine.Object.Instantiate(ShrinkRay.grabbablePlayerPrefab);
            newObject.GetComponent<NetworkObject>().Spawn();
            GrabbablePlayerObject gpo = newObject.GetComponent<GrabbablePlayerObject>();
            
            gpo.Initialize(pcb);
            grabbablePlayerObjects.Add(newObject);

            if (!onlyLocal) // Let everyone know
                BroadcastGrabbedPlayerObjectAdded(newObject);
        }

        public static void RemoveAllPlayerGrabbables(bool onlyLocal = false)
        {
            for (int i = grabbablePlayerObjects.Count - 1; i >= 0; i--)
                RemovePlayerGrabbable(grabbablePlayerObjects[i]);
        }

        public static void RemovePlayerGrabbableIfExists(PlayerControllerB pcb, bool onlyLocal = false)
        {
            if(pcb == null) return;

            Plugin.log("RemovePlayerGrabbableIfExists");
            var index = grabbablePlayerObjects.FindIndex(0, bindingObject =>
            {
                if (bindingObject == null)
                    return false;

                var hasGPO = bindingObject.TryGetComponent(out GrabbablePlayerObject gpo);
                var hasNO = bindingObject.TryGetComponent(out NetworkObject networkObject);
                return (hasGPO && hasNO && gpo != null && gpo.grabbedPlayer.playerClientId == pcb.playerClientId);
            });

            if (index != -1)
            {
                if(PlayerHelper.isHost())
                    RemovePlayerGrabbable(grabbablePlayerObjects[index], onlyLocal);

                grabbablePlayerObjects.RemoveAt(index);
            }
        }

        public static void RemovePlayerGrabbable(GameObject bindingObject, bool onlyLocal = false)
        {
            if(bindingObject == null) return;

            Plugin.log("RemovePlayerGrabbable");
            if (!onlyLocal) // Let everyone know
                BroadcastGrabbedPlayerObjectRemoved(bindingObject);

            var gpo = bindingObject.GetComponent<GrabbablePlayerObject>();
            if (gpo != null)
                UnityEngine.Object.Destroy(gpo);

            if (bindingObject != null)
                UnityEngine.Object.Destroy(bindingObject);
        }
    }
}
