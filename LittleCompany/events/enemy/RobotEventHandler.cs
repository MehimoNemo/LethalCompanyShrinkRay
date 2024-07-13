using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.dependency;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;
using System.IO;

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

            var spawnableItem = AssetLoader.littleCompanyAsset.LoadAsset<Item>(Path.Combine(AssetLoader.BaseAssetPath, "EnemyEvents/Robot/BurningRobotToy.asset"));
            if(spawnableItem == null)
            {
                Plugin.Log("BurningRobotToy.asset not found.", Plugin.LogType.Error);
                return;
            }

            BurningRobotToyPrefab = spawnableItem.spawnPrefab;

            var toyRobot = BurningRobotToyPrefab.GetComponent<GrabbableObject>();
            toyRobot.gameObject.AddComponent<BurningToyRobotBehaviour>();

            ScrapManagementFacade.RegisterItem(toyRobot.itemProperties);

            ItemModification.UnscalableItems.Add(toyRobot.itemProperties.itemName);
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

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Landmine.SpawnExplosion(transform.position, true);

            if (PlayerInfo.IsHost && BurningRobotToyPrefab != null)
            {
                var toyRobotObject = Instantiate(BurningRobotToyPrefab, enemy.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
                var toyRobot = toyRobotObject.GetComponent<GrabbableObject>();
                if (toyRobot.TryGetComponent(out BurningToyRobotBehaviour behaviour))
                    behaviour.playerWhoKilledRobot.Value = playerShrunkenBy.playerClientId;

                toyRobot.NetworkObject.Spawn();
            }
        }
        #endregion

        [DisallowMultipleComponent]
        public class BurningToyRobotBehaviour : NetworkBehaviour
        {
            #region Properties
            public NetworkVariable<ulong> playerWhoKilledRobot = new NetworkVariable<ulong>();

            GrabbableObject toyRobot = null;
            float damageFrameCounter = 0;
            Dictionary<ulong, ShrinkRayFX> boundPlayerFX = new Dictionary<ulong, ShrinkRayFX>();

            readonly int damagePerTick = 20;

            bool IsHeld => toyRobot != null && toyRobot.playerHeldBy != null;
            #endregion

            #region Base Methods

            public override void OnNetworkDespawn()
            {
                Plugin.Log("Burning toy robot has despawned");

                ClearBindings();

                base.OnNetworkSpawn();
            }

            void Awake()
            {
                Plugin.Log("Burning toy robot has awaken");

                toyRobot = GetComponentInParent<GrabbableObject>();

                BindPlayer(PlayerInfo.ControllerFromID(playerWhoKilledRobot.Value));
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
