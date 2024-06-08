using HarmonyLib;
using LittleCompany.components;
using LittleCompany.events.enemy;
using LittleCompany.events.item;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool IsGameInitialized = false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        [HarmonyPriority(Priority.First)]
        public static void Init()
        {
            EnemyEventManager.BindAllEventHandler();
            ItemEventManager.BindAllEventHandler();

            AssetLoader.LoadAllAssets();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        public static void Initialize()
        {
            IsGameInitialized = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            IsGameInitialized = false;
            GrabbablePlayerList.ResetAnyPlayerModificationsFor(PlayerInfo.CurrentPlayer);
            GrabbablePlayerList.ClearGrabbablePlayerObjects();
            PlayerInfo.Cleanup();
        }

        [HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip")]
        [HarmonyPrefix]
        public static void SaveItemsInShip()
        {
            GrabbableObject[] array = Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var item in array)
            {
                if (item.TryGetComponent(out ItemScaling scaling))
                    scaling.ResetItemProperties();
            }
        }
    }
}
