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
        public static Dictionary<ulong, Vector3> initialItemOffsets = new Dictionary<ulong, Vector3>();

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (__instance.playerHeldBy == null || __instance is GrabbablePlayerObject)
            {
                Plugin.log("adjustItemOffset: object is not held or other player", Plugin.LogType.Warning);
                return;
            }

            initialItemOffsets.Add(__instance.NetworkObjectId, __instance.itemProperties.positionOffset);

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

            if(initialItemOffsets.ContainsKey(__instance.NetworkObjectId))
            {
                __instance.itemProperties.positionOffset = initialItemOffsets[__instance.NetworkObjectId];
                initialItemOffsets.Remove(__instance.NetworkObjectId);
            }
        }

        public static void transformItemRelativeTo(GrabbableObject item, PlayerControllerB pcb)
        {
            if (pcb.transform.localScale.x == 1f)
                return;

            Vector3 initialOffset = initialItemOffsets.GetValueOrDefault(item.NetworkObjectId, item.itemProperties.positionOffset);

            var yOffset = (item.transform.localScale.y - (item.transform.localScale.y * pcb.transform.localScale.x)) / 2.5f; // somehow not exactly 2f as it should be for radius.. meh
            item.itemProperties.positionOffset = new Vector3(initialOffset.x, initialOffset.y - yOffset, initialOffset.z);
            item.transform.localScale *= pcb.transform.localScale.x;
        }
    }
}
