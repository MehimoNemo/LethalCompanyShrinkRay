using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerModificationPatch
    {
        public static Transform helmetHudTransform;
        
        public struct PlayerControllerValues
        {
            public float jumpForce { get; set; }
            public float sprintMultiplier { get; set; }
        }
        private static PlayerControllerValues? defaultPlayerValues = null;
        private static bool modified = false, wasModifiedLastFrame = false, wasResetLastFrame = false;
        private static float modifiedSprintMultiplier = 0f;

        public static void modify(float playerSize)
        {
            modified = true;
            wasModifiedLastFrame = true;
        }

        public static void reset()
        {
            Plugin.log("Resetting player modifications");
            modified = false;
            wasResetLastFrame = true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if(__instance.playerClientId != PlayerHelper.currentPlayer().playerClientId)
                return; 

            if (helmetHudTransform == null)
            {
                var scavengerHelmet = GameObject.Find("ScavengerHelmet");
                if(scavengerHelmet != null)
                {
                    helmetHudTransform = scavengerHelmet.GetComponent<Transform>();
                    helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                    Plugin.log("Player transform got!");
                }
            }

            if (defaultPlayerValues == null)
            {
                defaultPlayerValues = new PlayerControllerValues
                {
                    jumpForce = ___jumpForce,
                    sprintMultiplier = ___sprintMultiplier
                };
                Plugin.log("Setting default values: J -> " + ___jumpForce + " / S -> " + ___sprintMultiplier);
            }

            // Single-time changes
            if(wasModifiedLastFrame)
            {
                ___jumpForce *= ModConfig.Instance.values.jumpHeightMultiplier;
                wasModifiedLastFrame = false;
            }

            if(wasResetLastFrame)
            {
                ___jumpForce /= ModConfig.Instance.values.jumpHeightMultiplier;
                wasResetLastFrame = false;
            }

            // Continuos changes
            if (modified)
            {
                if (modifiedSprintMultiplier == 0f)
                    modifiedSprintMultiplier = defaultPlayerValues.Value.sprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier;

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




