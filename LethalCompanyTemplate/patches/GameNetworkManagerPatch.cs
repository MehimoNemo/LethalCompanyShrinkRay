using GameNetcodeStuff;
using HarmonyLib;
using LC_API.Networking;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LCShrinkRay.comp.ShrinkRay;

namespace LCShrinkRay.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool isGameInitialized = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            ShrinkRay.AddToGame();
        }

		[HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            isGameInitialized = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        public static void Initialize()
        {
            isGameInitialized = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void endOfRound()
        {
            Plugin.log("EndOfGame");
            if (true)
            {
                Plugin.log("EndOfGame host");
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) // reset player sizes
                {
                    if(PlayerHelper.isShrunk(player.gameObject))
                        ShrinkRay.debugOnPlayerModificationWorkaround(PlayerHelper.currentPlayer(), ModificationType.Normalizing);
                }

                //reset speed, pitch(if it doesn't reset naturally)
                PlayerControllerBPatch.defaultsInitialized = false;
                Vents.unsussifyAll();
            }
        }
    }
}
