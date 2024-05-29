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

        // TEMP FIX UNTIL LC_OFFICE REMOVES THEIR SCALE RESET
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [HarmonyPatch(typeof(ItemElevatorCheck), "LateUpdate")]
        [HarmonyPrefix]
        public static void ResetOriginalScaleOfItemElevatorCheck(ItemElevatorCheck __instance)
        {
            // LC_Office uses the value from orgScale to reset the localScale of the item it is attached to.
            // If the orgScale is set to LocalScale, it won't reset it
            // It isn't used by anything else so it should not have any impact
            __instance.orgScale = __instance.transform.localScale;
        }
    }
}
