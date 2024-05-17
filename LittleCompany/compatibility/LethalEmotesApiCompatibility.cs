using HarmonyLib;
using LittleCompany.helper;
using System.Runtime.CompilerServices;

namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class LethalEmotesApiCompatibility
    {
        public const string LethalEmotesApiReferenceChain = "com.weliveinasociety.CustomEmotesAPI";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalEmotesApiReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        private static bool ThirdPersonFixed = false;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(BoneMapper), "TurnOnThirdPerson")]
        [HarmonyPostfix]
        public static void TurnOnThirdPersonPostFix()
        {
            if (!ThirdPersonFixed)
            {
                PlayerCosmetics.RegularizePlayerCosmetics(PlayerInfo.CurrentPlayer);
                ThirdPersonFixed = true;
            }
        }
    }
}
