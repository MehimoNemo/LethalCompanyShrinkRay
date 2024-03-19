using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class PlayerModificationPatch
    {
        private static bool modified = false, wasModifiedLastFrame = false, wasResetLastFrame = false;
        private static float modifiedSprintMultiplier = 0f;

        public static void Modify(float playerSize)
        {
            modified = true;
            wasModifiedLastFrame = true;
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

            if(__instance.playerClientId != PlayerInfo.CurrentPlayerID)
                return; 

            if (PlayerDefaultValues.Values == null)
                return;

            var defaults = PlayerDefaultValues.Values;

            // Single-time changes
            if(wasModifiedLastFrame)
            {
                ___jumpForce = defaults.Value.jumpForce * ModConfig.Instance.values.jumpHeightMultiplier;
                wasModifiedLastFrame = false;
            }

            if(wasResetLastFrame)
            {
                ___jumpForce = defaults.Value.jumpForce;
                wasResetLastFrame = false;
            }

            // Continuos changes
            if (modified)
            {
                if (modifiedSprintMultiplier == 0f)
                    modifiedSprintMultiplier = defaults.Value.sprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier;

                // Base values taken from PlayerControllerB.Update()
                var baseModificationSpeed = __instance.isSprinting ? Time.deltaTime : (Time.deltaTime * 10f);
                var baseSpeed = (__instance.isSprinting ? 2.25f : 1f) * ModConfig.Instance.values.movementSpeedMultiplier;

                modifiedSprintMultiplier = Mathf.Lerp(modifiedSprintMultiplier, baseSpeed, baseModificationSpeed);
                ___sprintMultiplier = modifiedSprintMultiplier;
            }

            //__instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
        }
    }
}




