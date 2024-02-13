using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.helper;
using Vector3 = UnityEngine.Vector3;

namespace LCShrinkRay.patches
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
            if (placedPlayer == null) return;

            if(!Config.ModConfig.Instance.values.sellablePlayers)
            {
                Plugin.Log("Nuh uh honey bear! We are not selling this fella!!! (Player " + placedPlayer.grabbedPlayerID.Value + ")", Plugin.LogType.Warning);
                return;
            }

            placedPlayer.PlaceOnSellCounter();

            int scrapValue = 5;
            foreach (var valuableItem in placedPlayer.grabbedPlayer.ItemSlots)
            {
                if (valuableItem != null)
                    scrapValue += valuableItem.scrapValue;
            }
            placedPlayer.scrapValue = scrapValue;
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPrefix()]
        public static void SellStuffPrefix()
        {
            Plugin.Log("selling on desk");

            if(!GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
            {
                Plugin.Log("Our own GrabbablePlayerObject went missing..");
                return;
            }

            if (gpo.CanSellKill())
            {
                gpo.grabbedPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                Plugin.Log("We got killed by the sell counter monster!");
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        [HarmonyPrefix()]
        public static void ShipHasLeftPrefix()
        {
            if (!GrabbablePlayerList.TryFindGrabbableObjectForPlayer(PlayerInfo.CurrentPlayerID, out GrabbablePlayerObject gpo))
                return;

            if(gpo.IsOnSellCounter.Value)
                gpo.RemoveFromSellCounter(); // left behind
        }
    }
}
