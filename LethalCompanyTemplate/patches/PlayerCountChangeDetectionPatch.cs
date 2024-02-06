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
            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.isGameInitialized)
                return;

            // cigarette
            Plugin.log("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
            Plugin.log("Player " + clientId + " joined.");

            // Place things that should run after a player joins or leaves here vVVVVvvVVVVv
            Vents.rerenderAllSussified();

            GrabbablePlayerList.Instance.SendGrabbablePlayerListServerRpc(clientId);
        }

        // Before client disconnects
        [HarmonyPatch(typeof(StartOfRound), "OnClientDisconnect")]
        [HarmonyPrefix]
        public static void OnClientDisconnect(ulong clientId)
        {
            if (!PlayerInfo.IsHost || !GameNetworkManagerPatch.isGameInitialized)
                return;

            Plugin.log("Player " + clientId + " left.");

            if(PlayerInfo.CurrentPlayer.playerClientId == clientId)
                return; // handled in GameNetworkManagerPatch.Disconnect()

            GrabbablePlayerList.Instance.RemovePlayerGrabbableServerRpc(clientId);
        }
    }
}
