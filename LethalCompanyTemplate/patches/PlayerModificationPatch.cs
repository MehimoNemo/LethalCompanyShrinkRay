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
        private static PlayerControllerValues? defaultPlayerValues = null, modifiedPlayerValues = null;

        public static void modify(float playerSize)
        {
            if (defaultPlayerValues == null) return;

            modifiedPlayerValues = new PlayerControllerValues
            {
                jumpForce = defaultPlayerValues.Value.jumpForce * ModConfig.Instance.values.jumpHeightMultiplier,
                sprintMultiplier = defaultPlayerValues.Value.sprintMultiplier * ModConfig.Instance.values.movementSpeedMultiplier
            };
            Plugin.log("Setting modified values: J -> " + modifiedPlayerValues.Value.jumpForce + " / S -> " + modifiedPlayerValues.Value.sprintMultiplier);
        }

        public static void reset()
        {
            Plugin.log("Resetting player modifications");
            modifiedPlayerValues = null;
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

            ___jumpForce = modifiedPlayerValues != null ? modifiedPlayerValues.Value.jumpForce : defaultPlayerValues.Value.jumpForce;
            ___sprintMultiplier = modifiedPlayerValues != null ? modifiedPlayerValues.Value.sprintMultiplier : defaultPlayerValues.Value.sprintMultiplier;
            if (__instance.isSprinting)
                ___sprintMultiplier *= 2.25f;

            //__instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
        }
    }
}




