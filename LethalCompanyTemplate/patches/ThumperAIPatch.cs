using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    class ThumperAIPatch
    {
        [HarmonyPatch(typeof(CrawlerAI), "OnCollideWithPlayer")]
        [HarmonyPostfix]
        public static void OnCollideWithPlayer(CrawlerAI __instance, Collider other)
        {
            if (!PlayerInfo.IsCurrentPlayerShrunk) return;

            var pcb = __instance.MeetsStandardPlayerCollisionConditions(other);
            if (!pcb) return;

            if(pcb.playerClientId != PlayerInfo.CurrentPlayerID) return;

            switch (ModConfig.Instance.values.thumperBehaviour)
            {
                case ModConfig.ThumperBehaviour.OneShot:
                    pcb.KillPlayer(bodyVelocity: Vector3.zero, spawnBody: false, CauseOfDeath.Mauling);
                    break;
                case ModConfig.ThumperBehaviour.Bumper:
                    coroutines.PlayerThrowAnimation.StartRoutine(pcb, __instance.transform.forward + Vector3.up * 0.15f, 20f);
                    break;
                default:
                    break;
            }
        }
    }
}
