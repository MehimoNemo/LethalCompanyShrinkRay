using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.Config;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

namespace LittleCompany.patches.enemy_behaviours
{
    [HarmonyPatch]
    internal class ForestGiantAIPatch
    {
        internal static int fleeStateIndex = 0;
        internal static int fleeCoopStateIndex = 0;

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
                    new EnemyBehaviourState() { name = "FleeCoop" }
                }.ToArray();

                fleeStateIndex = forestGiantAI.enemyBehaviourStates.Length - 2;
                fleeCoopStateIndex = forestGiantAI.enemyBehaviourStates.Length - 1;

                Plugin.Log("Added ForestGiant flee state behaviours.");
            }
        }

        /*[HarmonyPatch(typeof(ForestGiantAI), "GiantSeePlayerEffect")]
        [HarmonyPrefix] // Disable fear increase when giant would run away?
        public static bool GiantSeePlayerEffect(ForestGiantAI __instance) => (EnemyModification.ScalingOf(__instance).RelativeScale - PlayerInfo.CurrentPlayerScale) < 0.5f;*/
        

        [HarmonyPatch(typeof(ForestGiantAI), "BeginChasingNewPlayerClientRpc")]
        [HarmonyPrefix]
        public static bool BeginChasingNewPlayerClientRpc(ForestGiantAI __instance, int playerId)
        {
            var targetPlayer = PlayerInfo.ControllerFromID((ulong)playerId);
            if (targetPlayer == null) return true;

            var sizeDiff = EnemyModification.ScalingOf(__instance).RelativeScale - PlayerInfo.SizeOf(targetPlayer);
            if (sizeDiff >= 0.5f)
            {
                Plugin.Log("Starting flee mode!");
                // Starting to flee
                if (__instance.roamPlanet.inProgress)
                    __instance.StopSearch(__instance.roamPlanet, clear: false);

                if (__instance.searchForPlayers.inProgress)
                    __instance.StopSearch(__instance.searchForPlayers);

                __instance.targetPlayer = targetPlayer;
                __instance.agent.speed = 8f;
                __instance.reachForPlayerRig.weight = Mathf.Lerp(__instance.reachForPlayerRig.weight, 0f, Time.deltaTime * 15f); // Hands down

                __instance.SwitchToBehaviourClientRpc(fleeStateIndex);
                __instance.noticePlayerTimer = 1f;
                return false;
            }

            return true;
        }
        

        [HarmonyPatch(typeof(ForestGiantAI), "Update")]
        [HarmonyPrefix]
        public static bool Update(ForestGiantAI __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || __instance.isEnemyDead)
                return true;

            if (__instance.currentBehaviourStateIndex == fleeStateIndex)
                return FleeBehaviour(__instance);
            else if (__instance.currentBehaviourStateIndex == fleeCoopStateIndex)
            {
                FleeCoopBehaviour(__instance);
                return false;
            }

            return true;
        }

        #region Flee
        private static bool FleeBehaviour(ForestGiantAI forestGiant)
        {
            Plugin.Log("magnitude: " + forestGiant.agentLocalVelocity.sqrMagnitude);
            if (forestGiant.agent.speed > 0f && forestGiant.agentLocalVelocity.sqrMagnitude < 0.5f) // Not moving or barely. Plain 0 is spawning speed
                forestGiant.stopAndLookTimer += Time.deltaTime;
            else
                forestGiant.stopAndLookTimer = 0f;

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
            var hasLOSToTarget = forestGiant.CheckLineOfSightForPosition(forestGiant.targetPlayer.transform.position, 80f);
            if (hasLOSToTarget)
                forestGiant.timeSinceChangingTarget = 0f;

            var distanceFromTargetPlayer = Vector3.Distance(forestGiant.transform.position, forestGiant.targetPlayer.transform.position);

            PlayerControllerB closestSmallPlayer = FindNearestSmallerPlayerInLOSFor(forestGiant, out float distanceToNearestSmallPlayer);
            if (closestSmallPlayer == null && distanceFromTargetPlayer > 50f || forestGiant.timeSinceChangingTarget > 3f) // over 50 away or not seen for 3s
            {
                Plugin.Log("Lost all players in chase.");

                forestGiant.StartCoroutine(MakePlayerUntargetableFor(forestGiant, forestGiant.targetPlayer, 5f));
                forestGiant.targetPlayer = null;
                forestGiant.SwitchToBehaviourState(0); // End fleeing
                return false;
            }

            if (closestSmallPlayer != null && distanceToNearestSmallPlayer < distanceFromTargetPlayer)
            {
                forestGiant.StartCoroutine(MakePlayerUntargetableFor(forestGiant, forestGiant.targetPlayer, 2f));
                forestGiant.targetPlayer = closestSmallPlayer;
                Plugin.Log("Moving away from newly seen player.");
            }

            // Attack after getting stuck
            if (forestGiant.stopAndLookTimer > 10f && hasLOSToTarget)
            {
                Plugin.Log("Already stuck for 10 seconds -> Attack");
                forestGiant.chasingPlayer = forestGiant.targetPlayer;
                forestGiant.agent.speed = 8f;
                forestGiant.SwitchToBehaviourState(1);
                return false;
            }

            // Movement & Looking angle
            if (forestGiant.stopAndLookTimer < 3f)
            {
                forestGiant.lookingAtTarget = false;

                var targetPlayerVector = forestGiant.transform.position - forestGiant.targetPlayer.transform.position;
                var oppositeDirectionFromPlayer = forestGiant.transform.position + targetPlayerVector;
                forestGiant.SetDestinationToPosition(oppositeDirectionFromPlayer);
            }
            else
            {
                if (!forestGiant.lookingAtTarget)
                    forestGiant.creatureVoice.PlayOneShot(forestGiant.giantCry);
                forestGiant.lookingAtTarget = true;

                forestGiant.SetDestinationToPosition(forestGiant.transform.position);
            }
            forestGiant.lookTarget.position = forestGiant.destination;

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

            forestGiant.agent.speed = 0f;
            forestGiant.lookTarget = otherGiant.transform;
            forestGiant.LookAtTarget();
            forestGiant.stopAndLookTimer = 0f;
            forestGiant.SwitchToBehaviourState(fleeCoopStateIndex);

            otherGiant.targetPlayer = forestGiant.targetPlayer;
            otherGiant.agent.speed = 0f;
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
                //InitiateFleeAttack(forestGiant);
                forestGiant.chasingPlayer = forestGiant.targetPlayer;
                forestGiant.agent.speed = 8f;
                forestGiant.SwitchToBehaviourState(1);
                return;
            }

            forestGiant.reachForPlayerRig.weight = 0.9f * Mathf.Sin(forestGiant.stopAndLookTimer - Mathf.PI);
        }
        #endregion
    }
}
