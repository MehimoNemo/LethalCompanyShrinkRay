using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class VehicleControllerPatch
    {
        private static float _realPlayerScale = 1f;
        private static bool _isInVehicle = false;

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetPlayerInControlOfVehicleClientRpc))]
        [HarmonyPostfix]
        public static void SetPlayerInControlOfVehicleClientRpcPostfix(VehicleController __instance)
        {
            SetScaleOnEnterVehicle(__instance.currentDriver, __instance);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetPassengerInCar))]
        [HarmonyPostfix]
        public static void SetPassengerInCarPostfix(PlayerControllerB player, VehicleController __instance)
        {
            SetScaleOnEnterVehicle(player, __instance);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.ExitDriverSideSeat))]
        [HarmonyPrefix]
        public static void ExitDriverSideSeatPrefix(VehicleController __instance)
        {
            if (__instance.localPlayerInControl)
            {
                ResetScaleOnExitVehicle(GameNetworkManager.Instance.localPlayerController);
            }
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SpringDriverSeatClientRpc))]
        [HarmonyPrefix]
        public static void SpringDriverSeatClientRpcPrefix(VehicleController __instance)
        {
            ResetScaleOnExitVehicle(__instance.currentDriver);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.ExitPassengerSideSeat))]
        [HarmonyPrefix]
        public static void ExitPassengerSideSeatPrefix(VehicleController __instance)
        {
            if (__instance.localPlayerInPassengerSeat)
            {
                ResetScaleOnExitVehicle(GameNetworkManager.Instance.localPlayerController);
            }
        }

        private static void SetScaleOnEnterVehicle(PlayerControllerB player, VehicleController vehicleController)
        {
            Plugin.Log("SetScaleOnEnterVehicle");
            if (_isInVehicle || player != PlayerInfo.CurrentPlayer) return;
            Plugin.Log("SetScaleOnEnterVehicle2");
            var vehicleScale = VehicleModification.ScalingOf(vehicleController).RelativeScale;
            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);
            _realPlayerScale = PlayerInfo.CurrentPlayerScale;
            playerScaling.TransformToScale.localScale = Vector3.one * vehicleScale;
            _isInVehicle = true;
        }

        private static void ResetScaleOnExitVehicle(PlayerControllerB player)
        {
            Plugin.Log("ResetScaleOnExitVehicle");
            if (!_isInVehicle || player != PlayerInfo.CurrentPlayer) return;
            Plugin.Log("ResetScaleOnExitVehicle2");

            var playerScaling = PlayerModification.ScalingOf(player);
            if (playerScaling == null)
                Plugin.Log("Unable to reset player size after using vehicle.", Plugin.LogType.Error);
            else
            {
                //playerScaling.NextFrameScale(_realPlayerScale, player, 0);
                //playerScaling.NextFrameScale(_realPlayerScale, player, 5);
                playerScaling.ScaleOverTimeTo(_realPlayerScale, player);
            }
            _isInVehicle = false;
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
