using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleCompany.patches.enemy_behaviours
{
    [HarmonyPatch]
    internal class ForestGiantAIPatch
    {
        internal static int fleeStateIndex = 0;
        internal static int fleeCoopStateIndex = 0;
        internal static int fleeAttackStateIndex = 0;

        internal static Dictionary<ForestGiantAI, List<PlayerControllerB>> UntargetablePlayers = new Dictionary<ForestGiantAI, List<PlayerControllerB>>();

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void EndOfGame() => UntargetablePlayers.Clear();

        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, EnemyAI __instance)
        {
            if (!__result || UntargetablePlayers.Count == 0)
                return __result;

            var forestGiant = __instance as ForestGiantAI;
            if (forestGiant == null)
                return __result;

            if (UntargetablePlayers.ContainsKey(forestGiant) && UntargetablePlayers[forestGiant].Contains(playerScript))
                return false;

            return __result;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        [HarmonyPriority(Priority.Last)]
        public static void AddFleeState()
        {
            var forestGiant = EnemyInfo.EnemyTypeByEnum(EnemyInfo.Enemy.ForestGiant);
            if (forestGiant?.enemyPrefab != null && forestGiant.enemyPrefab.TryGetComponent(out ForestGiantAI forestGiantAI))
            {
                forestGiantAI.enemyBehaviourStates = new List<EnemyBehaviourState>(forestGiantAI.enemyBehaviourStates)
                {
                    new EnemyBehaviourState() { name = "Flee" },
                    new EnemyBehaviourState() { name = "FleeCoop" },
                    new EnemyBehaviourState() { name = "FleeAttack" }
                }.ToArray();

                fleeStateIndex = forestGiantAI.enemyBehaviourStates.Length - 3;
                fleeCoopStateIndex = forestGiantAI.enemyBehaviourStates.Length - 2;
                fleeAttackStateIndex = forestGiantAI.enemyBehaviourStates.Length - 1;

                Plugin.Log("Added ForestGiant flee state behaviours.");
            }
        }

        [HarmonyPatch(typeof(ForestGiantAI), "Update")]
        [HarmonyPrefix]
        public static bool Update(ForestGiantAI __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || __instance.isEnemyDead)
                return true;

            if (__instance.targetPlayer != null && (__instance.currentBehaviourStateIndex < fleeStateIndex || __instance.currentBehaviourStateIndex > fleeAttackStateIndex))
            {
                var sizeDiff = EnemyModification.ScalingOf(__instance).RelativeScale - PlayerInfo.SizeOf(__instance.targetPlayer);
                if (sizeDiff >= 0.5f)
                {
                    Plugin.Log("Starting flee mode!");
                    // Starting to flee
                    if (__instance.roamPlanet.inProgress)
                        __instance.StopSearch(__instance.roamPlanet, clear: false);

                    if (__instance.searchForPlayers.inProgress)
                        __instance.StopSearch(__instance.searchForPlayers);

                    __instance.agent.speed = 0f;
                    __instance.SwitchToBehaviourState(fleeStateIndex);
                    __instance.noticePlayerTimer = 1f;
                }
            }

            if (__instance.currentBehaviourStateIndex == fleeStateIndex)
                return FleeBehaviour(__instance);
            else if (__instance.currentBehaviourStateIndex == fleeCoopStateIndex)
            {
                FleeCoopBehaviour(__instance);
                return false;
            }
            else if (__instance.currentBehaviourStateIndex == fleeAttackStateIndex)
                return FleeAttackBehaviour(__instance);

            return true;
        }

        #region Flee
        private static bool FleeBehaviour(ForestGiantAI forestGiant)
        {
            forestGiant.agent.speed = 8f;
            forestGiant.reachForPlayerRig.weight = Mathf.Lerp(forestGiant.reachForPlayerRig.weight, 0f, Time.deltaTime * 15f); // Hands down

            forestGiant.noticePlayerTimer++;
            forestGiant.noticePlayerTimer %= 200;
            if (forestGiant.noticePlayerTimer % 200 != 1) return true;

            // Check for coop
            ForestGiantAI nearestGiant = FindNearestGiantFor(forestGiant);
            if (nearestGiant != null)
            {
                InitiateFleeCoop(forestGiant, nearestGiant);
                return false;
            }

            // Flee behaviour
            var hasLOSToTarget = forestGiant.targetPlayer.HasLineOfSightToPosition(forestGiant.transform.position);
            var distanceFromTargetPlayer = Vector3.Distance(forestGiant.transform.position, forestGiant.targetPlayer.transform.position);

            PlayerControllerB closestSmallPlayer = FindNearestSmallerPlayerInLOSFor(forestGiant, out float distanceToNearestSmallPlayer);
            if (closestSmallPlayer == null && distanceFromTargetPlayer > 30f)
            {
                Plugin.Log("Lost all players in chase.");

                forestGiant.StartCoroutine(MakePlayerUntargetableFor(forestGiant, forestGiant.targetPlayer, 5f));
                forestGiant.targetPlayer = null;
                forestGiant.SwitchToBehaviourState(0); // End fleeing
                return false;
            }

            if (closestSmallPlayer != null && distanceToNearestSmallPlayer < distanceFromTargetPlayer)
            {
                forestGiant.targetPlayer = closestSmallPlayer;
                Plugin.Log("Moving away from seen player.");
            }

            Plugin.Log("Set fleeing point.");
            Vector3 wayTowardsPlayer = forestGiant.transform.position - forestGiant.targetPlayer.transform.position;
            var oppositeDirectionFromPlayer = forestGiant.transform.position + wayTowardsPlayer;
            var navMeshPos = RoundManager.Instance.GetNavMeshPosition(oppositeDirectionFromPlayer);
            var pos = RoundManager.Instance.GotNavMeshPositionResult ? navMeshPos : oppositeDirectionFromPlayer;
            forestGiant.SetDestinationToPosition(pos);

            if (Vector3.Distance(forestGiant.transform.position, pos) < 0.5f)
            {
                // Not/barely moving
                forestGiant.stopAndLookTimer += Time.deltaTime;
                if (forestGiant.stopAndLookTimer > 2f && !forestGiant.lookingAtTarget)
                {
                    forestGiant.lookingAtTarget = true;
                    forestGiant.lookTarget.position = pos;
                    forestGiant.creatureVoice.PlayOneShot(forestGiant.giantCry);
                }
                if (forestGiant.stopAndLookTimer > 8f && hasLOSToTarget)
                {
                    // Already stuck for 3 seconds -> Attack
                    forestGiant.SwitchToBehaviourState(fleeAttackStateIndex);
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.7f);
                }
                forestGiant.lookTarget.position = forestGiant.targetPlayer.transform.position + Vector3.up;
            }
            else
            {
                forestGiant.stopAndLookTimer = 0f;
                forestGiant.lookingAtTarget = false;
                forestGiant.lookTarget.position = pos;
            }

            forestGiant.LookAtTarget();

            return true;
        }

        private static IEnumerator MakePlayerUntargetableFor(ForestGiantAI forestGiant, PlayerControllerB player, float duration = 0f)
        {
            if (!UntargetablePlayers.ContainsKey(forestGiant))
                UntargetablePlayers.Add(forestGiant, new List<PlayerControllerB> { forestGiant.targetPlayer });
            else
                UntargetablePlayers[forestGiant].Add(forestGiant.targetPlayer);

            if(duration > 0f)
            {
                yield return new WaitForSeconds(duration);
                if(UntargetablePlayers.ContainsKey(forestGiant))
                    UntargetablePlayers[forestGiant].Remove(player);
            }
        }

        private static ForestGiantAI FindNearestGiantFor(ForestGiantAI forestGiant)
        {
            ForestGiantAI nearestGiant = null;
            float distanceToNearestGiant = 0f;
            var hits = Physics.SphereCastAll(forestGiant.transform.position, 10f, forestGiant.transform.forward, 5f, LayerMasks.ToInt([LayerMasks.Mask.Enemies]));
            if (hits == null) return null;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (nearestGiant == null || hit.distance < distanceToNearestGiant)
                {
                    var otherGiant = hit.collider.GetComponentInParent<ForestGiantAI>();
                    if (otherGiant != null && otherGiant != forestGiant && otherGiant != nearestGiant)
                    {
                        Plugin.Log("Found other enemy while fleeing: " + hit.collider.name);
                        nearestGiant = otherGiant;
                        distanceToNearestGiant = hit.distance;
                    }
                }
            }

            return nearestGiant;
        }

        private static PlayerControllerB FindNearestSmallerPlayerInLOSFor(ForestGiantAI forestGiant, out float distance)
        {
            PlayerControllerB closestSmallPlayer = null;
            distance = 0f;

            PlayerControllerB[] allPlayersInLineOfSight = forestGiant.GetAllPlayersInLineOfSight(50f, 70, forestGiant.eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);
            if (allPlayersInLineOfSight != null && allPlayersInLineOfSight.Length > 0)
            {
                foreach (var player in allPlayersInLineOfSight)
                {
                    var distanceToPlayer = Vector3.Distance(forestGiant.transform.position, player.transform.position);

                    var sizeDiff = EnemyModification.ScalingOf(forestGiant).RelativeScale - PlayerInfo.SizeOf(forestGiant.targetPlayer);
                    if (sizeDiff >= 0.5f)
                    {
                        if (closestSmallPlayer == null || distanceToPlayer < distance)
                        {
                            closestSmallPlayer = player;
                            distance = distanceToPlayer;
                        }
                    }
                }
            }

            return closestSmallPlayer;
        }
        #endregion

        #region Flee-Coop
        private static void InitiateFleeCoop(ForestGiantAI forestGiant, ForestGiantAI otherGiant)
        {
            Plugin.Log("Found other giant while fleeing.");
            forestGiant.noticePlayerTimer = 0f;

            forestGiant.lookTarget = otherGiant.transform;
            forestGiant.LookAtTarget();
            forestGiant.stopAndLookTimer = 0f;
            forestGiant.SwitchToBehaviourState(fleeCoopStateIndex);

            otherGiant.lookTarget = forestGiant.transform;
            otherGiant.LookAtTarget();
            otherGiant.stopAndLookTimer = 0f;
            otherGiant.SwitchToBehaviourState(fleeCoopStateIndex);
        }

        private static void FleeCoopBehaviour(ForestGiantAI forestGiant)
        {
            forestGiant.agent.speed = 0f;
            forestGiant.stopAndLookTimer += Time.deltaTime;
            if (forestGiant.stopAndLookTimer < Mathf.PI) return;

            if (forestGiant.stopAndLookTimer > Mathf.PI * 2f)
            {
                Plugin.Log("Attack together with other giant!");
                forestGiant.SwitchToBehaviourState(fleeAttackStateIndex);
                return;
            }

            forestGiant.reachForPlayerRig.weight = 0.9f * Mathf.Sin(forestGiant.stopAndLookTimer - Mathf.PI);
        }
        #endregion

        #region Flee-Attack
        private static bool FleeAttackBehaviour(ForestGiantAI forestGiant)
        {
            if(forestGiant.targetPlayer == null || forestGiant.targetPlayer.isPlayerDead || !forestGiant.targetPlayer.isPlayerControlled)
            {
                Plugin.Log("Lost player or they died!");
                forestGiant.targetPlayer = null;
                forestGiant.SwitchToBehaviourState(0);
                return false;
            }

            forestGiant.agent.speed = 8f;
            if(!forestGiant.inEatingPlayerAnimation)
                forestGiant.movingTowardsTargetPlayer = true;

            return true;
        }
        #endregion
    }
}
