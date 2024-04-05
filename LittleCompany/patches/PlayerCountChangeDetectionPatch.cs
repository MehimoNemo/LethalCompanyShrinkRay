﻿using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class PlayerCountChangeDetection
    {
        // After a client joins
        [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
        [HarmonyPrefix]
        public static void OnClientConnect(ulong clientId)
        {
            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.IsGameInitialized)
                return;

            Plugin.Log("Player " + clientId + " joined.");
        }


        // After client joined
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject(PlayerControllerB __instance, float ___sprintMultiplier, float ___jumpForce)
        {
            if (!GameNetworkManagerPatch.IsGameInitialized)
                return;

            // cigarette
            Plugin.Log("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
            Plugin.Log("We joined a lobby.");
            AddPlayerScalingToEveryScaledPlayer();
        }

        private static void AddPlayerScalingToEveryScaledPlayer()
        {
            Plugin.Log("Adding Scaling to every scaled player");
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            foreach (var player in players)
            {
                if (!PlayerInfo.IsNormalSize(player))
                {
                    float size = PlayerInfo.SizeOf(player);
                    PlayerScaling playerScaling = player.gameObject.AddComponent<PlayerScaling>();
                    playerScaling.OriginalSize = Vector3.one;
                    playerScaling.CurrentScale = size;
                }
            }
        }

        // Before client disconnects
        [HarmonyPatch(typeof(StartOfRound), "OnClientDisconnect")]
        [HarmonyPrefix]
        public static void OnClientDisconnect(ulong clientId)
        {
            if (GrabbablePlayerList.TryFindGrabbableObjectByHolder(clientId, out GrabbablePlayerObject gpo))
                gpo.DiscardItem();

            GrabbablePlayerList.ResetAnyPlayerModificationsFor(PlayerInfo.ControllerFromID(clientId));

            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.IsGameInitialized)
                return;

            Plugin.Log("Player " + clientId + " left.");
            GrabbablePlayerList.RemovePlayerGrabbable(clientId);
        }
    }
}
