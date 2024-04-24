using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.modifications;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class CoilheadEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Coilhead shrunken to death");
            if (PlayerInfo.IsHost)
            {
                var alivePlayers = PlayerInfo.AlivePlayers;
                var startPos = Random.Range(0, alivePlayers.Count - 1); // Start with a random player, so the coilhead won't always spawn on the same player

                for(int i = startPos; i < alivePlayers.Count + startPos; i++)
                {
                    var player = alivePlayers[i % alivePlayers.Count];
                    if (!player.isInsideFactory || player.playerClientId == playerShrunkenBy.playerClientId)
                        continue;

                    var playerPos = player.gameplayCamera.transform.position;
                    var playerCameraDirection = player.gameplayCamera.transform.forward;
                    playerCameraDirection.y = 1f;

                    var spawnPosition = playerPos + playerCameraDirection * 3f;
                    if (Physics.Raycast(new Ray(playerPos, playerCameraDirection), out RaycastHit rayHit, 3f, StartOfRound.Instance.collidersAndRoomMask))
                        spawnPosition = rayHit.point;

                    TeleportAndScaleCoilheadToServerRpc(spawnPosition, previousSize, playerShrunkenBy.playerClientId);
                    return;
                }

                TeleportAndScaleCoilheadToServerRpc(enemy.transform.position, previousSize, playerShrunkenBy.playerClientId);
            }

            //base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportAndScaleCoilheadToServerRpc(Vector3 position, float scale, ulong playerModifiedByID)
        {
            TeleportAndScaleCoilheadToClientRpc(position, scale, playerModifiedByID);
        }

        [ClientRpc]
        public void TeleportAndScaleCoilheadToClientRpc(Vector3 position, float scale, ulong playerModifiedByID)
        {
            Plugin.Log("TeleportAndScaleCoilheadToClientRpc -> " + scale);
            enemy.transform.position = position;

            var scaling = EnemyModification.ScalingOf(enemy);
            //scaling.StopScaling();
            scaling.ScaleOverTimeTo(scale, PlayerInfo.ControllerFromID(playerModifiedByID));
        }
    }
}
