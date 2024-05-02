﻿using HarmonyLib;
using LittleCompany.components;
using LittleCompany.events.enemy;
using LittleCompany.helper;

namespace LittleCompany.patches
{
    internal class GameNetworkManagerPatch
    {
        public static bool IsGameInitialized = false;

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        [HarmonyPriority(Priority.First)]
        public static void Init()
        {
            EnemyEventManager.BindAllEnemyEvents();
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
    }
}
