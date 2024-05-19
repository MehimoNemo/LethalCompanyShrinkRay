using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using LittleCompany.modifications;
using System.Linq;
using System.Collections;

namespace LittleCompany.components
{
    [HarmonyPatch]
    internal class GrabbablePlayerList
    {
        #region Properties
        public static Dictionary<ulong, GrabbablePlayerObject> GrabbablePlayerObjects = new Dictionary<ulong, GrabbablePlayerObject>();
        internal struct FoundGrabbables
        {
            internal GrabbablePlayerObject holderGPO { get; set; }
            internal GrabbablePlayerObject grabbedGPO { get; set; }
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        public static void DamagePlayer(PlayerControllerB __instance, int damageNumber, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false)
        {
            // Local method
            if (__instance == null || !TryFindGrabbableObjectByHolder(__instance.playerClientId, out GrabbablePlayerObject gpo)) return;

            // todo: check if grabbed player is drowning or suffocating too
            if (causeOfDeath == CauseOfDeath.Suffocation || causeOfDeath == CauseOfDeath.Drowning || causeOfDeath == CauseOfDeath.Abandoned) return;

            // Grabbed player takes half the damage
            gpo.DamageGrabbedPlayerServerRpc(damageNumber / 2, causeOfDeath, deathAnimation, fallDamage);
        }
        
        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
        [HarmonyPrefix]
        public static void KillPlayerClientRpcPrefix(int playerId, PlayerControllerB __instance/*, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation*/)
        {
            Plugin.Log("KillPlayerClientRpcPrefix");

            var targetPlayer = PlayerInfo.ControllerFromID((ulong)playerId);
            if(targetPlayer == null)
            {
                Plugin.Log("Unable to find player script for dying player", Plugin.LogType.Error);
                return;
            }

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

        [HarmonyPrefix, HarmonyPatch(typeof(StartOfRound), "FirePlayersAfterDeadlineClientRpc")]
        public static void FirePlayersAfterDeadlineClientRpc()
        {
            StartOfRound.Instance.StartCoroutine(ResetLocalPlayerOnShipDoorOpen());
        }

        public static IEnumerator ResetLocalPlayerOnShipDoorOpen()
        {
            yield return new WaitUntil(() => StartOfRound.Instance.suckingPlayersOutOfShip);
            yield return new WaitForSeconds(0.5f);

            if (PlayerInfo.CurrentPlayerHeldItem is GrabbablePlayerObject)
                PlayerInfo.CurrentPlayer.DiscardHeldObject();

            yield return new WaitWhile(() => StartOfRound.Instance.suckingFurnitureOutOfShip);

            foreach (var player in PlayerInfo.AllPlayers)
                ResetAnyPlayerModificationsFor(player);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        public static void DespawnPropsAtEndOfRoundPre(RoundManager __instance)
        {
			if (!__instance.IsServer)
			                return;
			
            var gpoList = Resources.FindObjectsOfTypeAll(typeof(GrabbablePlayerObject)); // Don't destroy these. Workaround
            foreach (var gpo in gpoList)
                gpo.hideFlags = HideFlags.HideAndDontSave;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        public static void DespawnPropsAtEndOfRoundPost(RoundManager __instance)
        {
            if (!__instance.IsServer)
                return;

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

            if (grabbables.grabbedGPO) // Player who teleports is grabbable
            {
                if (grabbables.grabbedGPO.playerHeldBy != null && grabbables.grabbedGPO.playerHeldBy.playerClientId == PlayerInfo.CurrentPlayerID) // Player who teleports is grabbed
                {
                    Plugin.Log("We're holding the person who teleported.");
                    grabbables.grabbedGPO.StartCoroutine(grabbables.grabbedGPO.UpdateRegionAfterTeleportEnsured(GrabbablePlayerObject.TargetPlayer.GrabbedPlayer));
                }
            }

            if (grabbables.holderGPO) // Player who teleports is holding someone
            {
                if (grabbables.holderGPO.grabbedPlayerID.Value == PlayerInfo.CurrentPlayerID)
                {
                    Plugin.Log("We're held by person who teleported.");
                    grabbables.holderGPO.StartCoroutine(grabbables.holderGPO.UpdateRegionAfterTeleportEnsured(GrabbablePlayerObject.TargetPlayer.Holder));
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "IsInSpecialAnimationClientRpc")]
        [HarmonyPostfix]
        public static void IsInSpecialAnimationClientRpc(PlayerControllerB __instance)
        {
            if(TryFindGrabbableObjectForPlayer(__instance.playerClientId, out GrabbablePlayerObject gpo)) // Used to disable trigger when climbing ladders
                gpo.UpdateInteractTrigger();
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
                foreach (var (playerID, gpo) in GrabbablePlayerObjects)
                {
                    output += ("------------------------------\n");

                    output += ("GPO for player " + playerID + ".\n");
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
            return GrabbablePlayerObjects.TryGetValue(playerID, out result);
        }

        public static bool TryFindGrabbableObjectByHolder(ulong playerHeldByID, out GrabbablePlayerObject result)
        {
            result = FindGrabbableObjectsFor(playerHeldByID).holderGPO;
            return result != null;
        }

        public static FoundGrabbables FindGrabbableObjectsFor(ulong targetPlayerID)
        {
            var grabbables = new FoundGrabbables();
            foreach (var (playerID, gpo) in GrabbablePlayerObjects)
            {
                if (gpo.playerHeldBy != null && gpo.playerHeldBy.playerClientId == targetPlayerID)
                    grabbables.holderGPO = gpo;
                if (playerID == targetPlayerID)
                    grabbables.grabbedGPO = gpo;
            }
            return grabbables;
        }
        #endregion

        #region Methods
        public static void ClearGrabbablePlayerObjects()
        {
            Plugin.Log("ClearGrabbablePlayerObjects");

            if (!PlayerInfo.IsHost) return;

            for(int i = GrabbablePlayerObjects.Count - 1; i >= 0; i--)
                RemovePlayerGrabbable(GrabbablePlayerObjects.ElementAt(i).Value);
        }

        public static bool SetPlayerGrabbable(ulong playerID, out GrabbablePlayerObject gpo)
        {
            gpo = null;
            if (!PlayerInfo.IsHost)
            {
                Plugin.Log("SetPlayerGrabbable called from client. This shouldn't happen!", Plugin.LogType.Warning);
                return false;
            }

            if (GrabbablePlayerObjects.ContainsKey(playerID))
            {
                Plugin.Log("Player " + playerID + " already grabbable!");
                gpo = GrabbablePlayerObjects[playerID];
                return false;
            }

            gpo = GrabbablePlayerObject.Instantiate(playerID);
            return true;
        }

        public static bool RemovePlayerGrabbable(ulong playerID)
        {
            if (!PlayerInfo.IsHost) return false;
            if (!GrabbablePlayerObjects.TryGetValue(playerID, out GrabbablePlayerObject gpo))
            {
                Plugin.Log("RemovePlayerGrabbable -> Player wasn't grabbable.");
                return false;
            }

            return RemovePlayerGrabbable(gpo);
        }

        public static bool RemovePlayerGrabbable(GrabbablePlayerObject gpo)
        {
            if (!gpo.TryGetComponent(out NetworkObject networkObject) || !networkObject.IsSpawned)
                return false;

            networkObject.Despawn();
            return true;
        }

        public static void ReInitializePlayerGrabbable(ulong playerID)
        {
            RemovePlayerGrabbable(playerID);
            if(PlayerInfo.ControllerFromID(playerID) != null)
                SetPlayerGrabbable(playerID, out _);
        }

        public static void ResetAnyPlayerModificationsFor(PlayerControllerB targetPlayer)
        {
            Plugin.Log("ResetAnyPlayerModificationsFor");
            if (targetPlayer == null) return;

            if (!PlayerInfo.IsDefaultSize(targetPlayer))
                PlayerModification.ApplyModificationTo(targetPlayer, Modification.ModificationType.Normalizing, null);
        }

        public static void UpdateWhoIsGrabbableFromPerspectiveOf(PlayerControllerB targetPlayer)
        {
            Plugin.Log("UpdateWhoIsGrabbableFromPerspectiveOf");
            if(targetPlayer == null) return;

            if(targetPlayer.playerClientId != PlayerInfo.CurrentPlayerID)
            {
                // Someone else's size changed
                if(TryFindGrabbableObjectForPlayer(targetPlayer.playerClientId, out GrabbablePlayerObject gpo))
                    gpo.UpdateInteractTrigger();
            }
            else
            {
                // We changed size
                foreach (var gpo in GrabbablePlayerObjects.Values)
                {
                    if (gpo == null) continue;
                    gpo.UpdateInteractTrigger();
                }
            }

            if(PlayerInfo.IsHost)
            {
                var largestGrabberSize = Mathf.Max(PlayerInfo.LargestPlayerSize, EnemyInfo.LargestGrabbingEnemy);
                foreach (var player in PlayerInfo.AlivePlayers)
                {
                    if (PlayerInfo.SmallerThan(player, largestGrabberSize)) // Make anyone grabbable who's smaller than the largest player / enemy
                        SetPlayerGrabbable(player.playerClientId, out _);
                    else
                        RemovePlayerGrabbable(player.playerClientId);
                }
            }
        }
        #endregion
    }
}
