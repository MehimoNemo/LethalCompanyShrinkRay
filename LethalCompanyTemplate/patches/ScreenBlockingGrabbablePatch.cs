using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LCShrinkRay.patches
{
    internal class ScreenBlockingGrabbablePatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
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

            if (!item.gameObject.TryGetComponent(out ScaledGrabbableObjectData scaledItemData))
                return;

            item.itemProperties.positionOffset = scaledItemData.initialValues.offset;
            item.transform.localScale = scaledItemData.initialValues.scale;
            if (scaledItemData.initialValues.rendererDefaults != null)
                UnGlassifyItem(item, scaledItemData);

            Object.Destroy(scaledItemData);
        }

        public static void TransformItemRelativeTo(GrabbableObject item, float scale, Vector3 additionalOffset = new Vector3())
        {
            Plugin.Log("TransformItemRelativeTo -> " + (item != null ? item.name : "null") + "/" + scale + "/" + additionalOffset);
            if (item == null) return;

            if (PlayerInfo.Rounded(scale) == 1f)
            {
                OnItemNormalize(item);
                return;
            }

            if (!item.gameObject.TryGetComponent(out ScaledGrabbableObjectData scaledItemData))
                scaledItemData = item.gameObject.AddComponent<ScaledGrabbableObjectData>();

            item.itemProperties.positionOffset = scaledItemData.initialValues.offset * scale + additionalOffset;
            item.transform.localScale = scaledItemData.initialValues.scale * scale;
        }

        public static void UnGlassifyItem(GrabbableObject item, ScaledGrabbableObjectData scaledItemData = null)
        {
            if (item == null || scaledItemData == null && !item.gameObject.TryGetComponent(out scaledItemData))
                return;

            var meshRenderer = item.gameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                foreach (var r in meshRenderer)
                {
                    var rendererDefaults = scaledItemData.initialValues.rendererDefaults[r.GetInstanceID()];
                    r.sharedMaterials = rendererDefaults.materials;
                    r.rendererPriority = rendererDefaults.priority;
                }
            }

            scaledItemData.IsGlassified = false;
        }

        public static void GlassifyItem(GrabbableObject item, ScaledGrabbableObjectData scaledItemData = null)
        {
            if (item == null) return;

            if (scaledItemData == null && !item.gameObject.TryGetComponent(out scaledItemData))
                scaledItemData = item.gameObject.AddComponent<ScaledGrabbableObjectData>();

            if (scaledItemData.IsGlassified) return;

            var meshRenderer = item.gameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                foreach (var r in meshRenderer)
                {
                    if (r.sharedMaterials == null || r.sharedMaterials.Length == 0)
                        return;

                    r.rendererPriority = 0;

                    var materials = new Material[r.sharedMaterials.Length];
                    System.Array.Fill(materials, Materials.Glass);
                    r.sharedMaterials = materials;
                }
            }
        }
    }
}
