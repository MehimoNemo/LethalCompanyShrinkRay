using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LCShrinkRay.comp.GrabbablePlayerObject;

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
        public static void KillPlayerServerRpc(int playerId,/* bool spawnBody,*/ Vector3 bodyVelocity, int causeOfDeath/*, int deathAnimation*/)
        {
            Plugin.Log("KillPlayerServerRpc. Cause: " + (CauseOfDeath)causeOfDeath + " / Velocity: " + bodyVelocity);
            RemovePlayerGrabbable((ulong)playerId);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
        [HarmonyPrefix]
        public static void KillPlayerClientRpcPrefix(int playerId/*, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation*/)
        {
            Plugin.Log("KillPlayerClientRpcPrefix");

            var targetPlayer = PlayerInfo.ControllerFromID((ulong)playerId);
            if (targetPlayer == null) return;

            ResetAnyPlayerModificationsFor(targetPlayer);

            if ((ulong)playerId != PlayerInfo.CurrentPlayerID) return;

            // We died while holding someone
            if ((ulong)playerId == PlayerInfo.CurrentPlayerID && TryFindGrabbableObjectByHolder((ulong)playerId, out _))
                PlayerInfo.CurrentPlayer.DiscardHeldObject();

            // The person we held died
            else if (TryFindGrabbableObjectForPlayer((ulong)playerId, out GrabbablePlayerObject gpo) && gpo.playerHeldBy != null && gpo.playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                PlayerInfo.CurrentPlayer.DiscardHeldObject();

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

        [HarmonyPatch(typeof(PlayerControllerB), "TeleportPlayer")]
        [HarmonyPostfix]
        public static void TeleportPlayer(PlayerControllerB __instance)
        {
            if (StartOfRound.Instance == null || StartOfRound.Instance.inShipPhase) return;

            // Handles the adjustment of our own GrabbablePlayerObject, aswell as follow someone who teleported (to adjust lighting, weather, items, etc)
            if (__instance.playerClientId == PlayerInfo.CurrentPlayerID)
            {
                if (TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
                {
                    if (gpo.playerHeldBy != null) // We're grabbable and someone holds us
                        gpo.StartCoroutine(gpo.UpdateAfterTeleportEnsured(TargetPlayer.GrabbedPlayer));
                }

                if (TryFindGrabbableObjectByHolder(PlayerInfo.CurrentPlayerID, out gpo))
                {
                    // We're holding someone
                    gpo.StartCoroutine(gpo.UpdateAfterTeleportEnsured(TargetPlayer.Holder));
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
        [HarmonyPostfix]
        public static void SetPlayerVoiceFilters()
        {
            if (SoundManager.Instance == null) return;

            foreach (var pcb in StartOfRound.Instance.allPlayerScripts)
            {
                if(pcb != null && pcb.isPlayerControlled && !pcb.isPlayerDead)
                {
                    float playerScale = PlayerInfo.SizeOf(pcb);
                    float intensity = (float)ModConfig.Instance.values.pitchDistortionIntensity;

                    float modifiedPitch = (float)(-1f * intensity * (playerScale - PlayerInfo.CurrentPlayerScale) + 1f);

                    SoundManager.Instance.playerVoicePitchTargets[pcb.playerClientId] = modifiedPitch;
                    SoundManager.Instance.SetPlayerPitch(modifiedPitch, (int)pcb.playerClientId);
                }
            }
        }
        #endregion

        #region Helper
#if DEBUG
        public static string Log
        {
            get
            {
                string output = "GrabbablePlayerList:\n";
                output += "------------------------------\n";
                foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
                    output += ("[" + gpo.name + "] with player " + gpo.grabbedPlayerID.Value + ".\n");

                if (PlayerInfo.IsHost)
                {
                    output += "------------------------------\n";
                    output += "Network List:\n";
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
                }

                return output;
            }
        }
#endif

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

        public static bool TryFindGrabbableObjectByHolder(ulong playerHeldByID, out GrabbablePlayerObject result)
        {
            foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
            {
                if (gpo != null && gpo.playerHeldBy != null && gpo.playerHeldBy.playerClientId == playerHeldByID)
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
            if(!PlayerInfo.IsHost)
            {
                Plugin.Log("SetPlayerGrabbable called from client. This shouldn't happen!", Plugin.LogType.Warning);
                return;
            }

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

            if (!TryFindGrabbableObjectForPlayer(playerID, out GrabbablePlayerObject gpo)) // todo: make it so the tryFind is not needed
            {
                Plugin.Log("Player " + playerID + " didn't had a grabbableObject!");
                return;
            }

            try
            {
                gpo.StartCoroutine(gpo.CleanUp(() =>
                {
                    DespawnGrabbablePlayer(playerID);
                }));
            }
            catch(Exception ex)
            {
                Plugin.Log("Error cleaning up grabbablePlayer: " + ex.Message, Plugin.LogType.Error);
                DespawnGrabbablePlayer(playerID);
            }

        }

        public static void DespawnGrabbablePlayer(ulong playerID)
        {
            if (PlayerInfo.IsHost && networkObjects[playerID].IsSpawned)
                networkObjects[playerID].Despawn();

            networkObjects.Remove(playerID);
        }

        public static void ResetAnyPlayerModificationsFor(PlayerControllerB targetPlayer)
        {
            Plugin.Log("ResetAnyPlayerModificationsFor");
            if (targetPlayer == null) return;

            if (targetPlayer.gameObject != null)
            {
                targetPlayer.gameObject.transform.localScale = Vector3.one;

                PlayerInfo.AdjustArmScale(targetPlayer);

                if(targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
                {
                    PlayerInfo.AdjustMaskPos();
                    PlayerInfo.AdjustMaskScale();
                }
            }

            SoundManager.Instance.SetPlayerPitch(1f, (int)targetPlayer.playerClientId);
        }

        public static void UpdateWhoIsGrabbableFromPerspectiveOf(PlayerControllerB targetPlayer)
        {
            /*Plugin.Log("UpdateWhoIsGrabbableFromPerspectiveOf");
            if(targetPlayer == null) return;

            if(targetPlayer.playerClientId != PlayerInfo.CurrentPlayerID)
            {
                // Someone else's size changed
                if(TryFindGrabbableObjectForPlayer(targetPlayer.playerClientId, out GrabbablePlayerObject gpo))
                    gpo.EnableInteractTrigger(PlayerInfo.SizeOf(targetPlayer) < PlayerInfo.SizeOf(PlayerInfo.CurrentPlayer));
            }
            else
            {
                // We changed size
                var currentSize = PlayerInfo.SizeOf(PlayerInfo.CurrentPlayer);

                foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
                {
                    if (gpo == null || gpo.grabbedPlayerID.Value == ulong.MaxValue) continue;
                    gpo.EnableInteractTrigger(PlayerInfo.SizeOf(gpo.grabbedPlayer) < currentSize);
                }
            }*/
        }
        #endregion
    }
}
