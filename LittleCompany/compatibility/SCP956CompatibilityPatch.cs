using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;

namespace LittleCompany.compatibility
{
    internal class SCP956CompatibilityPatch
    {
        public const string SCP956ApiReferenceChain = "Snowlance.Pinata";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    foreach(var a in BepInEx.Bootstrap.Chainloader.PluginInfos)
                    {
                        Plugin.Log(a.Key);
                    }
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(SCP956ApiReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        [HarmonyPatch(typeof(SCP956.NetworkHandler), "ChangePlayerSizeClientRpc")]
        [HarmonyPostfix]
        public static void ChangePlayerSizeClientRpcPostfix(ulong clientId, float size)
        {
            Plugin.Log("SCP956 Compatibility Scale");
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];
            PlayerScaling playerScaling = player.GetComponent<PlayerScaling>();
            if(playerScaling != null)
            {
                playerScaling.ScaleTo(size, player);
                playerScaling.CallListenersAtEndOfScaling(size, size, player);
            }
        }
    }
}
