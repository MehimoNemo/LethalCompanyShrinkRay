using GameNetcodeStuff;
using LittleCompany.helper;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class CustomEnemyEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (PlayerInfo.IsHost)
            {
                for (int i = 0; i < 2; i++)
                {
                    var spawnPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(enemy.transform.position, 150f, default, new System.Random());
                    var spawnedEnemy = EnemyInfo.SpawnEnemyAt(spawnPosition, 0f, enemy.enemyType);
                    if (spawnedEnemy != null)
                        Plugin.Log("Spawned enemy " + spawnedEnemy.enemyType.enemyName + " at position " + spawnPosition);
                }
            }
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }
    }
}
