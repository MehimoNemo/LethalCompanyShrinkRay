using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class RobotEventHandler : EnemyEventHandler
    {
        #region Properties
        private static GameObject BurningRobotToyPrefab = null;
        #endregion

        #region Networking
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
        #endregion

        #region Base Methods
        public override void OnAwake()
        {
            base.OnAwake();
            Plugin.Log("Robot enemy handler has awaken!");
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Robot shrunken to death");

            var explosionPrefab = (enemy as RadMechAI).explosionPrefab;
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Landmine.SpawnExplosion(transform.position, true, default, default, default, default, explosionPrefab);

            if (PlayerInfo.IsHost && BurningRobotToyPrefab != null)
            {
                var toyRobotObject = Instantiate(BurningRobotToyPrefab, enemy.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                toyRobotObject.GetComponent<BurningToyRobotBehaviour>().playerWhoKilledRobot.Value = playerShrunkenBy.playerClientId;
                toyRobotObject.GetComponent<NetworkObject>().Spawn();
            }
        }
        #endregion

        [DisallowMultipleComponent]
        public class BurningToyRobotBehaviour : NetworkBehaviour
        {
            #region Properties
            GrabbableObject toyRobot = null;
            GameObject burningEffect = Effects.BurningEffect;
            float damageFrameCounter = 0;
            public NetworkVariable<ulong> playerWhoKilledRobot = new NetworkVariable<ulong>();
            Dictionary<ulong, ShrinkRayFX> boundPlayerFX = new Dictionary<ulong, ShrinkRayFX>();

            readonly int damagePerTick = 20;

            bool IsHeld => toyRobot != null && toyRobot.playerHeldBy != null;
            #endregion

            #region Base Methods
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

                var player = PlayerInfo.ControllerFromID(playerWhoKilledRobot.Value);
                BindPlayer(player);
            }

            void FixedUpdate()
            {
                damageFrameCounter++;
                damageFrameCounter %= 100;

                if (damageFrameCounter == 1 || damageFrameCounter == 50)
                {
                    if (IsHeld)
                        toyRobot.playerHeldBy.DamagePlayer(damagePerTick, true, false, CauseOfDeath.Burning);
                }

                CheckPlayerBindings(damageFrameCounter == 1);

                if (burningEffect != null)
                    burningEffect.transform.position = transform.position; // todo: transform parenting...

                if (IsHeld)
                    BindPlayer(toyRobot.playerHeldBy);
            }

            void OnDestroy()
            {
                Destroy(burningEffect);
                base.OnDestroy();
            }
            #endregion

            #region Methods
            void CheckPlayerBindings(bool dealDamage = false)
            {
                for(int i = boundPlayerFX.Count - 1; i >= 0; i--)
                {
                    var binding = boundPlayerFX.ElementAt(i);
                    var player = PlayerInfo.ControllerFromID(binding.Key);
                    var fx = binding.Value;
                    if (player == null || player.isPlayerDead || !player.isPlayerControlled)
                    {
                        Destroy(fx);
                        boundPlayerFX.Remove(binding.Key);
                        continue;
                    }

                    var distance = Vector3.Distance(toyRobot.transform.position, player.transform.position);
                    if (distance > 5f)
                    {
                        fx.colorPrimary = Color.red;
                        fx.colorSecondary = Color.red;

                        if (dealDamage)
                            player.DamagePlayer(damagePerTick / 2, true, false, CauseOfDeath.Unknown);
                    }
                    else
                    {
                        fx.colorPrimary = Color.black;
                        fx.colorSecondary = Color.black;
                    }
                }
            }

            void BindPlayer(PlayerControllerB player)
            {
                if (player == null || boundPlayerFX.ContainsKey(player.playerClientId))
                    return;

                Plugin.Log("BurningToyRobotBehaviour.BindPlayer with name: " + player.name);
                var fx = toyRobot.gameObject.AddComponent<ShrinkRayFX>();
                fx.beamDuration = 0f;
                fx.bezier2YOffset = 0f;
                fx.bezier3YOffset = 0f;
                fx.bezier2YPoint = 0f;
                fx.bezier3YPoint = 0f;
                fx.usingOffsets = false;

                StartCoroutine(fx.RenderRayBeam(toyRobot.transform, PlayerInfo.SpineOf(player)));

                fx.sparksSize = 0f;
                fx.colorPrimary = Color.black;
                fx.colorSecondary = Color.black;
                fx.noiseSpeed = 1f;
                fx.noisePower = 0.1f;

                boundPlayerFX.Add(player.playerClientId, fx);
            }
            #endregion
        }
    }
}
