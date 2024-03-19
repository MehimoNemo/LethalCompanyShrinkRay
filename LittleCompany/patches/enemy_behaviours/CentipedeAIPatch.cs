using HarmonyLib;
using LittleCompany.helper;
using UnityEngine;

namespace LittleCompany.patches.EnemyBehaviours
{
    [HarmonyPatch]
    internal class CentipedeAIPatch
    {
        [HarmonyPatch(typeof(CentipedeAI), "StopClingingToPlayer")]
        [HarmonyPostfix]
        public static void StopClingingToPlayer(CentipedeAI __instance, bool playerDead, Coroutine ___killAnimationCoroutine)
        {
            if (!__instance.IsOwner || !playerDead || __instance.clingingToPlayer == null) return;

            if (PlayerInfo.SizeOf(__instance.clingingToPlayer) == 0f) // Player shrunk to death while being facehugged
            {
                __instance.KillEnemyOnOwnerClient();

                if (___killAnimationCoroutine != null)
                    __instance.StopCoroutine(___killAnimationCoroutine);
            }

        }
    }
}
