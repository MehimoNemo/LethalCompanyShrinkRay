﻿using GameNetcodeStuff;
using LCShrinkRay.helper;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class GrabbablePlayerList : NetworkBehaviour
    {
        #region Properties
        public List<GameObject> grabbablePlayerObjects = new List<GameObject>(); // todo: auf Dictionary<ulong, GameObject> ändern

        private static GrabbablePlayerList instance = null;

        public static bool HasInstance
        {
            get
            {
                return instance != null;
            }
        }

        public static GrabbablePlayerList Instance
        {
            get
            {
                if (!HasInstance)
                {
                    /*Plugin.log("GrabbablePlayerList.Instance called earlier than expected. Trying to load assets earlier.", Plugin.LogType.Warning);

                    if (networkPrefab == null)
                        GameNetworkManagerPatch.LoadAllAssets();*/
                    CreateInstance();
                }

                return instance;
            }
        }

        private static GameObject networkPrefab { get; set; }
        private static GameObject instanciatedPrefab { get; set; }
        #endregion

        #region Networking
        public static void CreateNetworkPrefab()
        {
            if (networkPrefab != null) return; // Already loaded

            networkPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("GrabbablePlayerList");
            networkPrefab.AddComponent<GrabbablePlayerList>();
        }

        public static void CreateInstance()
        {
            if (instance != null) return; // Already initialized

            if (PlayerHelper.isHost())
            {
                instanciatedPrefab = Instantiate(networkPrefab);
                var networkObj = instanciatedPrefab.GetComponent<NetworkObject>();
                networkObj.Spawn();
                instance = instanciatedPrefab.GetComponent<GrabbablePlayerList>();
            }
            else
            {
                instance = networkPrefab.GetComponent<GrabbablePlayerList>();
            }
        }

        public static void RemoveInstance()
        {
            if (instance == null) return; // Not initialized

            ClearGrabbablePlayerObjects();

            if (PlayerHelper.isHost())
                Destroy(instanciatedPrefab);

            instance = null;
        }
        #endregion

        #region Helper
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
        #endregion

        #region Methods
        [ServerRpc(RequireOwnership = false)]
        public void SendGrabbablePlayerListServerRpc(ulong receiver)
        {
            Plugin.log("SendGrabbablePlayerListServerRpc");
            var networkClientMap = new List<(ulong networkId, ulong client)>();
            if (grabbablePlayerObjects!.Count > 0)
            {
                foreach (GameObject obj in grabbablePlayerObjects)
                {
                    if(obj == null) continue;

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

        public static void ClearGrabbablePlayerObjects()
        {
            if(!HasInstance) return;

            if (PlayerHelper.isHost())
                Instance.ClearGrabbablePlayerObjectsServerRpc();
            else
                Instance.ClearGrabbablePlayerObjectsClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearGrabbablePlayerObjectsServerRpc()
        {
            foreach(var obj in grabbablePlayerObjects)
            {
                if (obj == null) continue;
                obj.GetComponent<NetworkObject>().Despawn();
            }

            ClearGrabbablePlayerObjectsClientRpc();
        }

        [ClientRpc]
        public void ClearGrabbablePlayerObjectsClientRpc()
        {
            for (int i = grabbablePlayerObjects.Count - 1; i >= 0; i--)
            {
                if (grabbablePlayerObjects[i] == null) continue;

                if (grabbablePlayerObjects[i].TryGetComponent(out GrabbablePlayerObject gpo))
                    Destroy(gpo);

                Destroy(grabbablePlayerObjects[i]);
            }

            grabbablePlayerObjects.Clear();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerGrabbableServerRpc(ulong playerID, bool onlyLocal = false)
        {
            Plugin.log("SetPlayerGrabbableServerRpc");

            foreach(var obj in grabbablePlayerObjects)
            {
                if (obj.TryGetComponent(out GrabbablePlayerObject gpoObj))
                {
                    if(gpoObj.grabbedPlayer.playerClientId == playerID)
                        return; // Already existing
                }
            }

            var pcb = PlayerHelper.GetPlayerController(playerID);
            if (pcb == null) return;

            Plugin.log("Adding grabbable player object for player: " + playerID);
            var networkObj = GrabbablePlayerObject.Instantiate();
            
            if (!onlyLocal) // Let everyone know
                SetPlayerGrabbableClientRpc(playerID, networkObj.NetworkObjectId);
        }

        [ClientRpc]
        public void SetPlayerGrabbableClientRpc(ulong playerID, ulong networkObjectID)
        {
            Plugin.log("SetPlayerGrabbableClientRpc. Grabbable players: " + grabbablePlayerObjects.Count);
            
            foreach (var obj in grabbablePlayerObjects)
            {
                if (obj.TryGetComponent(out GrabbablePlayerObject gpoObj) && gpoObj.grabbedPlayer.playerClientId == playerID)
                    return; // Already existing
            }

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

            Plugin.log("Init new grabbablePlayer.");
            gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb);
            Plugin.log("Add new grabbablePlayer to list.");
            grabbablePlayerObjects.Add(gpo.gameObject);
            Plugin.log("NEW GRABBALEPLAYER COUNT: " + grabbablePlayerObjects.Count);
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
            Plugin.log("RemovePlayerGrabbable");
            var bindingObjectID = getBindingObjectIDFromPlayerID(playerID);
            if (bindingObjectID == -1)
                return;

            grabbablePlayerObjects.RemoveAt(bindingObjectID);
        }
        #endregion
    }
}
