using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static UnityEngine.GraphicsBuffer;

namespace LCShrinkRay.patches
{
    internal class ScreenBlockingGrabbablePatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (__instance.playerHeldBy == null || __instance is GrabbablePlayerObject)
            {
                Plugin.log("adjustItemOffset: object is not held or other player", Plugin.LogType.Warning);
                return;
            }

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
            if (item == null || item.playerHeldBy == null || PlayerHelper.isNormalSize(item.playerHeldBy.transform.localScale.x)) return;

            //Plugin.log("CheckForGlassify - Item layer: " + item.gameObject.layer.ToString());
            //var camera = item.playerHeldBy.gameplayCamera;
            //var ray = new Ray(camera.transform.position, camera.transform.position + camera.transform.forward);
            // all vanilla items are on mask 6. may cause issues with other mods
            //if (Physics.Raycast(ray, out RaycastHit raycastHit, 2f, 6, QueryTriggerInteraction.Collide))
            if(item.itemProperties.twoHanded)
            {
                //Plugin.log("Ray has hit an item! Object: " + raycastHit.collider.name);

                //Plugin.log("Held item is visible in the center of the screen");
                GlassifyItem(item);
            }
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
            if (item == null) return;

            if (PlayerHelper.isNormalSize(scale))
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
            if (item == null) return;

            if (scaledItemData == null && !item.gameObject.TryGetComponent(out scaledItemData))
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
                    System.Array.Fill(materials, Glass);
                    r.sharedMaterials = materials;
                }
            }
        }

        private static string GlassMaterialName { get { return "LCGlass"; } }

        public static Material Glass
        {
            get
            {
                var m = new Material(Shader.Find("HDRP/Lit"));
                m.color = new Color(0.5f, 0.5f, 0.6f, 0.6f);
                m.renderQueue = 3300;
                m.shaderKeywords = [
                    "_SURFACE_TYPE_TRANSPARENT",
                    "_DISABLE_SSR_TRANSPARENT",
                    "_REFRACTION_THIN",
                    "_NORMALMAP_TANGENT_SPACE",
                    "_ENABLE_FOG_ON_TRANSPARENT"
                ];
                m.name = GlassMaterialName;
                return m;
            }
        }
    }
}
