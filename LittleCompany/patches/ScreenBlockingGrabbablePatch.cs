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
            if(!CanBlockOurScreen(__instance)) return;

            TransformItemRelativeTo(__instance, PlayerInfo.SizeOf(__instance.playerHeldBy));
            CheckForGlassify(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GrabbableObject), "DiscardItem")]
        public static void DiscardItem(GrabbableObject __instance)
        {
            if (!CanBlockOurScreen(__instance)) return;

            OnItemNormalize(__instance);
        }

        public static bool CanBlockOurScreen(GrabbableObject item)
        {
            var currentPlayerID = PlayerInfo.CurrentPlayerID;
            if (currentPlayerID.HasValue && item.playerHeldBy != null && item.playerHeldBy.playerClientId != currentPlayerID)
                return false;

            if (item is GrabbablePlayerObject)
                return false;

            return true;
        }

        public static void CheckForGlassify(GrabbableObject item)
        {
            if (item == null) return;

            bool isScaled = item.TryGetComponent(out TargetScaling scaling) && !scaling.Unchanged;

            if (PlayerInfo.IsNormalSize(item.playerHeldBy) && !isScaled) return;

            if (item.itemProperties.twoHanded || isScaled && scaling.CurrentSize > 1f)
                GlassifyItem(item);
            else
                UnGlassifyItem(item);
        }

        public static void OnItemNormalize(GrabbableObject item)
        {
            if (item == null) return;

            if (item.gameObject.TryGetComponent(out TargetScaling scaling))
                scaling.Reset();

            UnGlassifyItem(item);
        }

        public static void TransformItemRelativeTo(GrabbableObject item, float scale, Vector3 additionalOffset = new Vector3())
        {
            if (item == null) return;

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
