using HarmonyLib;
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
    }
}
