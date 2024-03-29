﻿using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

namespace LittleCompany.components
{
    [HarmonyPatch]
    internal class GrabbablePlayerList
    {
        #region Properties
        public static Dictionary<ulong, NetworkObject> networkObjects = new Dictionary<ulong, NetworkObject>();
        internal struct FoundGrabbables
        {
            internal GrabbablePlayerObject holderGPO { get; set; }
            internal GrabbablePlayerObject grabbedGPO { get; set; }
        }
        #endregion

        #region Patches


        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        public static void DamagePlayer(PlayerControllerB __instance, int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
        {
            if (__instance == null || !TryFindGrabbableObjectByHolder(__instance.playerClientId, out GrabbablePlayerObject gpo)) return;
            if (causeOfDeath == CauseOfDeath.Suffocation || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned) return;

            // Grabbed player takes half the damage
            gpo.grabbedPlayer.DamagePlayer(damageNumber / 2, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
        }
        
        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
        [HarmonyPrefix]
        public static void KillPlayerClientRpcPrefix(int playerId/*, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation*/)
        {
            Plugin.Log("KillPlayerClientRpcPrefix");

            var targetPlayer = PlayerInfo.ControllerFromID((ulong)playerId);
            if (targetPlayer == null) return;

            ResetAnyPlayerModificationsFor(targetPlayer);

            bool weDied = (ulong)playerId == PlayerInfo.CurrentPlayerID;
            var grabbables = FindGrabbableObjectsFor((ulong)playerId);

            if (grabbables.grabbedGPO == null && grabbables.holderGPO == null)
            {
                Plugin.Log("We aren't in a connection with anyone while dying.");
                return;
            }

            if (grabbables.grabbedGPO) // Player who dies is grabbable
            {
                if(grabbables.grabbedGPO.playerHeldBy != null && grabbables.grabbedGPO.playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                {
                    Plugin.Log("The person we are grabbing is dying -> dropping.");
                    PlayerInfo.CurrentPlayer.DiscardHeldObject();
                }
                if (PlayerInfo.IsHost)
                {
                    Plugin.Log("Delete gpo later");
                    grabbables.grabbedGPO.DeleteNextFrame = true; // Remove grabbable player object after everything is executed
                }
            }

            if(grabbables.holderGPO) // Player who dies is holding someone
            {
                if (weDied) // We die while holding someone
                {
                    Plugin.Log("We die while holding someone -> dropping.");
                    PlayerInfo.CurrentPlayer.DiscardHeldObject();
                }
                else if(grabbables.holderGPO.grabbedPlayerID.Value == PlayerInfo.CurrentPlayerID)
                {
                    Plugin.Log("The person who died is holding us.");
                    grabbables.holderGPO.DiscardItemOnClient();
                    grabbables.holderGPO.playerHeldBy = null;
                }
            }
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
            if (StartOfRound.Instance == null || StartOfRound.Instance.inShipPhase || __instance.isInHangarShipRoom || __instance.isPlayerDead) return;

            // Handles the adjustment of our own GrabbablePlayerObject, aswell as follow someone who teleported (to adjust lighting, weather, items, etc)
            var grabbables = FindGrabbableObjectsFor(__instance.playerClientId);

            if(grabbables.grabbedGPO == null && grabbables.holderGPO == null)
                return; // Teleporting person was in no connection with any other player

            if (grabbables.grabbedGPO) // Player who teleports is grabbed
            {
                if (grabbables.grabbedGPO.playerHeldBy != null && grabbables.grabbedGPO.playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID)
                {
                    Plugin.Log("We're holding the person who teleported.");
                    grabbables.grabbedGPO.StartCoroutine(grabbables.grabbedGPO.UpdateRegionAfterTeleportEnsured(TargetPlayer.GrabbedPlayer));
                }
            }

            if (grabbables.holderGPO) // Player who teleports is holding someone
            {
                if (grabbables.holderGPO.grabbedPlayerID.Value == PlayerInfo.CurrentPlayerID)
                {
                    Plugin.Log("We're held by person who teleported.");
                    grabbables.holderGPO.StartCoroutine(grabbables.holderGPO.UpdateRegionAfterTeleportEnsured(TargetPlayer.Holder));
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
        [HarmonyBefore(["MoreCompany"])]
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

        [HarmonyPatch(typeof(RoundManager), "GenerateNewLevelClientRpc")]
        [HarmonyPostfix]
        public static void GenerateNewLevelClientRpc()
        {
            foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
            {
                if (gpo != null && gpo.grabbedPlayerID != null)
                    gpo.UpdateScanNodeVisibility();
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), "EnablePhysics")]
        [HarmonyPrefix]
        public static void EnablePhysicsPrefix(GrabbableObject __instance, ref bool enable)
        {
            if (__instance is GrabbablePlayerObject && (__instance as GrabbablePlayerObject).IsCurrentPlayer)
                enable = false;
        }

        [HarmonyPatch(typeof(GrabbableObject), "EnablePhysics")]
        [HarmonyPostfix]
        public static void EnablePhysicsPostfix(GrabbableObject __instance, ref bool enable)
        {
            if (__instance is GrabbablePlayerObject)
                (__instance as GrabbablePlayerObject).UpdateScanNodeVisibility();
        }

        [HarmonyPatch(typeof(Shovel), "HitShovel")]
        [HarmonyPostfix]
        public static void HitShovel(bool cancel, RaycastHit[] ___objectsHitByShovel, Shovel __instance)
        {
            if (cancel) return;

            foreach(var obj in ___objectsHitByShovel)
            {
                if(obj.transform != null && obj.transform.TryGetComponent(out GrabbablePlayerObject gpo))
                {
                    if (__instance.playerHeldBy == null || __instance.playerHeldBy.playerClientId != gpo.grabbedPlayerID.Value)
                        gpo.OnGoombaServerRpc(gpo.grabbedPlayerID.Value);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        private static void SetHoverTipAndCurrentInteractTriggerPatch(ref InteractTrigger ___hoveringOverTrigger) // Disable storage closet for shrunken players
        {
            if (___hoveringOverTrigger == null || !ModConfig.Instance.values.cantOpenStorageCloset) return;

            var parentCube = ___hoveringOverTrigger.transform;
            if (parentCube == null) return;

            var parentCube001 = parentCube.parent;
            if (parentCube001 == null) return;

            var storageCloset = parentCube001.parent;
            if(storageCloset == null || storageCloset.name != "StorageCloset") return;

            ___hoveringOverTrigger.interactable = !PlayerInfo.IsCurrentPlayerShrunk;
            if (String.IsNullOrEmpty(___hoveringOverTrigger.disabledHoverTip))
                ___hoveringOverTrigger.disabledHoverTip = "[Too weak]";
        }
        #endregion

        #region Helper
#if DEBUG
        public static string Log
        {
            get
            {
                string output = "GrabbablePlayerList:\n";
                foreach (var gpo in Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>())
                {
                    output += ("------------------------------\n");

                    output += ("GPO: " + (gpo.grabbedPlayer != null ? gpo.grabbedPlayer.name : "No player") + ".\n");
                    output += ("ItemProperties: " + gpo.itemProperties?.itemName + ", " + gpo.itemProperties?.weight + "lb" + ".\n");
                    output += ("IDs: ItemPorperties[" + gpo.itemProperties.GetInstanceID() + "] - " +
                                    "AudioSource[" + (gpo.TryGetComponent(out AudioSource audioSource) ? audioSource.GetInstanceID() : "none") + "] - " +
                                    "NetworkObject[" + (gpo.TryGetComponent(out NetworkObject networkObject) ? networkObject.NetworkObjectId : "none") + "] - " +
                                    "GPO[" + gpo.GetInstanceID() + "]\n");
                }
                output += ("------------------------------\n");
                return output;
            }
        }
#endif

        public static bool TryFindGrabbableObjectForPlayer(ulong playerID, out GrabbablePlayerObject result)
        {
            var grabbables = FindGrabbableObjectsFor(playerID);
            result = grabbables.grabbedGPO;
            return result != null;
        }

        public static bool TryFindGrabbableObjectByHolder(ulong playerHeldByID, out GrabbablePlayerObject result)
        {
            var grabbables = FindGrabbableObjectsFor(playerHeldByID);
            result = grabbables.holderGPO;
            return result != null;
        }

        public static FoundGrabbables FindGrabbableObjectsFor(ulong playerID)
        {
            var grabbables = new FoundGrabbables();
            var gpos = Resources.FindObjectsOfTypeAll<GrabbablePlayerObject>();
            Plugin.Log("FindGrabbableObjectsFor " + playerID + " -> " + gpos.Length);
            foreach (var gpo in gpos)
            {
                if (gpo != null)
                {
                    if (gpo.playerHeldBy != null && gpo.playerHeldBy.playerClientId == playerID)
                        grabbables.holderGPO = gpo;
                    if (gpo.grabbedPlayerID.Value == playerID)
                        grabbables.grabbedGPO = gpo;
                }
            }
            return grabbables;
        }
        #endregion

        #region Methods
        public static void ClearGrabbablePlayerObjects()
        {
            Plugin.Log("ClearGrabbablePlayerObjects");

            if (!PlayerInfo.IsHost) return;

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
                if (networkObjects[playerID] == null)
                    networkObjects.Remove(playerID);
                else if (networkObjects[playerID].IsSpawned)
                {
                    Plugin.Log("Player " + playerID + " already grabbable!");
                    return;
                }
                else
                    UnityEngine.Object.Destroy(networkObjects[playerID]);
            }

            networkObjects[playerID] = GrabbablePlayerObject.Instantiate(playerID);
            Plugin.Log("NEW GRABBALEPLAYER COUNT: " + networkObjects.Count);
        }

        public static void RemovePlayerGrabbable(ulong playerID)
        {
            if (!PlayerInfo.IsHost)
            {
                Plugin.Log("RemovePlayerGrabbable called from client. This shouldn't happen!", Plugin.LogType.Warning);
                return;
            }

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

            DespawnGrabbablePlayer(playerID);
        }

        public static void DespawnGrabbablePlayer(ulong playerID)
        {
            if (PlayerInfo.IsHost && networkObjects[playerID].IsSpawned)
                networkObjects[playerID].Despawn();

            networkObjects.Remove(playerID);
        }

        public static void ReInitializePlayerGrabbable(ulong playerID)
        {
            RemovePlayerGrabbable(playerID);
            if(PlayerInfo.ControllerFromID(playerID) != null)
                SetPlayerGrabbable(playerID);
        }

        public static void ResetAnyPlayerModificationsFor(PlayerControllerB targetPlayer)
        {
            Plugin.Log("ResetAnyPlayerModificationsFor");
            if (targetPlayer == null) return;

            if (targetPlayer.gameObject != null)
            {
                targetPlayer.gameObject.transform.localScale = Vector3.one;

                if(targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID)
                    PlayerInfo.ScaleLocalPlayerBodyParts();
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
