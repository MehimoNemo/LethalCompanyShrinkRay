using GameNetcodeStuff;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LethalLib.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerList : NetworkBehaviour
    {
        public List<GameObject> grabbablePlayerObjects { get; private set; }

        public static GrabbablePlayerList Instance = null;
        public static GameObject networkPrefab { get; set; }

        public static void loadAsset(AssetBundle assetBundle)
        {
            var networkPrefab = assetBundle.LoadAsset<GameObject>("GrabbablePlayerList.prefab");
            if (networkPrefab == null)
            {
                Plugin.log("GrabbablePlayerList.asset not found!", Plugin.LogType.Error);
                return;
            }

            Instance = networkPrefab.AddComponent<GrabbablePlayerList>();
            Instance.grabbablePlayerObjects = new List<GameObject>();

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);
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

        public static GrabbablePlayerObject findGrabbableObjectForPlayer(ulong playerID)
        {
            foreach (var gpo in GameObject.FindObjectsOfType<GrabbablePlayerObject>())
            {
                if (gpo == null || gpo.grabbedPlayer == null)
                    continue;

                if (gpo.grabbedPlayer.playerClientId == playerID)
                    return gpo;
            }
            return null;
        }

        private static GrabbablePlayerObject findGrabbableObjectWithNetworkID(ulong networkID)
        {
            Plugin.log("we're looking for network id: " + networkID);
            foreach (var gpo in GameObject.FindObjectsOfType<GrabbablePlayerObject>())
            {
                Plugin.log("found one grabbable object..");
                if (gpo == null)
                    continue;

                ulong id = gpo.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                Plugin.log("has network id: " + id);

                if (id == networkID)
                    return gpo;
            }
            return null;
        }

        // Networking
        [ServerRpc(RequireOwnership = false)]
        public void SendGrabbablePlayerListServerRpc(ulong receiver)
        {
            Plugin.log("SendGrabbablePlayerListServerRpc");
            var networkClientMap = new List<(ulong networkId, ulong client)>();
            if (grabbablePlayerObjects!.Count > 0)
            {
                foreach (GameObject obj in grabbablePlayerObjects)
                {
                    ulong networkId = obj.GetComponent<NetworkObject>().NetworkObjectId;
                    ulong clientId = obj.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
                    networkClientMap.Add((networkId, clientId));
                }
                SendGrabbablePlayerListClientRpc(TupleListToString(networkClientMap), receiver);
            }
        }

        [ClientRpc]
        public void SendGrabbablePlayerListClientRpc(string grabbablePlayerListString, ulong receiver)
        {
            Plugin.log("SendGrabbablePlayerListClientRpc");
            var grabbablePlayerList = StringToTupleList(grabbablePlayerListString);

            if(PlayerHelper.currentPlayer().playerClientId != receiver) return; // Not meant for us

            Plugin.log("Oh hey!! There's that list I needed!!!!", Plugin.LogType.Error);
            Plugin.log("\t" + grabbablePlayerListString);

            var filteredObjects = GameObject.FindObjectsOfType<GrabbablePlayerObject>().Select(gpo => gpo.gameObject).ToList();
            if (filteredObjects.Count == 0)
                Plugin.log("ZERO?????", Plugin.LogType.Error);

            foreach (GameObject gpo in filteredObjects)
            {
                ulong gpoNetworkObjId = gpo.GetComponent<NetworkObject>().NetworkObjectId;
                foreach ((ulong networkId, ulong clientId) item in grabbablePlayerList)
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

        // Methods to add/remove/change grabbable players
        public void clearGrabbablePlayerObjects()
        {
            foreach (GameObject player in grabbablePlayerObjects)
                player.GetComponent<NetworkObject>().Despawn();

            grabbablePlayerObjects.Clear();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerGrabbableServerRpc(ulong playerID, bool onlyLocal = false)
        {
            Plugin.log("SetPlayerGrabbableServerRpc");
            var pcb = PlayerHelper.GetPlayerController(playerID);
            if (pcb == null) return;

            Plugin.log("Adding grabbable player: " + pcb.gameObject.ToString());
            var newObject = Instantiate(GrabbablePlayerObject.networkPrefab);
            var networkObj = newObject.GetComponent<NetworkObject>();
            networkObj.Spawn();
            GrabbablePlayerObject gpo = newObject.GetComponent<GrabbablePlayerObject>();

            gpo.Initialize(pcb);
            grabbablePlayerObjects.Add(newObject);

            if (!onlyLocal) // Let everyone know
                SetPlayerGrabbableClientRpc(playerID, networkObj.NetworkObjectId);
        }

        [ClientRpc]
        public void SetPlayerGrabbableClientRpc(ulong playerID, ulong networkObjectID)
        {
            Plugin.log("SetPlayerGrabbableClientRpc");
            var pcb = PlayerHelper.GetPlayerController(playerID);
            if(pcb == null)
            {
                Plugin.log("Unable to find Player (" + playerID + ")");
                return;
            }

            var gpo = findGrabbableObjectWithNetworkID(networkObjectID);
            if (gpo == null)
            {
                Plugin.log("Unable to find grabbablePlayerObject for Player (" + pcb.playerClientId + ")");
                return;
            }

            gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb);
        }

        [ServerRpc]
        public void RemoveAllPlayerGrabbablesServerRpc()
        {
            Plugin.log("RemoveAllPlayerGrabbablesServerRpc");
            for (int i = grabbablePlayerObjects.Count - 1; i >= 0; i--)
                Destroy(grabbablePlayerObjects[i]);
        }

        [ClientRpc]
        public void RemoveAllPlayerGrabbablesClientRpc()
        {
            Plugin.log("RemoveAllPlayerGrabbablesClientRpc");
            grabbablePlayerObjects.Clear();
        }

        private int getBindingObjectIDFromPlayerID(ulong playerID)
        {
            var index = grabbablePlayerObjects.FindIndex(0, bindingObject =>
            {
                if (bindingObject == null)
                    return false;

                var hasGPO = bindingObject.TryGetComponent(out GrabbablePlayerObject gpo);
                var hasNO = bindingObject.TryGetComponent(out NetworkObject networkObject);
                return (hasGPO && hasNO && gpo != null && gpo.grabbedPlayer != null && gpo.grabbedPlayer.playerClientId == playerID);
            });

            return index;
        }

        [ServerRpc]
        public void RemovePlayerGrabbableServerRpc(ulong playerID)
        {
            Plugin.log("RemovePlayerGrabbableServerRpc");
            var bindingObjectID = getBindingObjectIDFromPlayerID(playerID);
            if (bindingObjectID == -1)
                return;

            var bindingObject = grabbablePlayerObjects[bindingObjectID];
            if (!bindingObject.TryGetComponent( out GrabbablePlayerObject gpo))
                return;

            RemovePlayerGrabbableClientRpc(playerID); // Let everyone know

            Destroy(gpo);
            if (bindingObject != null)
                Destroy(bindingObject);
        }

        [ClientRpc]
        public void RemovePlayerGrabbableClientRpc(ulong playerID)
        {
            Plugin.log("RemovePlayerGrabbableClientRpc");
            RemovePlayerGrabbable(playerID);
        }

        public void RemovePlayerGrabbable(ulong playerID)
        {
            Plugin.log("RemovePlayerGrabbable");
            var bindingObjectID = getBindingObjectIDFromPlayerID(playerID);
            if (bindingObjectID == -1)
                return;

            grabbablePlayerObjects.RemoveAt(bindingObjectID);
        }
    }
}
