using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

        [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        [HarmonyPrefix()]
        public static void endOfRound()
        {
            Plugin.log("EndOfGame");
            if (true)
            {
                Plugin.log("EndOfGame host");
                //reset players size, speed, pitch(if it doesn't reset naturally)
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    Shrinking.ShrinkPlayer(player.gameObject, 1, player.playerClientId);
                    PlayerControllerBPatch.defaultsInitialized = false;
                }
            }
        }
    }
}
