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
        public static bool IsGameInitialized = false;

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

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        public static void Initialize()
        {
            IsGameInitialized = true;
            GrabbablePlayerList.CreateInstance();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            IsGameInitialized = false;
            GrabbablePlayerList.Instance.RemovePlayerGrabbableServerRpc(PlayerInfo.CurrentPlayerID); // remove us from the list, in case we were grabbable
            GrabbablePlayerList.RemoveInstance();
            PlayerModificationPatch.helmetRenderer = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void EndRound()
        {
            Plugin.Log("EndOfGame");

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) // reset player sizes
            {
                if(PlayerInfo.IsShrunk(player))
                    coroutines.PlayerShrinkAnimation.StartRoutine(player, 1f);
            }

            GrabbablePlayerList.ClearGrabbablePlayerObjects();
        }
    }
}
