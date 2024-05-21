﻿using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class PlayerMultiplierPatch
    {
        private const float DefaultJumpForce = 13f;
        private const float DefaultSprintMultiplier = 1f;

        private static bool modified = false, wasModifiedLastFrame = false, wasResetLastFrame = false;
        private static float modifiedSprintMultiplier = 0f;

        public static void Modify()
        {
            modified = ModConfig.Instance.values.movementSpeedMultiplier != 1f;
            wasModifiedLastFrame = ModConfig.Instance.values.jumpHeightMultiplier != 1f;
        }

        public static void Reset()
        {
            Plugin.Log("Resetting player modifications");
            modified = false;
            wasResetLastFrame = true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.IsGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if(__instance.playerClientId != PlayerInfo.CurrentPlayerID || !__instance.isPlayerControlled || __instance.isPlayerDead)
                return;

            // Single-time changes
            if(wasModifiedLastFrame)
            {
                ___jumpForce = DefaultJumpForce * ModConfig.Instance.values.jumpHeightMultiplier;
                modifiedSprintMultiplier = ___sprintMultiplier;
                wasModifiedLastFrame = false;
            }

            if(wasResetLastFrame)
            {
                ___jumpForce = DefaultJumpForce;
                wasResetLastFrame = false;
            }

            // Continuous changes
            if (modified)
            {
                // Base values taken from PlayerControllerB.Update()
                var delta = Time.deltaTime;
                var baseModificationSpeed = __instance.isSprinting ? delta : (delta * 10f);
                var baseSpeed = (__instance.isSprinting ? 2.25f : 1f) * ModConfig.Instance.values.movementSpeedMultiplier;
                modifiedSprintMultiplier = Mathf.Lerp(modifiedSprintMultiplier, baseSpeed, baseModificationSpeed);
                ___sprintMultiplier = modifiedSprintMultiplier;
            }
        }
    }
}




