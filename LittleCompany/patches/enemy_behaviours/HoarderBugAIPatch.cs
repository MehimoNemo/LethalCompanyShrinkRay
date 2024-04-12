using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using UnityEngine;
using static LittleCompany.events.enemy.HoarderBugEventHandler;
using static LittleCompany.helper.EnemyInfo.HoarderBug;

namespace LittleCompany.patches.EnemyBehaviours
{
    [HarmonyPatch]
    internal class HoarderBugAIPatch
    {
        private static readonly float maxAllowedNestDistance = 10f;

        [HarmonyPatch(typeof(EnemyAI), "PlayerIsTargetable")]
        [HarmonyPostfix]
        public static bool PlayerIsTargetable(bool __result, PlayerControllerB playerScript, EnemyAI __instance)
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.NoGrab || !(__instance is HoarderBugAI))
                return __result;

            if (IsDieing(__instance)) // about to die.. don't see players as threat
                return false;

            return !PlayerInfo.SmallerThan(playerScript, EnemyInfo.SizeOf(__instance));
        }

        [HarmonyPatch(typeof(HoarderBugAI), "RefreshGrabbableObjectsInMapList")]
        [HarmonyPostfix]
        public static void RefreshGrabbableObjectsInMapList()
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour != ModConfig.HoardingBugBehaviour.NoGrab)
            {
                HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject gpo) && (gpo.InLastHoardingBugNestRange.Value || !gpo.grabbableToEnemies)); // remove still grabbed ones
                return;
            }

            HoarderBugAI.grabbableObjectsInMap.RemoveAll(go => go.TryGetComponent(out GrabbablePlayerObject _));
        }

        [HarmonyPatch(typeof(HoarderBugAI), "DetectNoise")]
        [HarmonyPostfix]
        public static void DetectNoise(HoarderBugAI __instance, ref float ___timeSinceLookingTowardsNoise)
        {
            if (ModConfig.Instance.values.hoardingBugBehaviour != ModConfig.HoardingBugBehaviour.Addicted || !PlayerInfo.IsCurrentPlayerShrunk) return; // Not targetable

            if (__instance.heldItem != null && __instance.heldItem.itemGrabbableObject != null && __instance.heldItem.itemGrabbableObject as GrabbablePlayerObject != null)
                return; // Chill, you already got one..

            if (__instance.targetItem != null)
            {
                Plugin.Log("DetectNoise. Target item: " + __instance.targetItem.name + " with position: " + __instance.targetItem.transform.position);
                if (__instance.targetItem is GrabbablePlayerObject)
                {
                    ___timeSinceLookingTowardsNoise = 0f; // Set this to avoid switching to behaviour 1 (which is "return to nest")
                    Plugin.Log("targetItem: player position -> " + (__instance.targetItem as GrabbablePlayerObject).grabbedPlayer.transform.position);
                    return; // Already targeting a player
                }
            }


            var inLineOfSight = PlayerInfo.CurrentPlayer.HasLineOfSightToPosition(__instance.transform.position + Vector3.up * (PlayerInfo.CurrentPlayerScale - 1f + 0.75f)); // HoarderBugAI.Update() case 2
            if (!inLineOfSight) return;

            if (PlayerInfo.CurrentPlayerID.HasValue && GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID.Value, out GrabbablePlayerObject gpo))
            {
                if (!IsGrabbablePlayerTargetable(gpo, __instance))
                {
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " not targetable.");
                    return;
                }

                Plugin.Log("Forget everything else.. we found a grabbable player! Let's goooo..!");
                gpo.HoardingBugTargetUsServerRpc(__instance.NetworkObjectId);
                ___timeSinceLookingTowardsNoise = 0f; // Set this to avoid switching to behaviour 1 (which is "return to nest")
            }
        }

        public static bool IsDieing(EnemyAI hoarderBug) => hoarderBug.GetComponent<DieingBugBehaviour> != null;

        public static bool IsGrabbablePlayerTargetable(GrabbablePlayerObject gpo, HoarderBugAI hoarderBug)
        {
            if (IsDieing(hoarderBug))
                return true;

            if (gpo.InLastHoardingBugNestRange.Value)
                return false;

            return PlayerInfo.SmallerThan(gpo.grabbedPlayer, EnemyInfo.SizeOf(hoarderBug));
        }

        public static void AddToGrabbables(GrabbablePlayerObject gpo)
        {
            if (!HoarderBugAI.grabbableObjectsInMap.Contains(gpo.gameObject))
                HoarderBugAI.grabbableObjectsInMap.Add(gpo.gameObject);
            HoarderBugAI.HoarderBugItems.RemoveAll(item => item.itemGrabbableObject.NetworkObjectId == gpo.NetworkObjectId);
        }

        public static void DropHeldItem(HoarderBugAI hoarderBug, bool switchToSearchState = true)
        {
            if (hoarderBug?.heldItem?.itemGrabbableObject == null) return;

            hoarderBug.DropItemServerRpc(hoarderBug.heldItem.itemGrabbableObject.NetworkObject, hoarderBug.transform.position, false);

            if(switchToSearchState)
            {
                hoarderBug.heldItem = null;
                hoarderBug.targetItem = null;
                hoarderBug.SwitchToBehaviourState((int)BehaviourState.Searching);
            }
        }

        public static void HoardingBugTargetUs(HoarderBugAI hoarderBug, GrabbablePlayerObject gpo)
        {
            if (gpo.playerHeldBy != null && ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.Addicted)
            {
                Plugin.Log("Oh you better drop that beautiful player, my friend!");
                hoarderBug.targetItem = gpo;
                hoarderBug.StopSearch(hoarderBug.searchForItems, clear: false);
                gpo.lastHoarderBugGrabbedBy.angryAtPlayer = gpo.playerHeldBy;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState((int)BehaviourState.Chase);
                return;
            }

            DropHeldItem(hoarderBug, false);
            hoarderBug.targetItem = gpo;
            hoarderBug.StopSearch(hoarderBug.searchForItems, clear: false);
            hoarderBug.SwitchToBehaviourState((int)BehaviourState.Searching);
        }

        public static void HoarderBugEscapeRoutineForGrabbablePlayer(GrabbablePlayerObject gpo)
        {
            if (gpo.lastHoarderBugGrabbedBy.isEnemyDead || gpo.lastHoarderBugGrabbedBy.nestPosition == null)
            {
                gpo.MovedOutOfHoardingBugNestRangeServerRpc(true);
                return;
            }

            var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, gpo.grabbedPlayer.transform.position);
            //Plugin.Log("Difference between nest pos (" + gpo.lastHoarderBugGrabbedBy.nestPosition + ") and current pos (" + PlayerInfo.CurrentPlayer.transform.position + ") is " + distanceToNest);
            if (distanceToNest < maxAllowedNestDistance)
                return;

            Plugin.Log("Overly attached hoarding bug feels that the grabbed player is too far away. Following!");
            gpo.MovedOutOfHoardingBugNestRangeServerRpc(false);
        }

        public static void MovedOutOfHoardingBugNestRange(GrabbablePlayerObject gpo)
        {
            bool shouldDropItems = false;
            if (gpo.playerHeldBy != null)
            {
                foreach (var hoarderBugItem in HoarderBugAI.HoarderBugItems)
                {
                    if (hoarderBugItem.itemGrabbableObject != null && hoarderBugItem.itemGrabbableObject.name == gpo.name)
                        hoarderBugItem.status = HoarderBugItemStatus.Stolen;
                }
                gpo.lastHoarderBugGrabbedBy.targetItem = gpo;
                gpo.lastHoarderBugGrabbedBy.StopSearch(gpo.lastHoarderBugGrabbedBy.searchForItems, clear: false);
                gpo.lastHoarderBugGrabbedBy.angryAtPlayer = gpo.playerHeldBy;
                gpo.lastHoarderBugGrabbedBy.SwitchToBehaviourState((int)BehaviourState.Chase);
                shouldDropItems = true;
                Plugin.Log("HoarderBug saw that " + gpo.playerHeldBy.name + " stole " + gpo.name + ". Is angry now at them!");
            }
            else
            {
                var distanceToNest = Vector3.Distance(gpo.lastHoarderBugGrabbedBy.nestPosition, gpo.grabbedPlayer.transform.position);
                if (distanceToNest < (maxAllowedNestDistance + 5f))
                {
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " moved too far away from hoarder bug nest.");
                    if (ModConfig.Instance.values.hoardingBugBehaviour == ModConfig.HoardingBugBehaviour.Addicted)
                    {
                        Plugin.Log(" Bug tries to get them back!");
                        HoardingBugTargetUs(gpo.lastHoarderBugGrabbedBy, gpo);
                        shouldDropItems = true;
                    }
                }
                else
                    Plugin.Log("Player " + gpo.grabbedPlayerID.Value + " escaped from the hoarding bug!"); // Very likely we teleported (by using vent, shipTeleporter, ..)
            }

            if (shouldDropItems && gpo.lastHoarderBugGrabbedBy.heldItem != null && gpo.lastHoarderBugGrabbedBy.heldItem.itemGrabbableObject != null)
                gpo.lastHoarderBugGrabbedBy.DropItemServerRpc(gpo.lastHoarderBugGrabbedBy.heldItem.itemGrabbableObject.NetworkObject, gpo.lastHoarderBugGrabbedBy.transform.position, false);
        }

        [HarmonyPatch(typeof(HoarderBugAI), "SetGoTowardsTargetObject")]
        [HarmonyPrefix]
        public static void SetGoTowardsTargetObjectPrefix(ref GameObject foundObject, HoarderBugAI __instance)
        {
            if (__instance.heldItem != null && __instance.heldItem.itemGrabbableObject != null && __instance.heldItem.itemGrabbableObject as GrabbablePlayerObject != null)
            {
                Plugin.Log("Ayy?! Still holding a player, you forgot?! Nononono..");
                foundObject = __instance.heldItem.itemGrabbableObject.gameObject; // Hopefully this won't break stuff..
                __instance.SwitchToBehaviourState((int)BehaviourState.Nest);
            }

            if (!foundObject.TryGetComponent(out GrabbablePlayerObject gpo))
                return;

            if (!IsGrabbablePlayerTargetable(gpo, __instance))
            {
                Plugin.Log("SetGoTowardsTargetObject: Player not targetable for hoarding bug");
                return;
            }
        }

        // ------------------ DEBUG FROM HERE ON ------------------
#if DEBUG
        // ChatCommands mod line -> /spawnenemy Hoarding bug a=1 p=@me

        private static Vector3 hoarderBugNestPosition;
        [HarmonyPatch(typeof(HoarderBugAI), "SyncNestPositionClientRpc")]
        [HarmonyPostfix]
        public static void SyncNestPositionClientRpc(Vector3 newNestPosition)
        {
            hoarderBugNestPosition = newNestPosition;
        }

        [HarmonyPatch(typeof(HoarderBugAI), "DoAIInterval")]
        [HarmonyPostfix]
        public static void DoAIInterval(HoarderBugAI __instance)
        {
            //Plugin.Log("behaviourState: " + __instance.currentBehaviourStateIndex + " | targetItem: " + __instance.targetItem);
        }

        public static void SetUsAsStolen()
        {
            if (!PlayerInfo.CurrentPlayerID.HasValue || !GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID.Value, out GrabbablePlayerObject gpo))
                return;

            Plugin.Log("Added as stolen hoarding bug item");
            HoarderBugAI.HoarderBugItems.Add(new HoarderBugItem(gpo, HoarderBugItemStatus.Stolen, gpo.transform.position));
        }

        public static void TeleportToLatestNest()
        {
            if (hoarderBugNestPosition != Vector3.zero)
            {
                Plugin.Log("Teleporting to latest hoarder bug nest position.");
                PlayerInfo.CurrentPlayer.TeleportPlayer(hoarderBugNestPosition);
            }
            else
                Plugin.Log("No hoarder bug nest yet..");
        }

        public static string HoarderBugItemsLog
        {
            get
            {
                if (HoarderBugAI.HoarderBugItems == null)
                    return "No hoarder bug items.";

                var output = "Grabbable hoarder bug items:\n";
                output += "------------------------------\n";
                foreach (var item in HoarderBugAI.HoarderBugItems)
                    output += item.itemGrabbableObject.name + ": " + item.status.ToString() + "\n";
                output += "------------------------------\n";
                return output;
            }
        }

        public static string GrabbableObjectsLog
        {
            get
            {
                if (HoarderBugAI.grabbableObjectsInMap == null)
                    return "No grabbable hoarder bug objects.";

                var output = "Grabbable hoarder bug objects:\n";
                output += "------------------------------\n";
                foreach (var item in HoarderBugAI.grabbableObjectsInMap)
                    output += item.name + "\n";
                output += "------------------------------\n";
                return output;
            }
        }
#endif
    }
}
