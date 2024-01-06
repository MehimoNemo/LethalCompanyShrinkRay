using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System.Linq;
using UnityEngine;

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
                return !Shrinking.isShrunk(playerScript.gameObject);

            return __result;
        }
    }
}
