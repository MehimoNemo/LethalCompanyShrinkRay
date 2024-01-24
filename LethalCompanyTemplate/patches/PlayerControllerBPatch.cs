using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        public struct DefaultPlayerValues
        {
            public float jumpForce { get; set; }
            public float sprintMultiplier { get; set; }
        }
        public static DefaultPlayerValues defaultPlayerValues;
        public static bool defaultsInitialized = false, modified = false;

        //static bool logShowed = false, log2Showed = false;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if(__instance.playerClientId != PlayerHelper.currentPlayer().playerClientId)
                return;

            Shrinking.Instance.Update(); // maybe put that in shrinking?

            if (!defaultsInitialized)
            {
                defaultPlayerValues = new DefaultPlayerValues();
                defaultPlayerValues.jumpForce = ___jumpForce;
                defaultPlayerValues.sprintMultiplier = ___sprintMultiplier;
                defaultsInitialized = true;
            }

            // Speed & Jump Multiplier for shrunken players
            var scale = PlayerHelper.currentPlayerScale();
            if (PlayerHelper.isShrunk(scale) && scale > 0f)
            {
                ___jumpForce = defaultPlayerValues.jumpForce * ModConfig.Instance.values.jumpHeightMultiplier;

                ___sprintMultiplier = defaultPlayerValues.sprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier;
                if (__instance.isSprinting)
                    ___sprintMultiplier *= 2.25f;

                //__instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
            }
            else if (modified)
            {
                ___jumpForce = defaultPlayerValues.jumpForce;
                ___sprintMultiplier = defaultPlayerValues.sprintMultiplier;
                if (__instance.isSprinting)
                    ___sprintMultiplier *= 2.25f;
                modified = false;
            }
        }
    }

    // todo: itemActive maybe here?
}




