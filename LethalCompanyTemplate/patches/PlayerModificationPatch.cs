using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System.Diagnostics.CodeAnalysis;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerModificationPatch
    {
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

        static int count = 0;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance, ref float ___sprintMultiplier, ref float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if(__instance.playerClientId != PlayerHelper.currentPlayer().playerClientId)
                return;

            Shrinking.Instance.Update(); // maybe put that in shrinking?

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

            if (count % 100 == 1)
                Plugin.log("Player values: J -> " + ___jumpForce + " / S -> " + ___sprintMultiplier + " (Jumping: " + __instance.isSprinting.ToString() + ")");
            count++;

            //__instance.carryWeight = PlayerHelper.calculatePlayerWeightFor(__instance);
        }
    }
}




