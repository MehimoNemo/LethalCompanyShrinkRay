using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class HoarderBugAIPatch
    {
        private const float maxAllowedNestDistance = 10f;

        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, EnemyAI __instance)
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.NoGrab)
                return __result;

            if (__result && __instance is HoarderBugAI)
                return !PlayerInfo.IsShrunk(playerScript);

            return __result;
        }

        [HarmonyPatch(typeof(HoarderBugAI), "RefreshGrabbableObjectsInMapList")]
        [HarmonyPostfix]
        public static void RefreshGrabbableObjectsInMapList()
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour != ModConfig.HoardingBugBehaviour.NoGrab)
            {
                HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject gpo) && (gpo.InLastHoardingBugNestRange.Value || !gpo.grabbableToEnemies)); // remove still grabbed ones
                return;
            }

            HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject _));
        }

        [HarmonyPatch(typeof(HoarderBugAI), "DetectNoise")]
        [HarmonyPostfix]
        public static void DetectNoise(HoarderBugAI __instance, ref float ___timeSinceLookingTowardsNoise)
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour != ModConfig.HoardingBugBehaviour.Addicted || !PlayerInfo.IsCurrentPlayerShrunk) return; // Not targetable

            if (__instance.heldItem != null && __instance.heldItem.itemGrabbableObject != null && __instance.heldItem.itemGrabbableObject as GrabbablePlayerObject != null)
                return; // Chill, you already got one..

            if (__instance.targetItem != null)
            {
                Plugin.Log("DetectNoise. Target item: " + __instance.targetItem.name + " with position: " + __instance.targetItem.transform.position);
                if (__instance.targetItem is GrabbablePlayerObject)
                {
                    ___timeSinceLookingTowardsNoise = 0f; // Set this to avoid switching to behaviour 1 (which is "return to nest")
                    Plugin.Log("targetItem: player position -> " + (__instance.targetItem as GrabbablePlayerObject).grabbedPlayer.transform.position);
                    return; // Already targeting a player
                }
            }

            var inLineOfSight = __instance.HasLineOfSightToPosition(PlayerInfo.CurrentPlayer.transform.position);
            if (!inLineOfSight) return;

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
            {
                if (!IsGrabbablePlayerTargetable(gpo))
                {
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " not targetable.");
                    return;
                }

                Plugin.Log("Forget everything else.. we found a grabbable player! Let's goooo..!");
                gpo.HoardingBugTargetUsServerRpc(__instance.NetworkObjectId);
                ___timeSinceLookingTowardsNoise = 0f; // Set this to avoid switching to behaviour 1 (which is "return to nest")
                return;
            }
        }

        public static bool IsGrabbablePlayerTargetable(GrabbablePlayerObject gpo)
        {
            return !gpo.InLastHoardingBugNestRange.Value;
        }

        public static void AddToGrabbables(GrabbablePlayerObject gpo)
        {
            if (!HoarderBugAI.grabbableObjectsInMap.Contains(gpo.gameObject))
                HoarderBugAI.grabbableObjectsInMap.Add(gpo.gameObject);
            HoarderBugAI.HoarderBugItems.RemoveAll(item => item.itemGrabbableObject.NetworkObjectId == gpo.NetworkObjectId);
        }

        public static void HoardingBugTargetUs(HoarderBugAI hoarderBug, GrabbablePlayerObject gpo)
        {
            if (gpo.playerHeldBy != null && ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.Addicted)
            {
                Plugin.Log("Oh you better drop that beautiful player, my friend!");
                hoarderBug.targetItem = gpo;
                hoarderBug.StopSearch(hoarderBug.searchForItems, clear: false);
                gpo.lastHoarderBugGrabbedBy.angryAtPlayer = gpo.playerHeldBy;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState(2);
                return;
            }

            if (hoarderBug.heldItem != null && hoarderBug.heldItem.itemGrabbableObject != null)
                hoarderBug.DropItemServerRpc(hoarderBug.heldItem.itemGrabbableObject.NetworkObject, hoarderBug.transform.position, false);
            hoarderBug.targetItem = gpo;
            hoarderBug.StopSearch(hoarderBug.searchForItems, clear: false);
            hoarderBug.SwitchToBehaviourState(0);
        }

        public static void HoarderBugEscapeRoutineForGrabbablePlayer(GrabbablePlayerObject gpo)
        {
            if (gpo.lastHoarderBugGrabbedBy.isEnemyDead || gpo.lastHoarderBugGrabbedBy.nestPosition == null)
            {
                gpo.MovedOutOfHoardingBugNestRangeServerRpc(true);
            }

            var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, gpo.grabbedPlayer.transform.position);
            //Plugin.Log("Difference between nest pos (" + gpo.lastHoarderBugGrabbedBy.nestPosition + ") and current pos (" + PlayerInfo.CurrentPlayer.transform.position + ") is " + distanceToNest);
            if (distanceToNest < maxAllowedNestDistance)
                return;

            Plugin.Log("Overly attached hoarding bug feels that the grabbed player is too far away. Following!");
            gpo.MovedOutOfHoardingBugNestRangeServerRpc(false);
        }

        public static void MovedOutOfHoardingBugNestRange(GrabbablePlayerObject gpo)
        {
            bool shouldDropItems = false;
            if (gpo.playerHeldBy != null)
            {
                foreach (var hoarderBugItem in HoarderBugAI.HoarderBugItems)
                {
                    if (hoarderBugItem.itemGrabbableObject != null && hoarderBugItem.itemGrabbableObject.name == gpo.name)
                        hoarderBugItem.status = HoarderBugItemStatus.Stolen;
                }
                gpo.lastHoarderBugGrabbedBy.targetItem = gpo;
                gpo.lastHoarderBugGrabbedBy.StopSearch(gpo.lastHoarderBugGrabbedBy.searchForItems, clear: false);
                gpo.lastHoarderBugGrabbedBy.angryAtPlayer = gpo.playerHeldBy;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState(2);
                shouldDropItems = true;
                Plugin.Log("HoarderBug saw that " + gpo.playerHeldBy.name + " stole " + gpo.name + ". Is angry now at them!");
            }
            else
            {
                var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, gpo.grabbedPlayer.transform.position);
                if (distanceToNest < (maxAllowedNestDistance + 5f))
                {
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " moved too far away from hoarder bug nest.");
                    if (ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.Addicted)
                    {
                        Plugin.Log(" Bug tries to get them back!");
                        HoardingBugTargetUs(gpo.lastHoarderBugGrabbedBy, gpo);
                        shouldDropItems = true;
                    }
                }
                else
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " escaped from the hoarding bug!"); // Very likely we teleported (by using vent, shipTeleporter, ..)
            }

            if(shouldDropItems && gpo.lastHoarderBugGrabbedBy.heldItem != null && gpo.lastHoarderBugGrabbedBy.heldItem.itemGrabbableObject != null)
                gpo.lastHoarderBugGrabbedBy.DropItemServerRpc(gpo.lastHoarderBugGrabbedBy.heldItem.itemGrabbableObject.NetworkObject, gpo.lastHoarderBugGrabbedBy.transform.position, false);
        }

        [HarmonyPatch(typeof(HoarderBugAI), "SetGoTowardsTargetObject")]
        [HarmonyPrefix]
        public static void SetGoTowardsTargetObjectPrefix(ref GameObject foundObject, HoarderBugAI __instance)
        {
            if (__instance.heldItem != null && __instance.heldItem.itemGrabbableObject != null && __instance.heldItem.itemGrabbableObject as GrabbablePlayerObject != null)
            {
                Plugin.Log("Ayy?! Still holding a player, you forgot?! Nononono..");
                foundObject = __instance.heldItem.itemGrabbableObject.gameObject; // Hopefully this won't break stuff..
                __instance.SwitchToBehaviourState(1);
            }

            if (!foundObject.TryGetComponent(out GrabbablePlayerObject gpo))
                return;

            if (!IsGrabbablePlayerTargetable(gpo))
            {
                Plugin.Log("SetGoTowardsTargetObject: Player not targetable for hoarding bug");
                HoarderBugAI.grabbableObjectsInMap.Remove(foundObject);
                return;
            }
        }

        // ------------------ DEBUG FROM HERE ON ------------------
        [HarmonyPatch(typeof(HoarderBugAI), "SyncNestPositionClientRpc")]
        [HarmonyPostfix]
        public static void SyncNestPositionClientRpc(Vector3 newNestPosition)
        {
            if (!ModConfig.DebugMode) return;

            DebugPatches.hoarderBugNestPosition = newNestPosition;
        }

        /*[HarmonyPatch(typeof(HoarderBugAI), "DoAIInterval")]
        [HarmonyPostfix]
        public static void DoAIInterval(HoarderBugAI __instance)
        {
            if (!ModConfig.DebugMode) return;

            Plugin.Log("behaviourState: " + __instance.currentBehaviourStateIndex + " | targetItem: " + __instance.targetItem);
        }*/
    }
}
