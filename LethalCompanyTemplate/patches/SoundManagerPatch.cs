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
        private static ManualLogSource mls;
        static Shrinking shrinkin = new Shrinking();

        public void Awake()
        {
            shrinkin = new Shrinking();
        }

        public static void Postfix(float pitch, int playerObjNum)
        {
            if (shrinkin != null)
            {
                shrinkin.SetPlayerPitch(pitch, playerObjNum);
            }
            else
            {
                mls.LogError("SHRINKIN IS FUCKING NULL WTF");
            }
        }
    }
}