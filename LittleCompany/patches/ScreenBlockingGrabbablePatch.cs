using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections;
using UnityEngine;
using static LittleCompany.helper.LayerMasks;

namespace LittleCompany.patches
{
    internal class ScreenBlockingGrabbablePatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if(!CanBlockOurScreen(__instance)) return;

            // Check for glassification after the pickup anim is funished
            __instance.StartCoroutine(CheckForGlassifyLater(__instance));
        }

        public const float DurationOfGrabAnimationInSeconds = 0.3f;

        public static IEnumerator CheckForGlassifyLater(GrabbableObject item)
        {
            yield return new WaitForSeconds(DurationOfGrabAnimationInSeconds);
            CheckForGlassify(item);
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

            // If item relative scale is equal or smaller than the player scale then don't glassify
            if (CompareItemScaleToPlayerScale(item, item.playerHeldBy) <= 0) return;

            bool tooBig = IsItemBlockingCenterScreen(item);
            bool playerShrunk = PlayerInfo.IsShrunk(item.playerHeldBy);

            if ((playerShrunk && item.itemProperties.twoHanded) || (tooBig))
                GlassifyItem(item);
            else
                UnGlassifyItem(item);
        }

        public static float CompareItemScaleToPlayerScale(GrabbableObject item, PlayerControllerB pcb)
        {
            ItemScaling itemScaling = item.GetComponent<ItemScaling>();
            PlayerScaling playerScaling = pcb.GetComponent<PlayerScaling>();

            float itemRelativeScale = Modification.Rounded(itemScaling == null ? 1 : itemScaling.RelativeScale);
            float playerScale = Modification.Rounded(playerScaling == null ? 1 : playerScaling.RelativeScale);
            return (itemRelativeScale - playerScale);
        }

        private static bool IsItemBlockingCenterScreen(GrabbableObject item)
        {
            Camera main = item.playerHeldBy.gameplayCamera;

            RaycastHit[] hits;
            item.EnablePhysics(true);
            hits = Physics.RaycastAll(main.transform.position + main.transform.forward*10, -main.transform.forward, 10, ToInt([Mask.Props]));
            item.EnablePhysics(false);
            foreach (RaycastHit hit in hits)
            {
                if(hit.collider.gameObject == item.gameObject)
                {
                    return true;
                }
            }
            return false;
        }

        public static void OnItemNormalize(GrabbableObject item)
        {
            if (item == null) return;

            if (item.gameObject.TryGetComponent(out ItemScaling scaling))
                scaling.Reset();

            UnGlassifyItem(item);
        }

        public static void TransformItemRelativeTo(GrabbableObject item, float scale, Vector3 additionalOffset = new Vector3())
        {
            if (item == null) return;

            if (!item.gameObject.TryGetComponent(out ItemScaling scaling))
                scaling = item.gameObject.AddComponent<ItemScaling>();

            scaling.ScaleTemporarlyTo(scale);
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
            Plugin.Log("GlassifyItem");
            if (item == null) return;

            if (!item.gameObject.TryGetComponent<TargetGlassification>(out _))
                item.gameObject.AddComponent<TargetGlassification>();
        }
    }
}
