using GameNetcodeStuff;
using LethalLib.Modules;
using LittleCompany.helper;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class RobotEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Robot shrunken to death");

            if (PlayerInfo.IsHost)
            {
                var toyRobotIndex = ItemInfo.SpawnableItems.FindIndex((scrap) => scrap.name == "RobotToy");
                if (toyRobotIndex != -1)
                {
                    var toyRobotPrefab = ItemInfo.SpawnableItems[toyRobotIndex].spawnPrefab;
                    var toyRobotObject = Instantiate(toyRobotPrefab, enemy.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                    var toyRobot = toyRobotObject.GetComponent<GrabbableObject>();
                    toyRobot.gameObject.AddComponent<BurningToyRobotBehaviour>();
                    toyRobotObject.GetComponent<NetworkObject>().Spawn();

                    var robotAI = enemy as RadMechAI;
                    var explosion = Instantiate(robotAI.explosionPrefab);
                    Landmine.SpawnExplosion(toyRobot.transform.position, true, default, default, default, default, robotAI.explosionPrefab);
                }
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        #region BurningToyRobot
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
                    Plugin.Log("BurningToyRobotBehaviour: Checking for damage..");
                    if (toyRobot != null && toyRobot.playerHeldBy != null)
                    {
                        Plugin.Log("BurningToyRobotBehaviour: Dealing damage!");
                        toyRobot.playerHeldBy.DamagePlayer(damagePerTick, true, false, CauseOfDeath.Burning);
                    }
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
