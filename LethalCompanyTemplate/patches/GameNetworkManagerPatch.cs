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
                foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) // reset player sizes
                {
                    if(PlayerHelper.isShrunk(player.gameObject))
                    {
                        OnRayHitPlayer(PlayerHelper.currentPlayer());
                        Network.Broadcast("OnRayHitPlayerSync", new PlayerHitData() { playerID = PlayerHelper.currentPlayer().playerClientId, modificationType = ModificationType.Normalizing });
                    }
                }

                //reset speed, pitch(if it doesn't reset naturally)
                PlayerControllerBPatch.defaultsInitialized = false;
                Vents.unsussifyAll();
            }
        }
    }
}
