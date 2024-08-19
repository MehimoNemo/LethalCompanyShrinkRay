using HarmonyLib;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class ModdedDungeonEntrancePatch
    {
        [HarmonyPatch(typeof(EntranceTeleport), "TeleportPlayer")]
        [HarmonyPostfix]
        public static void TeleportPlayer(EntranceTeleport __instance, bool ___isEntranceToBuilding)
        {
            if (!___isEntranceToBuilding) return;
            
            if (RoundManager.Instance.currentDungeonType <= 3) return; // vanilla dungeon
            
            PlayerInfo.CurrentPlayer.gameObject.transform.position += new Vector3(0f, 1 - PlayerInfo.CurrentPlayerScale, 0f);
            Plugin.Log("Adjusted player pos on entrance.");
        }
    }
}
