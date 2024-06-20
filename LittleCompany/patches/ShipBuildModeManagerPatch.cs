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

        [HarmonyPatch(typeof(ShipBuildModeManager), "EnterBuildMode")]
        [HarmonyPrefix]
        public static void EnterBuildMode_Prefix(InputAction.CallbackContext context, ShipBuildModeManager __instance)
        {
            if (!context.performed || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isTypingChat)
            {
                return;
            }
            if (__instance.InBuildMode)
            {
                if (!(__instance.timeSincePlacingObject <= 1f) && __instance.PlayerMeetsConditionsToBuild())
                {
                    if (!__instance.CanConfirmPosition)
                    {
                        return;
                    }
                    ShipObjectScaling scaling = ShipObjectModification.ScalingOf(__instance.placingObject);
                    __instance.ghostObject.position -= Vector3.up * (scaling.offsetPivotToBottom * scaling.RelativeScale);
                }
                return;
            }
        }
    }
}
