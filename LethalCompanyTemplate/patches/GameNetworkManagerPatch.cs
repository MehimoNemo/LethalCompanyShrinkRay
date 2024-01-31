using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool isGameInitialized = false;

        private static void SpawnNetworkPrefab(GameObject networkPrefab)
        {
            if (networkPrefab == null)
            {
                Plugin.log("A networkPrefab was null!");
                return;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = GameObject.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                if (networkHandlerHost.TryGetComponent(out NetworkObject networkObject))
                {
                    Plugin.log("Spawned a networkObject!");
                    networkObject.Spawn();
                }
            }
        }

        public static void LoadAllAssets()
        {
            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shrinkasset");
            AssetBundle upgradeAssets = AssetBundle.LoadFromFile(assetDir);

            GrabbablePlayerObject.LoadAsset(upgradeAssets);
            GrabbablePlayerList.LoadAsset(upgradeAssets);
            ShrinkRay.LoadAsset(upgradeAssets);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            LoadAllAssets();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void Awake()
        {
            GrabbablePlayerList.Initialize();
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
            GrabbablePlayerList.Instance.OnNewRound();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void endOfRound()
        {
            Plugin.log("EndOfGame");
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) // reset player sizes
            {
                if(PlayerHelper.isShrunk(player.gameObject))
                    coroutines.PlayerShrinkAnimation.StartRoutine(player, 1f);
            }

            GrabbablePlayerList.Instance.ClearGrabbablePlayerObjects();

            //reset speed, pitch(if it doesn't reset naturally)
            PlayerControllerBPatch.defaultsInitialized = false;
            Vents.unsussifyAll();
        }
    }
}
