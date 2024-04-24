﻿using GameNetcodeStuff;
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

namespace LittleCompany.events.enemy
{
    internal class RobotEventHandler : EnemyEventHandler
    {
        #region Properties
        private static GameObject BurningRobotToyPrefab = null;
        private static readonly string BurningToyRobotName = "Burning toy robot";
        #endregion

        #region Networking
        public static void LoadBurningRobotToyPrefab()
        {
            if (BurningRobotToyPrefab != null) return;

            var spawnableItem = AssetLoader.littleCompanyAsset.LoadAsset<Item>("ShrinkingPotionItem.asset");
            var toyRobotPrefab = spawnableItem.spawnPrefab;
            BurningRobotToyPrefab = ScrapManagementFacade.CloneNetworkPrefab(toyRobotPrefab, BurningToyRobotName);
            var toyRobot = BurningRobotToyPrefab.GetComponent<GrabbableObject>();

            toyRobot.itemProperties = Instantiate(toyRobot.itemProperties);
            toyRobot.itemProperties.itemId = 2047483647;
            toyRobot.itemProperties.itemName = BurningToyRobotName;
            toyRobot.name = BurningToyRobotName;
            toyRobot.fallTime = 0f;

            var burningBehaviour = toyRobot.gameObject.AddComponent<BurningToyRobotBehaviour>();
            toyRobot.gameObject.AddComponent<ShrinkRayFX>();

            ScrapManagementFacade.RegisterItem(toyRobot.itemProperties);

            ObjectModification.UnscalableObjects.Add(BurningToyRobotName);
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

                foreach (MeshRenderer mesh in Materials.GetMeshRenderers(toyRobot.gameObject))
                {
                    List<Material> materials = new List<Material>();
                    foreach (var m in mesh.materials)
                        materials.Add(Materials.BurntMaterial);
                    mesh.materials = materials.ToArray();
                }
                if (playerWhoKilledRobot == null)
                    return;
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
