using GameNetcodeStuff;
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

            //TransformItemRelativeTo(__instance, PlayerInfo.SizeOf(__instance.playerHeldBy));
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

            bool tooBig = ItemBiggerThanHalfScreen(item);
            Plugin.Log("TooBig" + tooBig);

            if ((PlayerInfo.IsShrunk(item.playerHeldBy) && item.itemProperties.twoHanded) || tooBig)
                GlassifyItem(item);
            else
                UnGlassifyItem(item);
        }

        private const float widthItemForScreenBlockWhileNormalSized = 1.3f;

        private static bool ItemBiggerThanHalfScreen(GrabbableObject item)
        {
            float sizeX = getBiggestWidthFromAllMeshRenderer(item);
            Plugin.Log("SizeX: " + sizeX);
            return PlayerInfo.SizeOf(item.playerHeldBy) * widthItemForScreenBlockWhileNormalSized <= sizeX;
        }

        private static float getBiggestWidthFromAllMeshRenderer(GrabbableObject item)
        {
            float biggestX = 0f;
            foreach (MeshRenderer mesh in item.gameObject.GetComponentsInChildren<MeshRenderer>())
            {
                float meshSizeX = mesh.localBounds.size.x * getRelativeLocalScaleOfMesh(mesh, item);
                if (meshSizeX > biggestX)
                {
                    biggestX = meshSizeX;
                }
            }
            return biggestX;
        }

        private static float getRelativeLocalScaleOfMesh(MeshRenderer mesh, GrabbableObject item)
        {
            Transform parentTransform = mesh.transform;
            float relativeLocalScale = parentTransform.localScale.x;
            int counter = 0;
            // Counter to make sure we don't have an infinite loop. If the mesh is 10 deep it's too much anyway
            while(parentTransform != item.transform && counter < 10)
            {
                parentTransform = parentTransform.parent;
                relativeLocalScale *= parentTransform.localScale.x;
                counter++;
            }
            Plugin.Log("RelativeLocalScale: " + relativeLocalScale);
            return relativeLocalScale;
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
