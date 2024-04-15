using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
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

            toyRobot.itemProperties = Instantiate(toyRobot.itemProperties);
            toyRobot.itemProperties.itemId = 2047483647;
            toyRobot.itemProperties.itemName = "Burning toy robot";
            toyRobot.fallTime = 0f;
            
            var burningBehaviour = toyRobot.gameObject.AddComponent<BurningToyRobotBehaviour>();
            toyRobot.gameObject.AddComponent<ShrinkRayFX>();

            Items.RegisterItem(toyRobot.itemProperties);
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
                var toyRobot = toyRobotObject.GetComponent<GrabbableObject>();
                if (toyRobotObject.TryGetComponent(out BurningToyRobotBehaviour behaviour))
                {
                    behaviour.playerWhoKilledRobot.Value = playerShrunkenBy.playerClientId;
                    behaviour.scrapValue.Value = Random.Range(250, 330);
                }
                toyRobot.NetworkObject.Spawn();
                toyRobot.hideFlags = HideFlags.None;
            }
        }
        #endregion

        [DisallowMultipleComponent]
        public class BurningToyRobotBehaviour : NetworkBehaviour
        {
            #region Properties
            public NetworkVariable<ulong> playerWhoKilledRobot = new NetworkVariable<ulong>();
            public NetworkVariable<int> scrapValue = new NetworkVariable<int>();

            GrabbableObject toyRobot = null;
            GameObject burningEffect = Effects.BurningEffect;
            float damageFrameCounter = 0;
            Dictionary<ulong, ShrinkRayFX> boundPlayerFX = new Dictionary<ulong, ShrinkRayFX>();

            readonly int damagePerTick = 20;

            bool IsHeld => toyRobot != null && toyRobot.playerHeldBy != null;
            #endregion

            #region Base Methods
            public override void OnNetworkSpawn()
            {
                Plugin.Log("Burning toy robot has spawned");
                base.OnNetworkSpawn();
            }

            public override void OnNetworkDespawn()
            {
                Plugin.Log("Burning toy robot has despawned");

                DestroyImmediate(burningEffect);

                ClearBindings();

                base.OnNetworkSpawn();
            }

            void Start()
            {
                toyRobot = GetComponentInParent<GrabbableObject>();

                toyRobot.SetScrapValue(scrapValue.Value);
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
                        Plugin.Log("Attempt to destroy fx of player from burning robot toy");
                        DestroyImmediate(fx);
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
                if (player == null || !player.isPlayerControlled || player.isPlayerDead)
                    return;

                if (StartOfRound.Instance == null || StartOfRound.Instance.inShipPhase)
                    return;

                if (boundPlayerFX.ContainsKey(player.playerClientId))
                    return;

                Plugin.Log("BurningToyRobotBehaviour.BindPlayer with name: " + player.name);
                var fx = toyRobot.gameObject.AddComponent<ShrinkRayFX>();
                fx.beamDuration = 0f;
                fx.bezier2YOffset = 0f;
                fx.bezier3YOffset = 0f;
                fx.bezier2YPoint = 0f;
                fx.bezier3YPoint = 0f;
                fx.usingOffsets = false;

                fx.RenderRayBeam(toyRobot.transform, PlayerInfo.SpineOf(player));

                fx.sparksSize = 0f;
                fx.colorPrimary = Color.black;
                fx.colorSecondary = Color.black;
                fx.noiseSpeed = 1f;
                fx.noisePower = 0.1f;

                boundPlayerFX.Add(player.playerClientId, fx);
            }

            public void ClearBindings()
            {
                foreach (var fx in boundPlayerFX.Values)
                {
                    Plugin.Log("Destroyed fx.");
                    DestroyImmediate(fx);
                }
                boundPlayerFX.Clear();
            }
            #endregion
        }

        #region Patches
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndGameClientRpc")]
        public static void ClearRobotBindings()
        {
            var robotBehaviours = Resources.FindObjectsOfTypeAll<BurningToyRobotBehaviour>();
            foreach (var behaviour in robotBehaviours)
                behaviour.ClearBindings();
        }
        #endregion
    }
}
