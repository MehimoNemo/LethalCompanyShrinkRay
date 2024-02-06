using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LCShrinkRay.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool isGameInitialized = false;

        public static void LoadAllAssets()
        {
            string assetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // shrinkassets
            var shrinkAssets = AssetBundle.LoadFromFile(Path.Combine(assetDir, "shrinkasset"));
            GrabbablePlayerObject.LoadAsset(shrinkAssets);
            ShrinkRay.LoadAsset(shrinkAssets);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            LoadAllAssets();
            GrabbablePlayerList.CreateNetworkPrefab();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        public static void Initialize()
        {
            isGameInitialized = true;
            GrabbablePlayerList.CreateInstance();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            isGameInitialized = false;
            GrabbablePlayerList.RemoveInstance();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void EndOfRound()
        {
            Plugin.log("EndOfGame");

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) // reset player sizes
            {
                if(PlayerInfo.IsShrunk(player))
                    coroutines.PlayerShrinkAnimation.StartRoutine(player, 1f);
            }

            GrabbablePlayerList.ClearGrabbablePlayerObjects();
            Vents.unsussifyAll();
        }
    }
}
