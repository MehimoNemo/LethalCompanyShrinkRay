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
        public static void Postfix(float pitch, int playerObjNum)
        {
            shrinkin.SetPlayerPitch(pitch, playerObjNum);
        }
        


    }


}
