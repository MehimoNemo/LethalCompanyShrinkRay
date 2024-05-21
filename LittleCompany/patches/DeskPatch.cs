﻿using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using Vector3 = UnityEngine.Vector3;

namespace LittleCompany.patches
{
    internal class DeskPatch
    {
        [HarmonyPatch(typeof(DepositItemsDesk), "PlaceItemOnCounter")]
        [HarmonyPrefix()]
        public static void PlaceItemOnCounterPrefix(PlayerControllerB playerWhoTriggered)
        {
            Plugin.Log("PlaceItemOnCounterPrefix");
            var placedItem = playerWhoTriggered.currentlyHeldObjectServer;
            if (placedItem == null) return;

            var placedPlayer = (placedItem as GrabbablePlayerObject);
            if (placedPlayer == null)
            {
                ObjectModification.ScalingOf(placedItem).RemoveHologram();
                return;
            }

            if(!Plugin.Config.SELLABLE_PLAYERS)
            {
                Plugin.Log("Nuh uh honey bear! We are not selling this fella!!! (Player " + placedPlayer.grabbedPlayerID.Value + ")", Plugin.LogType.Warning);
                return;
            }

            placedPlayer.PlaceOnSellCounterServerRpc();
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPrefix()]
        public static void SellStuffPrefix(DepositItemsDesk __instance, ref int profit)
        {
            var placedObjects = __instance.deskObjectsContainer.GetComponentsInChildren<GrabbableObject>();
            foreach(var obj in placedObjects )
            {
                var gpo = obj as GrabbablePlayerObject;
                if (gpo == null) continue;

                if (gpo.IsCurrentPlayer)
                {
                    gpo.grabbedPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                    Plugin.Log("We got killed by the sell counter monster!");
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        [HarmonyPrefix()]
        public static void ShipHasLeftPrefix()
        {
            if (!PlayerInfo.CurrentPlayerID.HasValue || !GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID.Value, out GrabbablePlayerObject gpo))
                return;

            if (gpo.IsOnSellCounter.Value)
            {
                gpo.RemoveFromSellCounterServerRpc();
                Plugin.Log("We got left behind :c");
            }
        }
    }
}
