using BepInEx.Logging;
using HarmonyLib;
using LCShrinkRay.comp;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch(typeof(SoundManager))]
    [HarmonyPatch("SetPlayerPitch")]
    class SoundManagerPatch
    {
        static Shrinking shrinkin = new Shrinking();

        public void Awake()
        {
            shrinkin = new Shrinking();
        }

        public static void Postfix(float pitch, ulong playerID)
        {
            if (shrinkin != null)
            {
                shrinkin.SetPlayerPitch(pitch, playerID);
            }
            else
            {
                Plugin.log("SHRINKIN IS FUCKING NULL WTF", Plugin.LogType.Error);
            }
        }
    }
}