using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LCShrinkRay.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool isGameInitialized = false;
        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPostfix()]
        public static void Uninitialize()
        {
            isGameInitialized = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void Initialize()
        {
            isGameInitialized = true;
        }
    }
}
