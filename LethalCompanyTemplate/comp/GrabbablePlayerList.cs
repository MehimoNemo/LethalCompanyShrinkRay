using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.helper;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    [HarmonyPatch]
    internal class GrabbablePlayerList
    {
        #region Properties
        public static Dictionary<ulong, NetworkObject> networkObjects = new Dictionary<ulong, NetworkObject>();
        #endregion

        #region Patches

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerServerRpc")]
        [HarmonyPostfix]
        public static void KillPlayerServerRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation)
        {
            Plugin.Log("KillPlayerServerRpc");
            RemovePlayerGrabbable((ulong)playerId);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
        [HarmonyPostfix]
        public static void KillPlayerClientRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation)
        {
            Plugin.Log("KillPlayerClientRpc");
            ResetAnyPlayerModificationsFor(PlayerInfo.ControllerFromID((ulong)playerId));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        public static void DespawnPropsAtEndOfRoundPre()
        {
            var gpoList = Resources.FindObjectsOfTypeAll(typeof(GrabbablePlayerObject)); // Don't destroy these. Workaround
            foreach (var gpo in gpoList)
                gpo.hideFlags = HideFlags.HideAndDontSave;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        public static void DespawnPropsAtEndOfRoundPost()
        {
            var gpoList = Resources.FindObjectsOfTypeAll(typeof(GrabbablePlayerObject)); // reset to default
            foreach (var gpo in gpoList)
                gpo.hideFlags = HideFlags.None;
        }
        #endregion

        #region Helper
        public static string Log
        {
            get
            {
                if (!PlayerInfo.IsHost)
                    return "GrabbablePlayerList is saved on host only";

                string output = "GrabbablePlayerList:\n";
                output += "------------------------------\n";
                foreach (var networkObjectPair in networkObjects)
                {
                    output += ("PlayerID: " + networkObjectPair.Key + ".\n");
                    if (networkObjectPair.Value != null)
                    {
                        if (networkObjectPair.Value.TryGetComponent(out NetworkObject networkObject))
                            output += ("NetworkID: " + networkObject.NetworkObjectId + ".\n");
                        if (TryFindGrabbableObjectForPlayer(networkObjectPair.Key, out GrabbablePlayerObject gpo))
                            output += ("GPO: " + (gpo.grabbedPlayer != null ? gpo.grabbedPlayer.name : "No player") + ".\n");
                    }
                    output += ("------------------------------\n");
                }
                return output;
            }
        }
        public static bool TryFindGrabbableObjectForPlayer(ulong playerID, out GrabbablePlayerObject result)
        {
            foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
            {
                if (gpo != null && gpo.grabbedPlayerID.Value == playerID)
                {
                    result = gpo;
                    return true;
                }
            }

            result = null;
            return false;
        }
        #endregion

        #region Methods


        public static void ClearGrabbablePlayerObjects()
        {
            Plugin.Log("ClearGrabbablePlayerObjects");

            List<ulong> playerIDs = new List<ulong>(networkObjects.Keys);
            foreach (var playerID in playerIDs)
                RemovePlayerGrabbable(playerID);
        }

        public static void SetPlayerGrabbable(ulong playerID)
        {
            if (networkObjects.ContainsKey(playerID))
            {
                Plugin.Log("Player " + playerID + " already grabbable!");
                return;
            }

            networkObjects[playerID] = GrabbablePlayerObject.Instantiate(playerID);
            Plugin.Log("NEW GRABBALEPLAYER COUNT: " + networkObjects.Count);
        }

        public static void RemovePlayerGrabbable(ulong playerID)
        {
            Plugin.Log("RemovePlayerGrabbable");
            if (!networkObjects.TryGetValue(playerID, out NetworkObject networkObject))
            {
                Plugin.Log("Player " + playerID + " wasn't grabbable!");
                return;
            }

            if (PlayerInfo.IsHost && networkObject.IsSpawned)
                networkObject.Despawn();

            networkObjects.Remove(playerID);
        }

        public static void ResetAnyPlayerModificationsFor(PlayerControllerB targetPlayer)
        {
            Plugin.Log("ResetAnyPlayerModificationsFor");
            if (targetPlayer == null) return;

            if (targetPlayer.gameObject != null)
            {
                targetPlayer.gameObject.transform.localScale = Vector3.one;

                var armTransform = PlayerInfo.GetArmTransform(targetPlayer);
                if (armTransform != null)
                    armTransform.localScale = PlayerInfo.CalcArmScale(1f);

                if(targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
                {
                    var maskTransform = PlayerInfo.GetGlobalMaskTransform(targetPlayer);
                    if (maskTransform != null)
                    {
                        maskTransform.localScale = PlayerInfo.CalcMaskScaleVec(1f);
                        maskTransform.localPosition = PlayerInfo.CalcMaskPosVec(1f);
                    }
                }
            }

            SoundManager.Instance.SetPlayerPitch(1f, (int)targetPlayer.playerClientId);
        }
        #endregion
    }
}
