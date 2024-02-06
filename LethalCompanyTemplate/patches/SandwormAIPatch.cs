using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using System;
using System.Timers;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class SandwormAIPatch
    {
        public static int odds = 50; // %
        private static Timer untargetableState;

        private static void OnTargetableAgain(Object source, ElapsedEventArgs e)
        {
            Plugin.log("SandwormAIPatch: We're targetable again.");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            untargetableState = new Timer(5000); // 5s
            untargetableState.AutoReset = false;
            untargetableState.Elapsed += OnTargetableAgain;
        }

        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck, ref EnemyType ___enemyType)
        {
            if (!PlayerHelper.isShrunk(playerScript.gameObject) || ___enemyType.name != "SandWorm")
                return __result;

            if (untargetableState.Enabled)
            {
                Plugin.log("SandwormAIPatch: Attempt to target us, but we're luckily untargetable.");
                return false;
            }

            Random rnd = new Random();
            if(rnd.Next(101) < odds)
            {
                Plugin.log("SandwormAIPatch: Didn't notice us and we're 5s untargetable from now on.");
                untargetableState.Start(); // Make us untargetable by sandworms for 5s
                return false;
            }

            return true;
        }
    }
}
