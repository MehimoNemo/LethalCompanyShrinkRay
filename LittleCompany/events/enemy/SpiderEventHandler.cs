using GameNetcodeStuff;
using LittleCompany.helper;
using System.Collections;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class SpiderEventHandler : EnemyEventHandler<SandSpiderAI>
    {
        public override void OnAwake()
        {
            DeathPoofScale = 0.5f;
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Spider event triggered.");
            if (PlayerInfo.IsHost)
            {
                for (int i = 0; i < 10; i++) // Shoot 10 webs in any direction
                {
                    // Taken from SandSpiderAI.AttemptPlaceWebTrap()
                    Vector3 direction = Vector3.Scale(Random.onUnitSphere, new Vector3(1f, Random.Range(0.5f, 3f), 1f));
                    direction.y = Mathf.Min(0f, direction.y);
                    var ray = new Ray(enemy.abdomen.position + Vector3.up * 0.4f, direction);
                    if (Physics.Raycast(ray, out RaycastHit rayHit, 7f, StartOfRound.Instance.collidersAndRoomMask))
                    {
                        if (rayHit.distance < 2f)
                            continue;

                        Vector3 point = rayHit.point;
                        if (Physics.Raycast(enemy.abdomen.position, Vector3.down, out rayHit, 10f, StartOfRound.Instance.collidersAndRoomMask))
                        {
                            Vector3 startPosition = rayHit.point + Vector3.up * 0.2f;
                            enemy.SpawnWebTrapServerRpc(startPosition, point);
                        }
                    }
                }
            }

            // Ensure webs are spawned (todo: find a cleaner way for this)
            StartCoroutine(DelayedDeathShrinking(previousSize, playerShrunkenBy));
        }

        public IEnumerator DelayedDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            enemy.SetEnemyStunned(true);
            yield return new WaitForSeconds(0.3f);
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        public override void Shrunken(bool wasShrunkenBefore, PlayerControllerB playerShrunkenBy) { }
        public override void Enlarged(bool wasEnlargedBefore, PlayerControllerB playerEnlargedBy) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
    }
}
