using GameNetcodeStuff;
using LittleCompany.helper;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class ButlerEventHandler : EnemyEventHandler<ButlerEnemyAI>
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Butler shrunken to death");

            if (PlayerInfo.IsHost)
            {
                // Taken from ButlerEnemyAI.ButlerBlowUpAndPop
                for (int i = 0; i < 3; i++)
                    RoundManager.Instance.SpawnEnemyGameObject(enemy.transform.position, 0f, -1, enemy.butlerBeesEnemyType);

                Instantiate(enemy.knifePrefab, enemy.transform.position + Vector3.up * 0.5f, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer).GetComponent<NetworkObject>().Spawn();
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }
    }
}
