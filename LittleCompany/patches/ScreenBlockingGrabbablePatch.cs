using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class ScreenBlockingGrabbablePatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (PlayerInfo.CurrentPlayer == null) 
                return;

            if (__instance.playerHeldBy == null || __instance is GrabbablePlayerObject)
            {
                Plugin.Log("adjustItemOffset: object is not held or other player");
                return;
            }

            if (__instance.playerHeldBy.playerClientId != PlayerInfo.CurrentPlayerID)
                return;

            TransformItemRelativeTo(__instance, __instance.playerHeldBy.transform.localScale.x);
            CheckForGlassify(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GrabbableObject), "DiscardItem")]
        public static void DiscardItem(GrabbableObject __instance)
        {
            OnItemNormalize(__instance);
        }

        public static void CheckForGlassify(GrabbableObject item)
        {
            if (item == null || item.playerHeldBy == null || PlayerInfo.IsNormalSize(item.playerHeldBy)) return;

            if (item.playerHeldBy.playerClientId != PlayerInfo.CurrentPlayerID)
                return;

            if (item.itemProperties.twoHanded)
                GlassifyItem(item);
            else
                UnGlassifyItem(item);
        }

        public static void OnItemNormalize(GrabbableObject item)
        {
            if (item == null || item.playerHeldBy == null) return;

            if (item.gameObject.TryGetComponent(out TargetScaling scaling))
                scaling.Reset();

            UnGlassifyItem(item);
        }

        public static void TransformItemRelativeTo(GrabbableObject item, float scale, Vector3 additionalOffset = new Vector3())
        {
            if (item == null) return;

            if (PlayerInfo.Rounded(scale) == 1f)
            {
                OnItemNormalize(item);
                return;
            }

            if (!item.gameObject.TryGetComponent(out TargetScaling scaling))
                scaling = item.gameObject.AddComponent<TargetScaling>();

            scaling.ScaleRelativeTo(scale, additionalOffset);
        }

        public static void UnGlassifyItem(GrabbableObject item)
        {
            if (item == null)
                return;

            if(item.TryGetComponent(out TargetGlassification glassification))
                Object.Destroy(glassification);
        }

        public static void GlassifyItem(GrabbableObject item)
        {
            if (item == null) return;

            if (!item.gameObject.TryGetComponent<TargetGlassification>(out _))
                item.gameObject.AddComponent<TargetGlassification>();
        }
    }
}
