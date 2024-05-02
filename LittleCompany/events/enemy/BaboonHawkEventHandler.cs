using GameNetcodeStuff;
using LittleCompany.helper;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;
using static LittleCompany.helper.EnemyInfo;

namespace LittleCompany.events.enemy
{
    internal class BaboonHawkEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Baboon hawk shrunken to death");

            if (PlayerInfo.IsHost)
                MakeOtherBaboonHawksAngryAt(playerShrunkenBy);

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        public void MakeOtherBaboonHawksAngryAt(PlayerControllerB player)
        {
            //var playerThreat = MakePlayerAThreat(player);

            var baboonHawkName = EnemyNameOf(Enemy.BaboonHawk);
            foreach (var enemyAI in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemyAI.enemyType.enemyName != baboonHawkName) continue;

                Plugin.Log("Found a baboon hawk");

                var baboonHawk = enemy as BaboonBirdAI;
                baboonHawk.SetAggressiveModeClientRpc(2);
                baboonHawk.StartFocusOnThreatClientRpc(player.NetworkObject);
            }
        }

        /*public Threat MakePlayerAThreat(PlayerControllerB player)
        {
            var baboonHawk = enemy as BaboonBirdAI;
            if (baboonHawk.threats.TryGetValue(player.transform, out Threat threat))
            { // Already a threat
                threat.threatLevel = 0;
                threat.interestLevel = 99;
                threat.hasAttacked = true;
                Plugin.Log("Made player a higher target");
                return threat;
            }

            threat = new Threat();
            if (player.TryGetComponent<IVisibleThreat>(out var visibleThreat))
            {
                threat.type = visibleThreat.type;
                threat.threatScript = visibleThreat;
            }
            threat.timeLastSeen = Time.realtimeSinceStartup;
            threat.lastSeenPosition = player.transform.position + Vector3.up * 0.5f;
            threat.distanceToThreat = Vector3.Distance(player.transform.position, enemy.transform.position);
            threat.distanceMovedTowardsBaboon = 0f;
            threat.threatLevel = 0;
            threat.interestLevel = 99;
            threat.hasAttacked = true;
            if (baboonHawk.threats.TryAdd(player.transform, threat))
                Plugin.Log("Added player as threat");
            return threat;
        }*/
    }
}
