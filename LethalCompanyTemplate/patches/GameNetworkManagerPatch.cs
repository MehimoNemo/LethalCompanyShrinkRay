using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool isGameInitialized = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shrinkasset");
            AssetBundle upgradeAssets = AssetBundle.LoadFromFile(assetDir);

            GrabbablePlayerObject.loadAsset(upgradeAssets);
            ShrinkRay.loadAsset(upgradeAssets);
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
                        ShrinkRay.debugOnPlayerModificationWorkaround(PlayerHelper.currentPlayer(), ShrinkRay.ModificationType.Normalizing);
                }

                //reset speed, pitch(if it doesn't reset naturally)
                PlayerControllerBPatch.defaultsInitialized = false;
                Vents.unsussifyAll();
            }
        }
    }
}
