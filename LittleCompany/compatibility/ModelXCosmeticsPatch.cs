using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.patches;

namespace LittleCompany.compatibility
{
    internal class ModelXCosmeticsPatch
    {
        // We joined
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            if (!GameNetworkManagerPatch.IsGameInitialized)
                return;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                player.gameObject.AddComponent<ModelXCosmeticsComponent>();
            }
        }

        // After a client joins the game
        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
        [HarmonyPrefix]
        public static void OnPlayerConnectedClientRpc(ulong assignedPlayerObjectId)
        {
            var player = StartOfRound.Instance.allPlayerScripts[assignedPlayerObjectId];
            if (player == null)
            {
                Plugin.Log("Player joined without a player script.", Plugin.LogType.Error);
                return;
            }
            player.gameObject.AddComponent<ModelXCosmeticsComponent>();
        }
    }
}
