using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Yoga;

namespace LCShrinkRay.patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("Update")]
        static void PostFix(PlayerControllerB __instance)
        {
            SoundManager.Instance.playerVoicePitchTargets[__instance.playerClientId] = 1.2f;
        }
    }
}


