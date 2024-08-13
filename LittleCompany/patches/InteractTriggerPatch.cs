using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class InteractTriggerPatch
    {
        private static float _realPlayerScale = -1f;

        [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
        [HarmonyPrefix]
        public static bool InteractPrefix(Transform playerTransform, InteractTrigger __instance)
        {
            if (!ModConfig.Instance.values.resizeWhenInVehicle && (__instance.name == "DriverSeatTrigger" || __instance.name == "PassengerSeatTrigger"))
            {
                PlayerControllerB playerControllerB = playerTransform.GetComponent<PlayerControllerB>();
                if (playerControllerB == PlayerInfo.CurrentPlayer)
                {
                    float playerScale = 1f;
                    float vehicleScale = 1f;

                    PlayerScaling playerScaling = playerControllerB.GetComponent<PlayerScaling>();
                    if (playerScaling != null) playerScale = playerScaling.RelativeScale;
                    VehicleScaling vehicleScaling = __instance.GetComponentInParent<VehicleScaling>();
                    if (vehicleScaling != null) vehicleScale = vehicleScaling.RelativeScale;
                    if (playerScale > vehicleScale)
                    {
                        HUDManager.Instance.DisplayTip("Company Cruiser", "You are too big to fit the cruiser.", false, false, "littleCompany-Hint");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
