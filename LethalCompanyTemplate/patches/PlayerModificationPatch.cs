using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerModificationPatch
    {
        public static MeshRenderer helmetRenderer;
        
        public struct PlayerControllerValues
        {
            public float jumpForce { get; set; }
            public float sprintMultiplier { get; set; }
        }
        private static PlayerControllerValues? defaultPlayerValues = null;
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

            if (helmetRenderer == null)
            {
                var scavengerHelmet = GameObject.Find("ScavengerHelmet");
                if(scavengerHelmet != null)
                {
                    var helmetTransform = scavengerHelmet.GetComponent<Transform>();
                    helmetTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                    Plugin.Log("Player transform got!");

                    Plugin.Log("Finding helmet!");
                    try
                    {
                        helmetRenderer = helmetTransform.gameObject.GetComponent<MeshRenderer>();
                    }
                    catch (Exception e)
                    {
                        Plugin.Log(e.Message, Plugin.LogType.Warning);
                    }
                }
            }

            if (defaultPlayerValues == null)
            {
                defaultPlayerValues = new PlayerControllerValues
                {
                    jumpForce = ___jumpForce,
                    sprintMultiplier = ___sprintMultiplier
                };
                Plugin.Log("Setting default values: J -> " + ___jumpForce + " / S -> " + ___sprintMultiplier);
            }

            // Single-time changes
            if(wasModifiedLastFrame)
            {
                ___jumpForce = defaultPlayerValues.Value.jumpForce * ModConfig.Instance.values.jumpHeightMultiplier;
                wasModifiedLastFrame = false;
            }

            if(wasResetLastFrame)
            {
                ___jumpForce = defaultPlayerValues.Value.jumpForce;
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




