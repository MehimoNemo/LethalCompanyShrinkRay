using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        public static float defaultJumpForce { get; set; }
        public static float defaultSprintMultiplier { get; set; }

        public static bool defaultsInitialized = false;
        public static bool modified = false;

        //static bool logShowed = false, log2Showed = false;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            Shrinking.Instance.Update(); // todo: remove once Shrinking.Update() is gone

            if (!defaultsInitialized)
            {
                defaultJumpForce = ___jumpForce;
                defaultSprintMultiplier = ___sprintMultiplier;
            }

            // Speed & Jump Multiplier for shrunken players
            var isShrunk = PlayerHelper.isCurrentPlayerShrunk();
            if (isShrunk) // Modifying sadly has to be done each frame
            {
                ___jumpForce = defaultJumpForce * ModConfig.Instance.values.jumpHeightMultiplier;

                ___sprintMultiplier = defaultSprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier;

                if(!modified)
                    __instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);

                modified = true;
            }
            else if(modified) // Reset single time upon enlarge
            {
                ___jumpForce = defaultJumpForce;

                ___sprintMultiplier = defaultSprintMultiplier;
                __instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
                modified = false;
            }
        }
    }

    // todo: itemActive maybe here?
}




