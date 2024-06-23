using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static LethalLevelLoader.ExtendedEvent;

namespace LittleCompany.patches.enemy_behaviours
{
    [HarmonyPatch]
    internal class ForestGiantAIPatch
    {
        internal static int fleeStateIndex = 0;
        internal static void AddFleeState()
        {
            var forestGiant = EnemyInfo.EnemyTypeByEnum(EnemyInfo.Enemy.ForestGiant);
            if(forestGiant?.enemyPrefab != null && forestGiant.enemyPrefab.TryGetComponent(out ForestGiantAI forestGiantAI))
            {
                EnemyBehaviourState fleeState = new EnemyBehaviourState();
                fleeState.name = "Flee";

                forestGiantAI.enemyBehaviourStates = new List<EnemyBehaviourState>(forestGiantAI.enemyBehaviourStates) { fleeState }.ToArray();
                fleeStateIndex = forestGiantAI.enemyBehaviourStates.Length - 1;
            }
        }

        [HarmonyPatch(typeof(ForestGiantAI), "Update")]
        [HarmonyPostfix]
        public static void Update(ForestGiantAI __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || __instance.isEnemyDead)
                return;

            if (__instance.chasingPlayer != null && __instance.currentBehaviourStateIndex != fleeStateIndex)
            {
                var sizeDiff = EnemyModification.ScalingOf(__instance).RelativeScale - PlayerInfo.SizeOf(__instance.targetPlayer);
                if (sizeDiff >= 0.5f)
                {
                    Plugin.Log("Starting flee mode!");
                    // Starting to flee
                    __instance.SwitchToBehaviourState(fleeStateIndex);
                    __instance.noticePlayerTimer = 1f;
                }
            }

            if (__instance.currentBehaviourStateIndex != fleeStateIndex) return;

            __instance.agent.speed = 8f;
            __instance.reachForPlayerRig.weight = Mathf.Lerp(__instance.reachForPlayerRig.weight, 0f, Time.deltaTime * 15f); // Hands down
            //__instance.reachForPlayerTarget.position = __instance.transform.position + Vector3.up * 5f;
            //__instance.reachForPlayerRig.weight = -0.9f;
            //__instance.creatureAnimator.SetFloat("VelocityX", -2f);
            //__instance.creatureAnimator.SetFloat("VelocityY", 20f);

            __instance.noticePlayerTimer++;
            __instance.noticePlayerTimer %= 200;
            if (__instance.noticePlayerTimer % 200 != 1) return;

            // Flee behaviour
            var distanceTowardsChasingPlayer = Vector3.Distance(__instance.transform.position, __instance.chasingPlayer.transform.position);

            PlayerControllerB closestSmallPlayer = null;
            float distanceToClosestSmallPlayer = 0f;

            PlayerControllerB[] allPlayersInLineOfSight = __instance.GetAllPlayersInLineOfSight(50f, 70, __instance.eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);
            if(allPlayersInLineOfSight != null && allPlayersInLineOfSight.Length > 0)
            {
                foreach (var player in allPlayersInLineOfSight)
                {
                    var distanceToPlayer = Vector3.Distance(__instance.transform.position, player.transform.position);

                    var sizeDiff = EnemyModification.ScalingOf(__instance).RelativeScale - PlayerInfo.SizeOf(__instance.targetPlayer);
                    if (sizeDiff >= 0.5f)
                    {
                        if (closestSmallPlayer == null || distanceToPlayer < distanceToClosestSmallPlayer)
                        {
                            closestSmallPlayer = player;
                            distanceToClosestSmallPlayer = distanceToPlayer;
                        }
                    }
                }
            }

            if(closestSmallPlayer == null && distanceTowardsChasingPlayer > 30f)
            {
                Plugin.Log("Lost all players in chase.");
                __instance.lostPlayerInChase = true;
                __instance.chasingPlayer = null;
                __instance.SwitchToBehaviourState(0); // End fleeing
                return;
            }

            if (closestSmallPlayer != null && distanceToClosestSmallPlayer < distanceTowardsChasingPlayer)
            {
                __instance.chasingPlayer = closestSmallPlayer;
                Plugin.Log("Moving away from seen player.");
            }

            Plugin.Log("Set fleeing point.");
            Vector3 wayTowardsPlayer = __instance.transform.position - __instance.chasingPlayer.transform.position;
            var oppositeDirectionFromPlayer = __instance.transform.position + wayTowardsPlayer;
            var navMeshPos = RoundManager.Instance.GetNavMeshPosition(oppositeDirectionFromPlayer);
            var pos = RoundManager.Instance.GotNavMeshPositionResult ? navMeshPos : oppositeDirectionFromPlayer;
            __instance.SetDestinationToPosition(pos);

            __instance.lookTarget.position = pos;
            __instance.LookAtTarget();
        }
    }
}
