using DunGen;
using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static System.Net.Mime.MediaTypeNames;

namespace LCShrinkRay.patches
{
    internal class ScreenBlockingGrabbablePatch
    {
        public struct GrabbableObjectDefaults
        {
            public Vector3 scale { get; set; }
            public Vector3 offset { get; set; }
        }

        public static Dictionary<ulong, GrabbableObjectDefaults> itemDefaults = new Dictionary<ulong, GrabbableObjectDefaults>();

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (__instance.playerHeldBy == null || __instance is GrabbablePlayerObject)
            {
                Plugin.log("adjustItemOffset: object is not held or other player", Plugin.LogType.Warning);
                return;
            }

            itemDefaults.Add(__instance.NetworkObjectId, new GrabbableObjectDefaults() { scale = __instance.transform.localScale, offset = __instance.itemProperties.positionOffset });

            transformItemRelativeTo(__instance, __instance.playerHeldBy);

            /*Vector3 viewPos = __instance.playerHeldBy.gameplayCamera.WorldToViewportPoint(__instance.gameObject.transform.position);
            if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0)
            {
                Plugin.log("Held item is visible on screen");
            }*/
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GrabbableObject), "DiscardItem")]
        public static void DiscardItem(GrabbableObject __instance)
        {
            // __instance.transform.localScale /= __instance.playerHeldBy.transform.localScale.x; // already resetting automatically

            if(itemDefaults.ContainsKey(__instance.NetworkObjectId))
            {
                __instance.itemProperties.positionOffset = itemDefaults[__instance.NetworkObjectId].offset;
                __instance.transform.localScale = itemDefaults[__instance.NetworkObjectId].scale;
                itemDefaults.Remove(__instance.NetworkObjectId);
            }
        }

        public static void transformItemRelativeTo(GrabbableObject item, PlayerControllerB pcb)
        {
            if (pcb.transform.localScale.x == 1f)
                return;

            var itemDefault = itemDefaults.GetValueOrDefault(item.NetworkObjectId, new GrabbableObjectDefaults() { scale = item.transform.localScale, offset = item.itemProperties.positionOffset });

            item.transform.localScale = itemDefault.scale * pcb.transform.localScale.x;
            var yOffset = item.transform.localScale.y - (item.transform.localScale.y * pcb.transform.localScale.x);
            item.itemProperties.positionOffset = new Vector3(0f, item.itemProperties.twoHanded ? -yOffset : yOffset, 0f);
        }
    }
}
