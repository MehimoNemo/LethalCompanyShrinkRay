using HarmonyLib;
using LittleCompany.components;
using LittleCompany.modifications;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class ShipBuildModeManagerPatch
    {
        [HarmonyPatch(typeof(ShipBuildModeManager), "CreateGhostObjectAndHighlight")]
        [HarmonyPostfix]
        public static void CreateGhostObjectAndHighlight_PostFix(ShipBuildModeManager __instance)
        {
            if (__instance.placingObject != null)
            {
                ShipObjectScaling scaling = ShipObjectModification.ScalingOf(__instance.placingObject);
                Vector3 scale = Vector3.Scale(__instance.placingObject.mainMesh.transform.localScale, __instance.placingObject.parentObject.transform.localScale) / scaling.RelativeScale;
                __instance.ghostObjectMesh.transform.localScale = scale;
                __instance.selectionOutlineMesh.transform.localScale = scale * 1.04f;
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "Update")]
        [HarmonyPostfix]
        public static void Update_PostFix(ShipBuildModeManager __instance)
        {
            if (__instance.InBuildMode)
            {
                ShipObjectScaling scaling = ShipObjectModification.ScalingOf(__instance.placingObject);
                __instance.ghostObject.position -= Vector3.up * (__instance.placingObject.yOffset - (__instance.placingObject.yOffset * scaling.RelativeScale));
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "PlayerMeetsConditionsToBuild")]
        [HarmonyPostfix]
        private static bool PlayerMeetsConditionsToBuild(bool __result)
        {
            if (!__result) return __result;

            if (ShipBuildModeManager.Instance.placingObject != null && ShipObjectModification.ScalingOf(ShipBuildModeManager.Instance.placingObject).GettingScaled) return false;

            return true;
        }
    }
}
