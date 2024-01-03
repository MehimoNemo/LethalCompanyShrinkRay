using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance)
        {
            //SoundManager.Instance.playerVoicePitchTargets[__instance.playerClientId] = 1.2f;
            Shrinking.Instance.Update();
        }
    }
}




