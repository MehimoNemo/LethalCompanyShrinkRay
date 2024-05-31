using HarmonyLib;
using LittleCompany.modifications;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class FlashlightPatch
    {
        [HarmonyPatch(typeof(FlashlightItem), "Update")]
        [HarmonyPostfix()]
        public static void Update(FlashlightItem __instance, float ___initialIntensity)
        {
            var scaling = ObjectModification.ScalingOf(__instance);
            if (scaling.Unchanged) return;

            var intensityMultiplier = 1f + Mathf.Max((scaling.RelativeScale - 1f) * 2, -0.9f);
            __instance.flashlightBulb.intensity = ___initialIntensity * intensityMultiplier;
        }
    }
}
