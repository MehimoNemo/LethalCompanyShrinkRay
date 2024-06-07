using GameNetcodeStuff;
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
        //private const float DefaultSprintMultiplier = 1f;

        private static bool modified = false, wasModifiedLastFrame = false, wasResetLastFrame = false;
        private static float modifiedSprintMultiplier = 0f;

        public static void Modify()
        {
            modified = ModConfig.Instance.values.logicalMultiplier || !Mathf.Approximately(ModConfig.Instance.values.movementSpeedMultiplier, 1f);
            wasModifiedLastFrame = ModConfig.Instance.values.logicalMultiplier || !Mathf.Approximately(ModConfig.Instance.values.jumpHeightMultiplier, 1f);
        }

        public static void Reset()
        {
            Plugin.Log("Resetting player modifications");
            modified = false;
            wasResetLastFrame = ModConfig.Instance.values.logicalMultiplier ||!Mathf.Approximately(ModConfig.Instance.values.jumpHeightMultiplier, 1f);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.IsGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if(__instance.playerClientId != PlayerInfo.CurrentPlayerID || !__instance.isPlayerControlled || __instance.isPlayerDead)
                return;

            var playerSize = PlayerInfo.SizeOf(__instance);

            // Single-time changes
            if(wasModifiedLastFrame)
            {
                if (ModConfig.Instance.values.logicalMultiplier)
                {
                    ___jumpForce = DefaultJumpForce * (1f + ((playerSize - 1f) * 0.25f)); // adjust last value when needed
                }
                else
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
                float speedMultiplier;
                if (ModConfig.Instance.values.logicalMultiplier)
                    speedMultiplier = 1f + ((playerSize - 1f) * 0.5f);
                else
                    speedMultiplier = ModConfig.Instance.values.movementSpeedMultiplier;
                
                var baseSpeed = (__instance.isSprinting ? 2.25f : 1f) * speedMultiplier;
                modifiedSprintMultiplier = Mathf.Lerp(modifiedSprintMultiplier, baseSpeed, baseModificationSpeed);
                ___sprintMultiplier = modifiedSprintMultiplier;
            }
        }
    }
}




