﻿using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using LittleCompany.components;
using LittleCompany.helper;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    [HarmonyPatch]
    internal class BrackenEventHandler : EnemyEventHandler
    {
        public override void OnAwake()
        {
            base.OnAwake();
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (PlayerInfo.IsHost)
                SpawnBrackenOrbAt(enemy.transform.position);

            Plugin.Log("Bracken shrunken to death");
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        public void SpawnBrackenOrbAt(Vector3 position)
        {
            if (BrackenOrb != null)
                Destroy(BrackenOrb);

            BrackenOrb = Instantiate(_brackenOrbPrefab);
            BrackenOrb.GetComponent<BrackenOrbBehaviour>().origin.Value = position;
            BrackenOrb.GetComponent<NetworkObject>().Spawn();
        }

        #region BrackenOrb
        private static GameObject _brackenOrbPrefab = null;
        private static GameObject BrackenOrb;

        public static void LoadBrackenOrbPrefab()
        {
            if (_brackenOrbPrefab != null) return;

            _brackenOrbPrefab = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "EnemyEvents/Bracken/BrackenOrb.prefab"));
            if (_brackenOrbPrefab != null)
            {
                _brackenOrbPrefab.AddComponent<BrackenOrbBehaviour>();
                NetworkManager.Singleton.AddNetworkPrefab(_brackenOrbPrefab);
            }
        }

        // todo: maybe avoid this by binding the gameobject to the scene
        [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        [HarmonyPrefix()]
        public static void ShipHasLeftPrefix()
        {
            if (!PlayerInfo.IsHost) return;

            if (BrackenOrb != null)
                Destroy(BrackenOrb);
        }

        [DisallowMultipleComponent]
        public class BrackenOrbBehaviour : NetworkBehaviour
        {
            float damageFrameCounter = 0;
            public NetworkVariable<Vector3> origin = new NetworkVariable<Vector3>();
            public NetworkVariable<float> radius = new NetworkVariable<float>(0f);

            List<ulong> fearedPlayers = new List<ulong>();

            void Start()
            {
                transform.position = origin.Value;
                transform.localScale = Vector3.zero;

                foreach (var player in PlayerInfo.AllPlayers)
                {
                    var distanceToPlayer = Vector3.Distance(player.transform.position, origin.Value);
                    if (distanceToPlayer < 2f)
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    else if (distanceToPlayer < 10f || player.HasLineOfSightToPosition(transform.position))
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                }
            }

            void FixedUpdate()
            {
                damageFrameCounter++;
                damageFrameCounter %= 100;

                if(damageFrameCounter == 1)
                {
                    foreach (var player in PlayerInfo.AllPlayers)
                    {
                        var distanceToPlayer = Vector3.Distance(player.transform.position, origin.Value);
                        if (distanceToPlayer <= radius.Value)
                            player.DamagePlayer(20, true, false, CauseOfDeath.Unknown);

                        if (!fearedPlayers.Contains(player.playerClientId))
                        {
                            if(distanceToPlayer <= (radius.Value + 2f))
                                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
                            else
                                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.6f);

                            fearedPlayers.Add(player.playerClientId);
                        }
                    }
                }

                if (PlayerInfo.IsHost)
                {
                    if (radius.Value < 0.5f)
                        radius.Value += Time.deltaTime;
                    else
                        radius.Value += Time.deltaTime * 0.1f;
                }

                transform.localScale = Vector3.one * radius.Value * 2f;
            }
        }
        #endregion
    }
}
