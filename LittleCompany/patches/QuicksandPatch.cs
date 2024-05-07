using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class QuicksandPatch
    {
        // Sinking speed
        /*[HarmonyPatch(typeof(PlayerControllerB), "StartSinkingClientRpc")]
        [HarmonyPrefix]
        private static void StartSinkingClientRpc(PlayerControllerB __instance, ref float sinkingSpeed)
        {
            sinkingSpeed = 0.15f * PlayerInfo.SizeOf(__instance);
        }*/

        // Sinking depth
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        private static void Update(PlayerControllerB __instance)
        {
            if ( __instance.isPlayerControlled && __instance.isSinking)
            {
                var adjustedMaxDepth = 2.8f + (PlayerInfo.SizeOf(__instance) - 1f) * 2.8f;
                __instance.meshContainer.position = Vector3.Lerp(__instance.transform.position, __instance.transform.position - Vector3.up * adjustedMaxDepth, StartOfRound.Instance.playerSinkingCurve.Evaluate(__instance.sinkingValue));
            }
        }

    }       
}
