using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class HoarderBugAIPatch
    {
        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck, ref EnemyType ___enemyType)
        {
            if (ModConfig.Instance.values.hoardingBugSteal && ___enemyType.name == "HoarderBug")  // Not working yet i believe... it never wanted me :(
                return !PlayerInfo.IsShrunk(playerScript);

            return __result;
        }
    }
}
