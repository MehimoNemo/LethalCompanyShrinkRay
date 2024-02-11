using GameNetcodeStuff;
using LCShrinkRay.helper;
using Newtonsoft.Json;
using System.Collections;
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
                    CreateInstance();

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
            if (HasInstance) return; // Already initialized

            if (!PlayerInfo.IsHost) return;

            instanciatedPrefab = Instantiate(networkPrefab);
            var networkObj = instanciatedPrefab.GetComponent<NetworkObject>();
            networkObj.Spawn();
            instance = instanciatedPrefab.GetComponent<GrabbablePlayerList>();
        }

        public static void RemoveInstance()
        {
            if (instance == null) return; // Not initialized

            if (PlayerInfo.IsHost)
            {
                instance.ClearGrabbablePlayerObjectsServerRpc();
                Destroy(instanciatedPrefab);
            }

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

        public static GrabbablePlayerObject FindGrabbableObjectForPlayer(ulong playerID)
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

        private static GrabbablePlayerObject FindGrabbableObjectWithNetworkID(ulong networkID)
        {
            Plugin.Log("we're looking for network id: " + networkID);
            foreach (var gpo in GameObject.FindObjectsOfType<GrabbablePlayerObject>())
            {
                Plugin.Log("found one grabbable object..");
                if (gpo == null)
                    continue;

                ulong id = gpo.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                Plugin.Log("has network id: " + id);

                if (id == networkID)
                    return gpo;
            }
            return null;
        }

        private int GetBindingObjectIDFromPlayerID(ulong playerID)
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
        [ServerRpc]
        public void SyncInstanceServerRpc()
        {
            SyncInstanceClientRpc();
        }

        [ClientRpc]
        public void SyncInstanceClientRpc()
        {
            instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializeGrabbablePlayerObjectsServerRpc(ulong receiver)
        {
            var networkClientMap = new List<(ulong networkId, ulong client)>();

            foreach (GameObject obj in grabbablePlayerObjects)
            {
                if(obj == null) continue;

                ulong networkId = obj.GetComponent<NetworkObject>().NetworkObjectId;
                ulong clientId = obj.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
                networkClientMap.Add((networkId, clientId));
            }
            InitializeGrabbablePlayerObjectsClientRpc(TupleListToString(networkClientMap), receiver);
        }

        [ClientRpc]
        public void InitializeGrabbablePlayerObjectsClientRpc(string grabbablePlayerListString, ulong receiver)
        {
            if (receiver != PlayerInfo.CurrentPlayerID) return; // Not meant for us

            if (grabbablePlayerListString == null || grabbablePlayerListString.Length == 0) return;

            var grabbablePlayerList = StringToTupleList(grabbablePlayerListString);

            Plugin.Log("Oh hey!! There's that list I needed!!!! (the list: " + grabbablePlayerListString + ")");

            var filteredObjects = GameObject.FindObjectsOfType<GrabbablePlayerObject>().Select(gpo => gpo.gameObject).ToList();
            if (filteredObjects.Count == 0)
                Plugin.Log("ZERO????? Nobody got shrinked so far?!");

            foreach (GameObject gpo in filteredObjects)
            {
                ulong gpoNetworkObjId = gpo.GetComponent<NetworkObject>().NetworkObjectId;
                foreach ((ulong networkId, ulong clientId) item in grabbablePlayerList)
                {
                    if (item.networkId == gpoNetworkObjId)
                    {
                        Plugin.Log("\t" + item.networkId + ", " + item.clientId);
                        PlayerControllerB pcb = PlayerInfo.ControllerFromID(item.clientId).gameObject.GetComponent<PlayerControllerB>();
                        gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb, pcb.playerClientId == receiver);
                    }
                }
            }
        }

        public static void ClearGrabbablePlayerObjects()
        {
            if(!HasInstance) return;

            if (PlayerInfo.IsHost)
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
            Plugin.Log("SetPlayerGrabbableServerRpc");

            foreach(var obj in grabbablePlayerObjects)
            {
                if (obj.TryGetComponent(out GrabbablePlayerObject gpoObj))
                {
                    if(gpoObj.grabbedPlayer.playerClientId == playerID)
                        return; // Already existing
                }
            }

            var pcb = PlayerInfo.ControllerFromID(playerID);
            if (pcb == null) return;

            Plugin.Log("Adding grabbable player object for player: " + playerID);
            var networkObj = GrabbablePlayerObject.Instantiate();
            
            if (!onlyLocal) // Let everyone know
                SetPlayerGrabbableClientRpc(playerID, networkObj.NetworkObjectId);
        }

        [ClientRpc]
        public void SetPlayerGrabbableClientRpc(ulong playerID, ulong networkObjectID)
        {
            Plugin.Log("SetPlayerGrabbableClientRpc. Grabbable players: " + grabbablePlayerObjects.Count);
            
            foreach (var obj in grabbablePlayerObjects)
            {
                if (obj.TryGetComponent(out GrabbablePlayerObject gpoObj) && gpoObj.grabbedPlayer.playerClientId == playerID)
                    return; // Already existing
            }

            var pcb = PlayerInfo.ControllerFromID(playerID);
            if(pcb == null)
            {
                Plugin.Log("Unable to find Player (" + playerID + ")");
                return;
            }

            var gpo = FindGrabbableObjectWithNetworkID(networkObjectID);
            if (gpo == null)
            {
                Plugin.Log("Unable to find grabbablePlayerObject for Player (" + pcb.playerClientId + ")");
                return;
            }

            Plugin.Log("Init new grabbablePlayer.");
            gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb, PlayerInfo.CurrentPlayer != null && pcb.playerClientId == PlayerInfo.CurrentPlayerID);
            Plugin.Log("Add new grabbablePlayer to list.");
            grabbablePlayerObjects.Add(gpo.gameObject);
            Plugin.Log("NEW GRABBALEPLAYER COUNT: " + grabbablePlayerObjects.Count);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemovePlayerGrabbableServerRpc(ulong playerID)
        {
            Plugin.Log("RemovePlayerGrabbableServerRpc");
            var bindingObjectID = GetBindingObjectIDFromPlayerID(playerID);
            if (bindingObjectID == -1)
            {
                Plugin.Log("Player wasn't grabbable.");
                return;
            }

            var bindingObject = grabbablePlayerObjects[bindingObjectID];
            if (!bindingObject.TryGetComponent( out GrabbablePlayerObject gpo))
            {

                Plugin.Log("Player had no GrabbablePlayerObject somehow.");
                return;
            }

            RemovePlayerGrabbableClientRpc(playerID); // Let everyone know

            Destroy(gpo);
            if (bindingObject != null)
                Destroy(bindingObject);
        }

        [ClientRpc]
        public void RemovePlayerGrabbableClientRpc(ulong playerID)
        {
            Plugin.Log("RemovePlayerGrabbable");
            var bindingObjectID = GetBindingObjectIDFromPlayerID(playerID);
            if (bindingObjectID == -1)
            {
                Plugin.Log("Player wasn't grabbable.");
                return;
            }

            var bindingObject = grabbablePlayerObjects[bindingObjectID];
            if (bindingObject.TryGetComponent(out GrabbablePlayerObject gpo) && gpo.grabbedPlayer != null && !PlayerInfo.IsNormalSize(gpo.grabbedPlayer))
                gpo.grabbedPlayer.transform.localScale = Vector3.one; // Reset size

            grabbablePlayerObjects.RemoveAt(bindingObjectID);
        }
        #endregion
    }
}
