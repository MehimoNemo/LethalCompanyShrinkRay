using GameNetcodeStuff;
using UnityEngine;
using System.Collections;

using LittleCompany.helper;
using LittleCompany.modifications;
using static LittleCompany.helper.EnemyInfo;
using static LittleCompany.modifications.Modification;
using static LittleCompany.events.enemy.EnemyEventManager;
using static LittleCompany.components.TargetScaling<EnemyAI>;

namespace LittleCompany.events.enemy
{
    internal class WormEventHandler : EnemyEventHandler
    {
        Transform wormBody = null;

        public override void OnAwake()
        {
            base.OnAwake();
            wormBody = enemy.transform.Find("MeshContainer")?.Find("Armature")?.Find("Bone");
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Worm shrunken to death");

            var worm = enemy as SandWormAI;
            var emergePosition = enemy.transform.position;
            enemy.transform.position = worm.endOfFlightPathPosition;
            enemy.transform.Rotate(new Vector3(0f, 180f, 0f));

            var scaling = EnemyModification.ScalingOf(enemy);
            scaling.ScaleOverTimeTo(previousSize, playerShrunkenBy, null, 1f, Mode.Linear);

            if (PlayerInfo.IsHost)
            {
                StartCoroutine(SpawnWormAfterGroundHitOf(worm, emergePosition));

                Plugin.Log("Looking for goldbar item");
                var itemIndex = ItemInfo.SpawnableItems.FindIndex((scrap) => scrap.name == "GoldBar");
                if (itemIndex != -1)
                {
                    Plugin.Log("Found goldBar");
                    var goldBarObject = Instantiate(ItemInfo.SpawnableItems[itemIndex].spawnPrefab, emergePosition, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                    var goldBar = goldBarObject.GetComponent<GrabbableObject>();
                    goldBar.NetworkObject.Spawn();
                    goldBar.fallTime = 0f;
                }
            }

            //base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        public override void Scaled(float from, float to, PlayerControllerB playerBy) // todo: outsource to its own class with custom enemy scalers
        {
            enemy.transform.localScale = Vector3.one;
            
            if (wormBody != null)
                wormBody.transform.localScale = Vector3.one * to;

            base.Scaled(from, to, playerBy);
        }

        public IEnumerator SpawnWormAfterGroundHitOf(SandWormAI worm, Vector3 spawnPosition)
        {
            Plugin.Log("Wait for worm to hit ground");
            yield return new WaitWhile(() => worm.inSpecialAnimation);

            Plugin.Log("Spawn other worm");
            var spawnedWorm = SpawnEnemyAt(spawnPosition, 0f, worm.enemyType) as SandWormAI;
            yield return new WaitForSeconds(0.5f);
            spawnedWorm.StartEmergeAnimation();
        }

#if DEBUG
        #region Testing
        public static IEnumerator WormTest()
        {
            var enemyType = EnemyTypeByName(EnemyNameOf(Enemy.Worm));
            if (enemyType == null)
            {
                Plugin.Log("No worm enemy found..");
                yield break;
            }

            var location = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 15;
            var enemy = SpawnEnemyAt(location, 0f, enemyType) as SandWormAI;

            Plugin.Log("Spawning worm!");
            yield return new WaitForSeconds(0.5f);

            Plugin.Log("Start emerging!");
            enemy.StartEmergeAnimation();

            yield return new WaitUntil(() => !enemy.emerged);
            Plugin.Log("Emerged!");
            yield return new WaitForSeconds(3f);

            Plugin.Log("Scale!");
            EnemyModification.ApplyModificationTo(enemy, ModificationType.Shrinking, PlayerInfo.CurrentPlayer);
        }
        #endregion
#endif
    }
}
