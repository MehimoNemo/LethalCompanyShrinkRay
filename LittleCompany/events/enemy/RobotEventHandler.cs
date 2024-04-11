using GameNetcodeStuff;
using LittleCompany.helper;
using System.IO;
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
            BurningRobotToyPrefab = LethalLib.Modules.NetworkPrefabs.CloneNetworkPrefab(toyRobotPrefab);
            var toyRobot = BurningRobotToyPrefab.GetComponent<GrabbableObject>();
            var burningBehaviour = BurningRobotToyPrefab.AddComponent<BurningToyRobotBehaviour>();
        }

        public override void OnAwake()
        {
            base.OnAwake();
            Plugin.Log("Robot enemy handler has awaken!");
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Robot shrunken to death");

            if (PlayerInfo.IsHost && BurningRobotToyPrefab != null)
            {
                var toyRobotObject = Instantiate(BurningRobotToyPrefab, enemy.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                toyRobotObject.GetComponent<NetworkObject>().Spawn();
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

            void Start()
            {
                Plugin.Log("Burning toy robot has spawned!");

                toyRobot = GetComponentInParent<GrabbableObject>();

                toyRobot.itemProperties.itemName = "Burning toy robot";
                toyRobot.transform.rotation = Quaternion.Euler(toyRobot.itemProperties.restingRotation);
                toyRobot.fallTime = 0f;
                toyRobot.SetScrapValue(Random.Range(250, 330));
                var scanNode = toyRobot.gameObject.GetComponentInChildren<ScanNodeProperties>();
                if (scanNode != null)
                    scanNode.headerText = toyRobot.itemProperties.itemName;

                Landmine.SpawnExplosion(transform.position, true);
                //radmechai.explosionPrefab
            }

            void FixedUpdate()
            {
                damageFrameCounter++;
                damageFrameCounter %= 50;

                if (damageFrameCounter == 1)
                {
                    Plugin.Log("Position: " + burningEffect.transform.position);
                    if (toyRobot != null && toyRobot.playerHeldBy != null)
                        toyRobot.playerHeldBy.DamagePlayer(damagePerTick, true, false, CauseOfDeath.Burning);
                }

                if (burningEffect != null)
                    burningEffect.transform.position = transform.position; // todo: transform parenting...
            }

            void OnDestroy()
            {
                Destroy(burningEffect);
            }
            #endregion
        }
    }
}
