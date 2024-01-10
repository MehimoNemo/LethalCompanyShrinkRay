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
    [HarmonyPatch]
    internal class PlayerCountChangeDetection
    {
        public static List<GameObject> currentPlayerList { get; set; }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix()]
        public static void Initialize()
        {
            currentPlayerList = PlayerHelper.getAllPlayers();
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance)
        {
            var newPlayerList = PlayerHelper.getAllPlayers();
            if (currentPlayerList == null || currentPlayerList.Count == newPlayerList.Count)
            {
                return;
            }

            currentPlayerList = newPlayerList;

            // cigarette
            Plugin.log("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
            Plugin.log("Detected miscounted players, trying to update");

            // Place things that should run after a player joins or leaves here vVVVVvvVVVVv

            // re-enable renderers for all vent covers
            MeshRenderer renderer = GameObject.Find("VentEntrance").gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject.GetComponentsInChildren<MeshRenderer>()[0];
            renderer.enabled = true;

            // Self explains, plus I put a million comments around this function
            GrabbablePlayerList.UpdateGrabbablePlayerObjects();
        }
    }
}
