using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class SlimeEventHandler : EnemyEventHandler
    {
        [DisallowMultipleComponent]
        public class TinySlimeBehaviour : MonoBehaviour
        {
            private BlobAI slime = null;
            void Awake()
            {
                slime = GetComponent<BlobAI>();
                if (slime != null)
                {
                    Plugin.Log("Found slime.");
                    slime.transform.localScale = Vector3.one * SlimeCloneSize;
                }

            }

            void Update()
            {
                if(slime?.agent != null)
                    slime.agent.speed = 4f;
            }
        }

        internal const float SlimeCloneSize = 0.2f;
        internal const int SlimeCloneAmount = 5;
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (enemy.GetComponent<TinySlimeBehaviour>() == null) // Not a tiny slime
            {
                Plugin.Log("Slime event triggered.");
                if (PlayerInfo.IsHost)
                {
                    // Spawn <SlimeCloneAmount> slimes around the one who died
                    var enemyPosition = enemy.transform.position + Vector3.up * 0.5f;
                    for (int i = 1; i <= SlimeCloneAmount; i++)
                    {
                        var angle = 2 * Mathf.PI / SlimeCloneAmount * i;
                        var direction = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));

                        var spawnPosition = enemyPosition + direction * 1.5f;
                        if (Physics.Raycast(new Ray(enemyPosition, direction), out RaycastHit rayHit, 1.5f, StartOfRound.Instance.collidersAndRoomMask))
                        {
                            Plugin.Log("Raycast hit.");
                            spawnPosition = rayHit.point - direction * (SlimeCloneSize / 2);
                        }

                        var slime = EnemyInfo.SpawnEnemyAt(spawnPosition, 0f, enemy.enemyType);
                        if (slime == null)
                        {
                            Plugin.Log("Unable to create slime clone from event.", Plugin.LogType.Error);
                            continue;
                        }
                        slime.GetComponent<SlimeEventHandler>()?.AddTinySlimeBehaviorClientRpc();
                    }
                }
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Plugin.Log("Slime shrunken to death. But at what cost..");
        }

        [ClientRpc]
        public void AddTinySlimeBehaviorClientRpc()
        {
            gameObject.AddComponent<TinySlimeBehaviour>();
        }
    }
}
