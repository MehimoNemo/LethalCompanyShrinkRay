using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class VehicleControllerPatch
    {
        private static float _realPlayerScale = -1f;

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetPlayerInControlOfVehicleClientRpc))]
        [HarmonyPostfix]
        public static void SetPlayerInControlOfVehicleClientRpcPostfix(VehicleController __instance)
        {
            if (ModConfig.Instance.values.resizeWhenInVehicle)
            {
                SetScaleOnEnterVehicle(__instance.currentDriver, __instance);
            }
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetPassengerInCar))]
        [HarmonyPostfix]
        public static void SetPassengerInCarPostfix(PlayerControllerB player, VehicleController __instance)
        {
            if (ModConfig.Instance.values.resizeWhenInVehicle)
            {
                SetScaleOnEnterVehicle(player, __instance);
            }
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(VehicleController __instance)
        {
            if (ModConfig.Instance.values.resizeWhenInVehicle && _realPlayerScale != -1f && !IsPlayerInASeat(__instance))
            {
                ResetScaleOnExitVehicle(PlayerInfo.CurrentPlayer);
            }
        }

        private static void SetScaleOnEnterVehicle(PlayerControllerB player, VehicleController vehicleController)
        {
            if (_realPlayerScale != -1 || player != PlayerInfo.CurrentPlayer) return;

            var vehicleScale = VehicleModification.ScalingOf(vehicleController).RelativeScale;
            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);

            if (vehicleScale == playerScaling.RelativeScale) return;

            _realPlayerScale = PlayerInfo.CurrentPlayerScale;
            playerScaling.TransformToScale.localScale = Vector3.one * vehicleScale;
        }

        private static void ResetScaleOnExitVehicle(PlayerControllerB player)
        {
            if (_realPlayerScale == -1 || player != PlayerInfo.CurrentPlayer) return;

            var playerScaling = PlayerModification.ScalingOf(player);
            if (playerScaling == null)
                Plugin.Log("Unable to reset player size after using vehicle.", Plugin.LogType.Error);
            else
            {
                playerScaling.TransformToScale.localScale = Vector3.one * _realPlayerScale;
                playerScaling.SetLocalScaleAfterYield(_realPlayerScale, new WaitForSeconds(0.5f));
                playerScaling.SetLocalScaleAfterYield(_realPlayerScale, new WaitForSeconds(1f));

            }
            _realPlayerScale = -1;
        }

        private static bool IsPlayerInASeat(VehicleController vehicleController)
        {
            return vehicleController.localPlayerInControl || vehicleController.localPlayerInPassengerSeat || PlayerInfo.CurrentPlayer.inVehicleAnimation;
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
