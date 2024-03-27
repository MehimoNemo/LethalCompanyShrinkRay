using HarmonyLib;
using System.Runtime.CompilerServices;

namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class LethalEmotesApiCompatibility
    {
        public const string LethalEmotesApiReferenceChain = "com.weliveinasociety.CustomEmotesAPI";

        private static bool? _enabled;

        public static bool enabled
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

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(BoneMapper), "TurnOnThirdPerson")]
        [HarmonyPostfix]
        public static void TurnOnThirdPersonPostFix()
        {
            PlayerCosmetics.RegularizeCosmetics();
        }
    }
}
