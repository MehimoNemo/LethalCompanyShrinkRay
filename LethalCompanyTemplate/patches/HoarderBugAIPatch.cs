using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.XR;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class HoarderBugAIPatch
    {
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
            if (ModConfig.Instance.values.hoardingBugSteal) return;

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
            Plugin.Log("Aiming for us.");

            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
            {
                Plugin.Log("Found gpo.");
                if (!IsGrabbablePlayerTargetable(gpo)) return;

                __instance.targetItem = gpo;

                Plugin.Log("Forget everything else.. we found a grabbable player! Let's goooo..!");
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

                if (!HoarderBugAI.grabbableObjectsInMap.Contains(gpo.gameObject))
                    HoarderBugAI.grabbableObjectsInMap.Add(gpo.gameObject);
                return;
            }

            var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, PlayerInfo.CurrentPlayer.transform.position);
            Plugin.Log("Distance nest->player: " + distanceToNest);
            if (distanceToNest < 10f)
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
                gpo.lastHoarderBugGrabbedBy.targetItem = gpo;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState(0);
                HoarderBugAI.HoarderBugItems.RemoveAll(item => item.itemGrabbableObject == gpo);
                Plugin.Log("Moved too far away from hoarder bug nest. Bug tries to get you back!");
            }

            gpo.lastHoarderBugGrabbedBy = null; // Forget about it after that
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

        public static bool SetDestinationToPosition(Vector3 position, bool checkForPath = false, HoarderBugAI enemyAI = null)
        {
            Plugin.Log("SetDestinationToPosition -> " + position);
            if (checkForPath)
            {
                position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 1.75f);
                Plugin.Log("1 -> " + position);
                var path1 = new NavMeshPath();
                if (!enemyAI.agent.CalculatePath(position, path1))
                {
                    Plugin.Log("2");
                    return false;
                }

                var path1Pos = path1.corners[path1.corners.Length - 1];
                var navMeshPos = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f);
                var distance = Vector3.Distance(path1Pos, navMeshPos);
                Plugin.Log("3 -> " + path1Pos + " | " + navMeshPos + " | " + distance);
                if (distance > 1.55f)
                {
                    Plugin.Log("3");
                    return false;
                }
            }

            enemyAI.moveTowardsDestination = true;
            enemyAI.movingTowardsTargetPlayer = false;
            enemyAI.destination = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, -1f);
            Plugin.Log("4 -> " + enemyAI.destination);
            return true;
        }

        [HarmonyPatch(typeof(HoarderBugAI), "SetGoTowardsTargetObject")]
        [HarmonyPostfix]
        public static void SetGoTowardsTargetObjectPostfix(GameObject foundObject, HoarderBugAI __instance)
        {
            Plugin.Log("HoarderBug found [" + foundObject.name + "]" + (__instance.targetItem != null ? (" and set target to [" + __instance.targetItem.name + "].") : ". No target found."));
        }
    }
}
