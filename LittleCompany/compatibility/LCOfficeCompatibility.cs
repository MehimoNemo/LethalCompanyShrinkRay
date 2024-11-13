using HarmonyLib;
using LCOffice.Patches;
using System.Runtime.CompilerServices;

namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class LCOfficeCompatibility
    {
        public const string LCOfficeReferenceChain = "Piggy.LCOffice";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LCOfficeReferenceChain);
                }
                return (bool)_enabled;
            }
        }
    }
}
