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
            if (__result && ModConfig.Instance.values.hoardingBugSteal && __instance is HoarderBugAI)
                return !PlayerInfo.IsShrunk(playerScript);

            return __result;
        }

        [HarmonyPatch(typeof(HoarderBugAI), "RefreshGrabbableObjectsInMapList")]
        [HarmonyPostfix]
        public static void RefreshGrabbableObjectsInMapList()
        {
            if (ModConfig.Instance.values.hoardingBugSteal)
            {
                HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject gpo) && gpo.lastHoarderBugGrabbedBy != null); // remove still grabbed ones
                return;
            }

            // Remove all GrabbablePlayerObjects from the list
            Plugin.Log("Previously grabbable hoarder bug objects: " + HoarderBugAI.grabbableObjectsInMap.Count.ToString());
            HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject _));
            Plugin.Log("Grabbable hoarder bug objects after change: " + HoarderBugAI.grabbableObjectsInMap.Count.ToString());
        }

        [HarmonyPatch(typeof(HoarderBugAI), "DetectNoise")]
        [HarmonyPostfix]
        public static void DetectNoise(HoarderBugAI __instance)
        {
            if (!ModConfig.Instance.values.hoardingBugSteal || !PlayerInfo.IsCurrentPlayerShrunk) return; // Not targetable

            if (__instance.targetItem != null && __instance.targetItem is GrabbablePlayerObject) return; // Already targeting a player

            var inLineOfSight = __instance.HasLineOfSightToPosition(PlayerInfo.CurrentPlayer.transform.position);
            if (!inLineOfSight) return;
            Plugin.Log("HoarderBug aiming for us.");

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
            {
                Plugin.Log("Found gpo. May it be targetable?");
                if (!IsGrabbablePlayerTargetable(gpo)) return;

                Plugin.Log("Forget everything else.. we found a grabbable player! Let's goooo..!");
                __instance.targetItem = gpo;
            }
        }

        public static bool IsGrabbablePlayerTargetable(GrabbablePlayerObject gpo)
        {
            if (gpo.lastHoarderBugGrabbedBy != null)
                return false;

            return !gpo.isHeld || Random.Range(0, 100) < 25;
        }

        public static void HoarderBugEscapeRoutineForGrabbablePlayer(GrabbablePlayerObject gpo)
        {
            if (gpo.lastHoarderBugGrabbedBy.isEnemyDead || gpo.lastHoarderBugGrabbedBy.nestPosition == null)
            {
                Plugin.Log("The hoarder bug who grabbed us died or lost their nest. Poor bug.");
                gpo.lastHoarderBugGrabbedBy = null;

                HoarderBugAI.HoarderBugItems.RemoveAll(item => item.itemGrabbableObject == gpo);
                if (!HoarderBugAI.grabbableObjectsInMap.Contains(gpo.gameObject))
                    HoarderBugAI.grabbableObjectsInMap.Add(gpo.gameObject);
                return;
            }

            var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, PlayerInfo.CurrentPlayer.transform.position);
            Plugin.Log("Difference between nest pos (" + gpo.lastHoarderBugGrabbedBy.nestPosition + ") and current pos (" + PlayerInfo.CurrentPlayer.transform.position + ") is " + distanceToNest);
            if (distanceToNest < maxAllowedNestDistance)
                return;

            Plugin.Log("Overly attached hoarding bug feels that the grabbed player is too far away. Following!");

            if (gpo.isHeld && gpo.playerHeldBy != null)
            {
                // angry mode
                foreach (var hoarderBugItem in HoarderBugAI.HoarderBugItems)
                {
                    if (hoarderBugItem.itemGrabbableObject != null && hoarderBugItem.itemGrabbableObject.name == gpo.name)
                        hoarderBugItem.status = HoarderBugItemStatus.Stolen;
                }

                gpo.lastHoarderBugGrabbedBy.targetPlayer = gpo.playerHeldBy;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState(2);
                Plugin.Log("HoarderBug saw that player " + gpo.playerHeldBy.name + " stole " + gpo.name + ". Is angry now at them!");
            }
            else
            {
                // try to get it back!
                if (distanceToNest < (maxAllowedNestDistance + 5f))
                {
                    gpo.lastHoarderBugGrabbedBy.targetItem = gpo;
                    gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState(0);
                    Plugin.Log("Moved too far away from hoarder bug nest. Bug tries to get you back!");
                }
                else
                    Plugin.Log("Escaped from the hoarding bug!"); // Very likely we teleported (by using vent, shipTeleporter, ..)

                HoarderBugAI.HoarderBugItems.RemoveAll(item => item.itemGrabbableObject == gpo);
                if (!HoarderBugAI.grabbableObjectsInMap.Contains(gpo.gameObject))
                    HoarderBugAI.grabbableObjectsInMap.Add(gpo.gameObject);
            }

            gpo.lastHoarderBugGrabbedBy = null; // Forget about it after that
        }

        [HarmonyPatch(typeof(HoarderBugAI), "SetGoTowardsTargetObject")]
        [HarmonyPrefix]
        public static void SetGoTowardsTargetObjectPrefix(ref GameObject foundObject, HoarderBugAI __instance)
        {
            if (!foundObject.TryGetComponent(out GrabbablePlayerObject gpo))
                return;

            if (!IsGrabbablePlayerTargetable(gpo))
                HoarderBugAI.grabbableObjectsInMap.Remove(foundObject);

            //Plugin.Log("SetGoTowardsTargetObject -> foundObject position: " + foundObject.transform.position + ". gpo position: " + gpo.transform.position);
        }

        // ------------------ DEBUG FROM HERE ON ------------------
        public static Vector3 latestNestPosition = Vector3.zero;
        [HarmonyPatch(typeof(HoarderBugAI), "SyncNestPositionClientRpc")]
        [HarmonyPostfix]
        public static void SyncNestPositionClientRpc(Vector3 newNestPosition)
        {
            if (!ModConfig.DebugMode) return;

            latestNestPosition = newNestPosition;
        }
    }
}
