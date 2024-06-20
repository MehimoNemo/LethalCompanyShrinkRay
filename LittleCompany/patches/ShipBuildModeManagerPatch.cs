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
                float relativeScaleOfParent = scaling.RelativeScale;
                Plugin.Log("Scale: " + relativeScaleOfParent);
                Vector3 scale = Vector3.Scale(__instance.placingObject.mainMesh.transform.localScale, __instance.placingObject.parentObject.transform.localScale) / relativeScaleOfParent;
                __instance.ghostObjectMesh.transform.localScale = scale;
                __instance.selectionOutlineMesh.transform.localScale = scale;
                __instance.ghostObjectMesh.transform.position -= Vector3.up * (scaling.offsetPivotToBottom * relativeScaleOfParent);
            }
        }

        /*[HarmonyPatch(typeof(ShipBuildModeManager), "ConfirmBuildMode_performed")]
        [HarmonyPrefix]
        public static void ConfirmBuildMode_performed_Prefix()
        {
            Plugin.Log("ConfirmBuildMode_performed");
            if (!(ShipBuildModeManager.Instance.timeSincePlacingObject <= 1f) && ShipBuildModeManager.Instance.PlayerMeetsConditionsToBuild() && ShipBuildModeManager.Instance.InBuildMode)
            {
                if (!ShipBuildModeManager.Instance.CanConfirmPosition)
                {
                    return;
                }
                Plugin.Log("OFFSETTING");
                ShipObjectScaling scaling = ShipObjectModification.ScalingOf(ShipBuildModeManager.Instance.placingObject);
                ShipBuildModeManager.Instance.ghostObject.position -= Vector3.up * (scaling.offsetPivotToBottom * scaling.RelativeScale);
            }
        }*/
    }
}
