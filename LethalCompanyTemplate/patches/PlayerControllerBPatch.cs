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
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void OnUpdate(PlayerControllerB __instance)
        {
            //SoundManager.Instance.playerVoicePitchTargets[__instance.playerClientId] = 1.2f;
            Shrinking.Instance.Update();
        }
    }
}


