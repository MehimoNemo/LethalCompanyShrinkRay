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
        #region Properties
        private const float DurationOfGrabAnimationInSeconds = 0.3f;
        #endregion

        #region Patches
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if(!CanBlockOurScreen(__instance)) return;

            // Check for glassification after the pickup anim is finished
            __instance.StartCoroutine(CheckForGlassifyLater(__instance));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GrabbableObject), "DiscardItem")]
        public static void DiscardItem(GrabbableObject __instance)
        {
            if (!CanBlockOurScreen(__instance)) return;

            UnGlassifyItem(__instance);
        }
        #endregion

        #region Methods
        private static IEnumerator CheckForGlassifyLater(GrabbableObject item)
        {
            yield return new WaitForSeconds(DurationOfGrabAnimationInSeconds);
            CheckForGlassify(item);
        }

        private static bool CanBlockOurScreen(GrabbableObject item)
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
            if (item.playerHeldBy == null) return;

            // If item relative scale is equal or smaller than the player scale then don't glassify
            if (CompareItemScaleToPlayerScale(item, item.playerHeldBy) <= 0) return;

            bool tooBig = IsItemBlockingMostOfScreen(item);
            bool playerShrunk = PlayerInfo.IsShrunk(item.playerHeldBy);

            if ((playerShrunk && item.itemProperties.twoHanded) || (tooBig))
                GlassifyItem(item);
            else
                UnGlassifyItem(item);
        }

        private static float CompareItemScaleToPlayerScale(GrabbableObject item, PlayerControllerB pcb)
        {
            ItemScaling itemScaling = item.GetComponent<ItemScaling>();
            PlayerScaling playerScaling = pcb.GetComponent<PlayerScaling>();

            float itemRelativeScale = Modification.Rounded(itemScaling == null ? 1 : itemScaling.RelativeScale);
            float playerScale = Modification.Rounded(playerScaling == null ? 1 : playerScaling.RelativeScale);
            return (itemRelativeScale - playerScale);
        }

        private static bool IsItemBlockingMostOfScreen(GrabbableObject item)
        {
            Camera main = item.playerHeldBy.gameplayCamera;
            item.EnablePhysics(true);
            int numberOfPointsAxisY = 5;
            int numberOfPointsAxisX = 6;


            int numberOfRay = 0;
            int numberOfHits = 0;
            for (float stepY = numberOfPointsAxisY; stepY > 0; stepY--)
            {
                for (float stepX = 1; stepX <= numberOfPointsAxisX; stepX++)
                {
                    if(ScreenPosHitCollider(stepX * Screen.width / numberOfPointsAxisX+1, stepY * Screen.height / numberOfPointsAxisY+1, main.nearClipPlane, main, item.gameObject))
                    {
                        numberOfHits++;
                    }
                    numberOfRay++;
                }
            }
            item.EnablePhysics(false);

            // Plugin.Log("Percent: " + ((float)numberOfHits / numberOfRay)*100 + "%");
            return ((float) numberOfHits / numberOfRay) > 0.75f;
        }

        private static bool ScreenPosHitCollider(float x, float y, float focusDistance, Camera cam, GameObject target)
        {
            Vector3 origin = cam.ScreenToWorldPoint(new Vector3(x, y, focusDistance));
            RaycastHit[] hits = Physics.RaycastAll(origin + cam.transform.forward * 8, -cam.transform.forward, 10, ToInt([Mask.Props]));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == target)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UnGlassifyItem(GrabbableObject item)
        {
            if (item == null)
                return;

            if(item.TryGetComponent(out TargetGlassification glassification))
                Object.Destroy(glassification);
        }

        private static void GlassifyItem(GrabbableObject item)
        {
            if (item == null) return;

            if (!item.gameObject.TryGetComponent<TargetGlassification>(out _))
                item.gameObject.AddComponent<TargetGlassification>();
        }
        #endregion
    }
}
