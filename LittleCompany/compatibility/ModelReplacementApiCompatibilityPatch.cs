using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.patches;
using System.Collections.Generic;
namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class ModelReplacementApiCompatibilityPatch
    {
        public static Dictionary<ulong, int> PlayerToSuit = new Dictionary<ulong, int>();

        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void DetectSuitChange_LateUpdate_Postfix(PlayerControllerB __instance)
        {
            
            if (PlayerChangedSuit(__instance))
            {
                Plugin.Log("DetectSuitChange");
                // Update the suit in the map
                PlayerToSuit[__instance.playerClientId] = __instance.currentSuitID;
                __instance.GetComponent<ModelReplacementApiCompatibilityComponent>()?.ReloadCurrentReplacementModel();
            }
        }

        public static bool PlayerChangedSuit(PlayerControllerB pcb)
        {
            return PlayerToSuit.GetValueOrDefault(pcb.playerClientId, -1) != pcb.currentSuitID;
        }

        // We joined
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            if (!GameNetworkManagerPatch.IsGameInitialized)
                return;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                player.gameObject.AddComponent<ModelReplacementApiCompatibilityComponent>();
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
            player.gameObject.AddComponent<ModelReplacementApiCompatibilityComponent>();
        }
    }
}
