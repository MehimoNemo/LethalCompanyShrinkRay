using LittleCompany.helper;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class SpiderEventHandler : EnemyEventHandler
    {
        public override void OnAwake()
        {
            DeathPoofScale = 0.5f;
        }

        public override void OnDeathShrinking(float previousSize)
        {
            Plugin.Log("Spider event triggered.");
            if (PlayerInfo.IsHost)
            {
                for (int i = 0; i < 10; i++) // Shoot 10 webs in any direction
                {
                    // Taken from SandSpiderAI.AttemptPlaceWebTrap()
                    Vector3 direction = Vector3.Scale(Random.onUnitSphere, new Vector3(1f, Random.Range(0.5f, 1f), 1f));
                    direction.y = Mathf.Min(0f, direction.y);
                    var ray = new Ray((enemy as SandSpiderAI).abdomen.position + Vector3.up * 0.4f, direction);
                    if (Physics.Raycast(ray, out RaycastHit rayHit, 7f, StartOfRound.Instance.collidersAndRoomMask))
                    {
                        if (rayHit.distance < 2f)
                            continue;

                        Vector3 point = rayHit.point;
                        if (Physics.Raycast((enemy as SandSpiderAI).abdomen.position, Vector3.down, out rayHit, 10f, StartOfRound.Instance.collidersAndRoomMask))
                        {
                            Vector3 startPosition = rayHit.point + Vector3.up * 0.2f;
                            (enemy as SandSpiderAI).SpawnWebTrapServerRpc(startPosition, point);
                        }
                    }
                }
            }

            base.OnDeathShrinking(previousSize);
        }
        public override void Shrunken(bool wasShrunkenBefore) { }
        public override void Enlarged(bool wasEnlargedBefore) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged) { }
    }
}
