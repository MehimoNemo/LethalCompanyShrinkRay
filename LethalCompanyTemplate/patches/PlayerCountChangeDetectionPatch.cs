using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerCountChangeDetection
    {
        // After client joined
        [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
        [HarmonyPostfix]
        public static void OnClientConnect(ulong clientId)
        {
            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.IsGameInitialized)
                return;

            // cigarette
            Plugin.Log("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
            Plugin.Log("Player " + clientId + " joined.");

            GrabbablePlayerList.Instance.SyncInstanceServerRpc();
            GrabbablePlayerList.Instance.SyncGrabbablePlayerListServerRpc();
        }

        // Before client disconnects
        [HarmonyPatch(typeof(StartOfRound), "OnClientDisconnect")]
        [HarmonyPrefix]
        public static void OnClientDisconnect(ulong clientId)
        {
            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.IsGameInitialized)
                return;

            Plugin.Log("Player " + clientId + " left.");
        }
    }
}
