using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.patches;
using ModelReplacement;
using ModelReplacement.Monobehaviors.Enemies;
using System.Runtime.CompilerServices;
namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class ModelReplacementApiCompatibilityPatch
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(BodyReplacementBase), "Awake")]
        [HarmonyPostfix]
        public static void BodyReplacementBase_Postfix(BodyReplacementBase __instance)
        {
            Plugin.Log("BodyReplacementBase_Postfix");
            __instance.gameObject.GetComponent<ModelReplacementApiCompatibilityComponent>().ReloadNextFrame();
        }

        // We joined
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            if (!GameNetworkManagerPatch.IsGameInitialized)
                return;
            Plugin.Log("We Joined");
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

        [HarmonyPatch(typeof(MaskedReplacementBase), "SetReplacement")]
        [HarmonyPostfix]
        public static void SetReplacementPrefix(PlayerControllerB mimicking, MaskedReplacementBase __instance)
        {
            if (mimicking == null || __instance == null) return;
            ModelReplacementApiCompatibilityComponent modelReplacement = mimicking.GetComponent<ModelReplacementApiCompatibilityComponent>();
            if(modelReplacement != null)
            {
                __instance.replacementModel.transform.localScale = modelReplacement.replacementModelOriginalScale;
            }
        }
    }
}
