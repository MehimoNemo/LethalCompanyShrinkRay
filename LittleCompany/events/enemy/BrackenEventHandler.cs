using GameNetcodeStuff;
using LittleCompany.helper;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class BrackenEventHandler : EnemyEventHandler
    {

        public override void OnAwake()
        {
            base.OnAwake();

            LoadBrackenOrbPrefab();
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (PlayerInfo.IsHost)
                SpawnBrackenOrbAt(enemy.transform.position);

            Plugin.Log("Bracken shrunken to death");
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }
        public override void Shrunken(bool wasShrunkenBefore, PlayerControllerB playerShrunkenBy) { }
        public override void Enlarged(bool wasEnlargedBefore, PlayerControllerB playerEnlargedBy) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }

        public void SpawnBrackenOrbAt(Vector3 position)
        {
            var brackenOrb = Instantiate(_brackenOrbPrefab);
            brackenOrb.GetComponent<NetworkObject>().Spawn();
            brackenOrb.transform.position = position;
        }

        #region BrackenOrb
        private static GameObject _brackenOrbPrefab = null;

        public static void LoadBrackenOrbPrefab()
        {
            if (_brackenOrbPrefab != null) return;

            _brackenOrbPrefab = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "EnemyEvents/Bracken/BrackenOrb.prefab"));
            if (_brackenOrbPrefab != null)
                _brackenOrbPrefab.AddComponent<BrackenOrbBehaviour>();
        }

        public class BrackenOrbBehaviour : NetworkBehaviour
        {
            float damageFrameCounter = 0;
            NetworkVariable<float> radius = new NetworkVariable<float>(0f);

            List<ulong> fearedPlayers = new List<ulong>();

            void Awake()
            {
                Plugin.Log("BrackenOrb has awaken!");
                transform.localScale = Vector3.zero;

                foreach (var player in PlayerInfo.AllPlayers)
                {
                    var distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);
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
                        var distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);
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
                    Plugin.Log("Radius: " + radius.Value + " / LocalScale: " + transform.localScale);
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
