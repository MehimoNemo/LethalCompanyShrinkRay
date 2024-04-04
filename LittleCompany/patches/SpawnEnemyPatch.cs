using HarmonyLib;
using System.Linq;

using LittleCompany.events.enemy;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class SpawnEnemyPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnEnemyGameObject))]
        [HarmonyPostfix]
        public static void SpawnEnemyGameObject()
        {
            Plugin.Log("SpawnEnemyGameObject");
            var enemyAI = RoundManager.Instance.SpawnedEnemies.Last();
            var eventHandler = EnemyEventManager.EventHandlerOf(enemyAI);
#if DEBUG
            if (eventHandler != null)
                eventHandler.OnDeathShrinking(); // ayoooo SPAWNKILL !!
#endif
        }
    }
}
