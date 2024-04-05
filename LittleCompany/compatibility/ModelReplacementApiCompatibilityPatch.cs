using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using System.Collections.Generic;
namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class ModelReplacementApiCompatibilityPatch
    {
        public static Dictionary<ulong, int> PlayerToSuit = new Dictionary<ulong, int>();
        public static Dictionary<ulong, bool> SuitChangedDetectedLastFrame = new Dictionary<ulong, bool>();

        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void DetectSuitChange_LateUpdate_Postfix(PlayerControllerB __instance)
        {
            if (SuitChangedDetectedLastFrame.GetValueOrDefault(__instance.playerClientId, false))
            {
                // Reload the suit
                Plugin.Log("Reload the suit");
                __instance.GetComponent<PlayerScaling>()?.modelReplacementApiCompatibility?.ReloadCurrentReplacementModel();
                PlayerInfo.RebuildRig(__instance);
                SuitChangedDetectedLastFrame[__instance.playerClientId] = false;
            }
            else if (PlayerChangedSuit(__instance))
            {
                Plugin.Log("DetectSuitChange");
                // Update the suit in the map
                PlayerToSuit[__instance.playerClientId] = __instance.currentSuitID;
                SuitChangedDetectedLastFrame[__instance.playerClientId] = true;
            }
        }

        public static bool PlayerChangedSuit(PlayerControllerB pcb)
        {
            return PlayerToSuit.GetValueOrDefault(pcb.playerClientId, -1) != pcb.currentSuitID;
        }
    }
}