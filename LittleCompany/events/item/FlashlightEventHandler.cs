using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.modifications;
using System.Collections.Generic;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    [HarmonyPatch]
    internal class FlashlightEventHandler : ItemEventHandler
    {
        static Dictionary<ulong, float> defaultFlashlightSpotAngles = new Dictionary<ulong, float>();
        static Dictionary<ulong, float> defaultFlashlightInnerSpotAngles = new Dictionary<ulong, float>();

        float baseBatteryUsage = 0f;

        public override void OnAwake()
        {
            base.OnAwake();
            baseBatteryUsage = item.itemProperties.batteryUsage;
        }

        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaled(from, to, playerShrunkenBy);

            var batteryUsageMultiplier = 1f + Mathf.Max((1f - to) / 5, -0.9f);
            item.itemProperties.batteryUsage = baseBatteryUsage * batteryUsageMultiplier;
            Plugin.Log("Multiplier: " +  batteryUsageMultiplier);
        }

        [HarmonyPatch(typeof(FlashlightItem), "Update")]
        [HarmonyPostfix()]
        public static void Update(FlashlightItem __instance, float ___initialIntensity)
        {
            var scaling = ObjectModification.ScalingOf(__instance);
            if (scaling.Unchanged) return;

            if (!defaultFlashlightSpotAngles.ContainsKey(__instance.NetworkObjectId))
                defaultFlashlightSpotAngles.Add(__instance.NetworkObjectId, __instance.flashlightBulb.spotAngle);

            if (!defaultFlashlightInnerSpotAngles.ContainsKey(__instance.NetworkObjectId))
                defaultFlashlightInnerSpotAngles.Add(__instance.NetworkObjectId, __instance.flashlightBulb.innerSpotAngle);

            var intensityMultiplier = 1f + Mathf.Max((scaling.RelativeScale - 1f) / 2, -0.9f);
            __instance.flashlightBulb.intensity = ___initialIntensity * intensityMultiplier;
            __instance.flashlightBulb.spotAngle = Mathf.Max(defaultFlashlightSpotAngles[__instance.NetworkObjectId] * intensityMultiplier, 75f);
            __instance.flashlightBulb.innerSpotAngle = Mathf.Max(defaultFlashlightInnerSpotAngles[__instance.NetworkObjectId] * intensityMultiplier, 75f);
        }
    }
}
