using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class VehicleControllerPatch
    {
        private static float _realPlayerScale = 1f;

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetPlayerInControlOfVehicleClientRpc))]
        [HarmonyPostfix]
        public static void SetPlayerInControlOfVehicleClientRpcPostfix(VehicleController __instance)
        {
            if (__instance.currentDriver != PlayerInfo.CurrentPlayer) return;

            var vehicleScale = VehicleModification.ScalingOf(__instance).RelativeScale;
            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);
            _realPlayerScale = PlayerInfo.CurrentPlayerScale;
            playerScaling.TransformToScale.localScale = Vector3.one * vehicleScale;
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.ExitDriverSideSeat))]
        [HarmonyPostfix]
        public static void ExitDriverSideSeatPostfix(VehicleController __instance)
        {
            Plugin.Log("ExitDriverSideSeatPostfix");
            if (!__instance.localPlayerInControl) return;
            Plugin.Log("ExitDriverSideSeatPostfix2");

            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);
            if (playerScaling == null)
                Plugin.Log("Unable to reset player size after using vehicle.", Plugin.LogType.Error);
            else
                playerScaling.TransformToScale.localScale = playerScaling.OriginalScale * _realPlayerScale;
        }

        private static float GetMaximumCarStress(VehicleController vehicule)
        {
            VehicleScaling scaling = VehicleModification.ScalingOf(vehicule);
            if (scaling == null)
            {
                return 7f;
            }
            else
            {
                if (scaling.RelativeScale < 1f)
                {
                    return 10f/scaling.RelativeScale;
                }
            }
            return 7f;
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetInternalStress))]
        [HarmonyPrefix]
        public static bool SetInternalStress(VehicleController __instance, float carStressIncrease = 0f)
        {
            if (!(StartOfRound.Instance.testRoom == null) || !StartOfRound.Instance.inShipPhase)
            {
                if (carStressIncrease <= 0f)
                {
                    __instance.carStressChange = Mathf.Clamp(__instance.carStressChange - Time.deltaTime, -0.25f, 0.5f);
                }
                else
                {
                    __instance.carStressChange = Mathf.Clamp(__instance.carStressChange + Time.deltaTime * carStressIncrease, 0f, 10f);
                }

                __instance.underExtremeStress = carStressIncrease >= 1f;
                __instance.carStress = Mathf.Clamp(__instance.carStress + __instance.carStressChange, 0f, 100f);
                if (__instance.carStress > GetMaximumCarStress(__instance))
                {
                    __instance.carStress = 0f;
                    __instance.DealPermanentDamage(2);
                    __instance.lastDamageType = "Stress";
                }
            }
            return true;
        }
    }
}
