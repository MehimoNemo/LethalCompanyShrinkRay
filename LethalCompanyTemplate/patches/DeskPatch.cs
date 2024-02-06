using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace LCShrinkRay.patches
{
    internal class DeskPatch
    {
        private static List<GrabbablePlayerObject> doomedPlayers = new List<GrabbablePlayerObject>();
        public static bool isPlayerSelling = false;
        private static PlayerControllerB playerWhoTriggered = null;
        private static GrabbableObject placedItem = null;
        private static List<GrabbableObject> placedItems = new List<GrabbableObject>();

        [HarmonyPatch(typeof(DepositItemsDesk), "Start")]
        [HarmonyPostfix()]
        public static void Start()
        {
            Plugin.log("STARTING PLAYER SELLING PATCH");
            isPlayerSelling = Config.ModConfig.Instance.values.sellablePlayers;
            Plugin.log("isPlayerSelling: " + isPlayerSelling.ToString());
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "PlaceItemOnCounter")]
        [HarmonyPrefix()]
        public static void PlaceItemOnCounterPrefix(PlayerControllerB playerWhoTriggered)
        {
            Plugin.log("PlaceItemOnCounterPrefix");
            if (playerWhoTriggered == null)
            {
                Plugin.log("Player is null", Plugin.LogType.Error);
            }
            DeskPatch.playerWhoTriggered = playerWhoTriggered;
            placedItem = playerWhoTriggered.currentlyHeldObjectServer;
            placedItems.Add(placedItem);
            if (placedItem == null)
                Plugin.log("placedItem is null", Plugin.LogType.Error);
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "AddObjectToDeskServerRpc")]
        [HarmonyPrefix()]
        public static void AddObjectToDeskServerRpcPrefix()
        {
            Plugin.log("addin Object to desk");
            if (playerWhoTriggered == null)
            {
                Plugin.log("Oh sure, now nobody want to be in fault for placing this poor player here.....");
            }
            if (isPlayerSelling)
            {
                var placedPlayer = placedItem as GrabbablePlayerObject;
                if (placedPlayer == null)
                {
                    Plugin.log("placedItem is not a player.. not my job");
                    return;
                }

                Plugin.log("Item is a sellable player >:) little does he know..");
                doomedPlayers.Add(placedPlayer);
                //freeze player here!
                placedPlayer.Freeze();
                //Set player value to value of held items??? here

                int scrapValue = 5;
                foreach (var valuableItem in placedPlayer.grabbedPlayer.ItemSlots)
                {
                    if (valuableItem != null)
                        scrapValue += valuableItem.scrapValue;
                    else
                        Plugin.log("item of the placedPlayer is null....");
                }
                placedPlayer.scrapValue = scrapValue;
            }
            else
            {
                foreach (var item in placedItems)
                {
                    if (item is GrabbablePlayerObject)
                        Plugin.log("Nuh uh honey bear! We are not selling this fella!!!", Plugin.LogType.Warning);
                }
            }
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "AddObjectToDeskServerRpc")]
        [HarmonyPostfix()]
        public static void AddToDeskServer(DepositItemsDesk __instance, NetworkObjectReference grabbableObjectNetObject)
        {
            grabbableObjectNetObject.TryGet(out NetworkObject thisObject);
            GrabbableObject grabbableObject = null;
            try
            {
                grabbableObject = thisObject.gameObject.GetComponent<GrabbableObject>();
            } catch (Exception e)
            {
                Plugin.log("Unable to add player to sell counter. Reason: " + e.Message, Plugin.LogType.Error);
            }
            //if the object exists, and it is a player object
            bool isPlayer = grabbableObject != null && grabbableObject is GrabbablePlayerObject;
            //return true and DON'T run the rest of the original method
            if (!isPlayerSelling && isPlayer)
            {
                Plugin.log("CANCELLING ORIGINAL METHOD CALL IN AddObjectToDeskServerRpc");
                //exit the original method early
                __instance.itemsOnCounterNetworkObjects.Remove(grabbableObjectNetObject);
                __instance.itemsOnCounter.Remove(grabbableObject);
            }
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPostfix()]
        public static void SellStuffPostfix()
        {
            Plugin.log("selling on desk");
            if (isPlayerSelling && doomedPlayers.Count > 0)
            {
                Plugin.log("doomedPlayersCount: " + doomedPlayers.Count);
                foreach (GrabbablePlayerObject gplayer in doomedPlayers)
                {
                    PlayerControllerB player = gplayer.grabbedPlayer;
                    Plugin.log("Killing player: " + player.playerClientId);
                    //kill player then enable movement here!!
                    gplayer.SellKill();
                    gplayer.Unfreeze();
                    placedItems.Clear();
                }
            }
        }
    }
}
