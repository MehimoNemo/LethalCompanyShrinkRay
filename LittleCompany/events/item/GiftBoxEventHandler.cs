using HarmonyLib;
using LittleCompany.components;
using LittleCompany.modifications;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    [HarmonyPatch]
    internal class GiftBoxEventHandler : ItemEventHandler
    {
        #region Patches
        [HarmonyPrefix, HarmonyPatch(typeof(GiftBoxItem), "OpenGiftBoxClientRpc")]
        public static void OpenGiftBox(GiftBoxItem __instance, NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
        {
            if (__instance.TryGetComponent(out TargetHighlighting highlighting))
                DestroyImmediate(highlighting);

            var giftBoxScaling = ObjectModification.ScalingOf(__instance);
            giftBoxScaling.RemoveHologram();

            var giftBoxScale = giftBoxScaling.RelativeScale;
            if (Mathf.Approximately(giftBoxScale, 1f)) return;

            __instance.StartCoroutine(waitForGiftPresentToSpawnAndScaleTo(giftBoxScale, netObjectRef));
        }

        private static IEnumerator waitForGiftPresentToSpawnAndScaleTo(float scale, NetworkObjectReference netObjectRef)
        {
            NetworkObject netObject = null;
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < 8f && !netObjectRef.TryGet(out netObject))
            {
                yield return new WaitForSeconds(0.03f);
            }
            if (netObject == null)
            {
                Plugin.Log("GiftBoxItemPatch: No network object found.", Plugin.LogType.Error);
                yield break;
            }
            yield return new WaitForEndOfFrame();

            if (netObject.TryGetComponent(out GrabbableObject item))
                ObjectModification.ScalingOf(item).ScaleToImmediate(scale, null);
        }
        #endregion
    }
}
