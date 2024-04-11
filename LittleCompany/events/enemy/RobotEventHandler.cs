using GameNetcodeStuff;
using LittleCompany.helper;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class RobotEventHandler : EnemyEventHandler
    {
        private static GameObject BurningRobotToyPrefab = null;

        public static void LoadBurningRobotToyPrefab()
        {
            if (BurningRobotToyPrefab != null) return;

            var toyRobotIndex = ItemInfo.SpawnableItems.FindIndex((scrap) => scrap.name == "RobotToy");
            if (toyRobotIndex == -1) return;

            var toyRobotPrefab = ItemInfo.SpawnableItems[toyRobotIndex].spawnPrefab;
            BurningRobotToyPrefab = LethalLib.Modules.PrefabUtils.ClonePrefab(toyRobotPrefab);
            var toyRobot = BurningRobotToyPrefab.GetComponent<GrabbableObject>();
            toyRobot.gameObject.AddComponent<BurningToyRobotBehaviour>();
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Robot shrunken to death");

            if (PlayerInfo.IsHost && BurningRobotToyPrefab != null)
            {
                var toyRobotObject = Instantiate(BurningRobotToyPrefab, enemy.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                toyRobotObject.GetComponent<NetworkObject>().Spawn();
                Landmine.SpawnExplosion(enemy.transform.position, true, default, default, default, default, (enemy as RadMechAI).explosionPrefab);
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        #region BurningToyRobot
        [DisallowMultipleComponent]
        public class BurningToyRobotBehaviour : NetworkBehaviour
        {
            GrabbableObject toyRobot = null;
            GameObject burningEffect = Effects.BurningEffect;
            float damageFrameCounter = 0;

            readonly int damagePerTick = 20;

            void Awake()
            {
                Plugin.Log("Burning toy robot has awaken!");
                toyRobot = GetComponentInParent<GrabbableObject>();

                toyRobot.itemProperties.itemName = "Burning toy robot";
                toyRobot.transform.rotation = Quaternion.Euler(toyRobot.itemProperties.restingRotation);
                toyRobot.fallTime = 0f;
                toyRobot.SetScrapValue(Random.Range(250, 330));
                var scanNode = toyRobot.gameObject.GetComponentInChildren<ScanNodeProperties>();
                if (scanNode != null)
                    scanNode.headerText = toyRobot.itemProperties.itemName;

                if (burningEffect != null)
                {
                    burningEffect.transform.position = toyRobot.transform.position;
                    //burningEffect.transform.SetParent(toyRobot.transform, false);
                }
            }

            void FixedUpdate()
            {
                damageFrameCounter++;
                damageFrameCounter %= 50;

                if (damageFrameCounter == 1)
                {
                    if (toyRobot != null && toyRobot.playerHeldBy != null)
                        toyRobot.playerHeldBy.DamagePlayer(damagePerTick, true, false, CauseOfDeath.Burning);
                }

                if (burningEffect != null)
                    burningEffect.transform.position = toyRobot.transform.position; // todo: transform parenting...
            }

            void OnDestroy()
            {
                Destroy(burningEffect);
            }
            #endregion
        }
    }
}
