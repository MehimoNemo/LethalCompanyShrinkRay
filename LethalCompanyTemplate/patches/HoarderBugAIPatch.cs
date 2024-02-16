using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class HoarderBugAIPatch
    {
        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck, EnemyType ___enemyType)
        {
            if (__result && ModConfig.Instance.values.hoardingBugSteal && ___enemyType.name == "HoarderBug")
                return !PlayerInfo.IsShrunk(playerScript);

            return __result;
        }

        [HarmonyPatch(typeof(HoarderBugAI), "RefreshGrabbableObjectsInMapList")]
        [HarmonyPostfix]
        public static void RefreshGrabbableObjectsInMapList()
        {
            if (!ModConfig.Instance.values.hoardingBugSteal) return;

            // Remove all GrabbablePlayerObjects from the list
            Plugin.Log("Previously grabbable hoarder bug objects: " + HoarderBugAI.grabbableObjectsInMap.Count.ToString());
            HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject _));
            Plugin.Log("Grabbable hoarder bug objects after change: " + HoarderBugAI.grabbableObjectsInMap.Count.ToString());
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

        [HarmonyPatch(typeof(HoarderBugAI), "SetGoTowardsTargetObject")]
        [HarmonyPostfix]
        public static void SetGoTowardsTargetObject(GameObject foundObject, HoarderBugAI __instance)
        {
            Plugin.Log("HoarderBug found [" + foundObject.name + "]" + __instance.targetItem != null ? (" and set target to [" + __instance.targetItem.name + "].") : ". No target found.");
        }

        /*[HarmonyPatch(typeof(HoarderBugAI), "GrabItem")]
        [HarmonyPostfix]
        public static void GrabItem(EnemyAI __instance, NetworkObject item)
        {
            if(item.gameObject.TryGetComponent(out GrabbablePlayerObject gpo))
                gpo.GrabItemFromEnemy(__instance);
        }
        [HarmonyPatch(typeof(HoarderBugAI), "DropItem")]
        [HarmonyPostfix]
        public static void DropItem(NetworkObject item, Vector3 targetFloorPosition, bool droppingInNest = true)
        {
            if (item.gameObject.TryGetComponent(out GrabbablePlayerObject gpo))
                gpo.DiscardItemFromEnemy();
        }*/

        [HarmonyPatch(typeof(EnemyAI), "CheckLineOfSight")]
        [HarmonyPostfix]
        public static GameObject CheckLineOfSight(GameObject __result, List<GameObject> objectsToLookFor, float width = 45f, int range = 60, float proximityAwareness = -1f)
        {
            if (__result == null) return __result;

            /*string output = "CheckLineOfSight found " + __result.name + ", while looking for:\n";
            foreach(var obj in objectsToLookFor)
                output += obj.name + " | ";
            Plugin.Log(output);*/

            if (!__result.name.StartsWith("grabbable_")) return __result;

            if(__result.TryGetComponent(out GrabbablePlayerObject gpo) && !gpo.isPocketed)
                    Plugin.Log("CheckLineOfSight found a grabbable object!!!");

            return __result;
        }
    }
}
