using GameNetcodeStuff;
using HarmonyLib;
using LC_API.Networking;
using LCShrinkRay.comp;
using UnityEngine.InputSystem;
using UnityEngine;
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
            public float currentAnimationSpeed { get; set; }
        }
        public static DefaultPlayerValues defaultPlayerValues;
        public static bool defaultsInitialized = false;

        //static bool logShowed = false, log2Showed = false;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___currentAnimationSpeed, ref float ___jumpForce)
        {
            Shrinking.Instance.Update(); // maybe put that in shrinking?

            if (!defaultsInitialized)
            {
                defaultPlayerValues = new DefaultPlayerValues();
                defaultPlayerValues.jumpForce = ___jumpForce;
                defaultPlayerValues.sprintMultiplier = ___sprintMultiplier;
                defaultPlayerValues.currentAnimationSpeed = ___currentAnimationSpeed;
                defaultsInitialized = true;
            }

            // Speed & Jump Multiplier for shrunken players
            if (PlayerHelper.isCurrentPlayerShrunk())
            {
                ___jumpForce = defaultPlayerValues.jumpForce * ModConfig.Instance.values.jumpHeightMultiplier;

                ___sprintMultiplier = defaultPlayerValues.sprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier;
                ___currentAnimationSpeed = defaultPlayerValues.currentAnimationSpeed * (ModConfig.Instance.values.movementSpeedMultiplier * 2f);
                if (__instance.isSprinting)
                {
                    ___sprintMultiplier *= 2.25f;
                    ___currentAnimationSpeed *= 2.25f;
                }

                __instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
            }
        }
    }

    // todo: itemActive maybe here?
}




