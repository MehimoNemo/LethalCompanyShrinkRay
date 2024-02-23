using HarmonyLib;
using LCShrinkRay.helper;
using UnityEngine;

namespace LCShrinkRay.compatibility
{
    [HarmonyPatch]
    internal class ModdedDungeonEntrancePatch
    {
        [HarmonyPatch(typeof(EntranceTeleport), "TeleportPlayer")]
        [HarmonyPostfix]
        public static void TeleportPlayer(EntranceTeleport __instance, bool ___isEntranceToBuilding)
        {
            if (!___isEntranceToBuilding) return;

            if (__instance.dungeonFlowId <= 2) return; // vanilla dungeon
            
            PlayerInfo.CurrentPlayer.gameObject.transform.position += new Vector3(0f, 1 - PlayerInfo.CurrentPlayerScale, 0f);
            Plugin.Log("Adjusted player pos on entrance.");
        }
    }
}
