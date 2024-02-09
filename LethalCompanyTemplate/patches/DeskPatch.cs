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
        public static bool IsPlayerSelling = false;
        private static PlayerControllerB playerWhoTriggered = null;
        private static GrabbableObject placedItem = null;
        private static List<GrabbableObject> placedItems = new List<GrabbableObject>();

        [HarmonyPatch(typeof(DepositItemsDesk), "Start")]
        [HarmonyPostfix()]
        public static void Start()
        {
            Plugin.Log("STARTING PLAYER SELLING PATCH");
            IsPlayerSelling = Config.ModConfig.Instance.values.sellablePlayers;
            Plugin.Log("isPlayerSelling: " + IsPlayerSelling.ToString());
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "PlaceItemOnCounter")]
        [HarmonyPrefix()]
        public static void PlaceItemOnCounterPrefix(PlayerControllerB playerWhoTriggered)
        {
            Plugin.Log("PlaceItemOnCounterPrefix");
            if (playerWhoTriggered == null)
            {
                Plugin.Log("Player is null", Plugin.LogType.Error);
            }
            DeskPatch.playerWhoTriggered = playerWhoTriggered;
            placedItem = playerWhoTriggered.currentlyHeldObjectServer;
            placedItems.Add(placedItem);
            if (placedItem == null)
                Plugin.Log("placedItem is null", Plugin.LogType.Error);
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "AddObjectToDeskServerRpc")]
        [HarmonyPrefix()]
        public static void AddObjectToDeskServerRpcPrefix()
        {
            Plugin.Log("addin Object to desk");
            if (playerWhoTriggered == null)
            {
                Plugin.Log("Oh sure, now nobody want to be in fault for placing this poor player here.....");
            }
            if (IsPlayerSelling)
            {
                var placedPlayer = placedItem as GrabbablePlayerObject;
                if (placedPlayer == null)
                {
                    Plugin.Log("placedItem is not a player.. not my job");
                    return;
                }

                Plugin.Log("Item is a sellable player >:) little does he know..");
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
                        Plugin.Log("item of the placedPlayer is null....");
                }
                placedPlayer.scrapValue = scrapValue;
            }
            else
            {
                foreach (var item in placedItems)
                {
                    if (item is GrabbablePlayerObject)
                        Plugin.Log("Nuh uh honey bear! We are not selling this fella!!!", Plugin.LogType.Warning);
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
                Plugin.Log("Unable to add player to sell counter. Reason: " + e.Message, Plugin.LogType.Error);
            }
            //if the object exists, and it is a player object
            bool isPlayer = grabbableObject != null && grabbableObject is GrabbablePlayerObject;
            //return true and DON'T run the rest of the original method
            if (!IsPlayerSelling && isPlayer)
            {
                Plugin.Log("CANCELLING ORIGINAL METHOD CALL IN AddObjectToDeskServerRpc");
                //exit the original method early
                __instance.itemsOnCounterNetworkObjects.Remove(grabbableObjectNetObject);
                __instance.itemsOnCounter.Remove(grabbableObject);
            }
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPostfix()]
        public static void SellStuffPostfix()
        {
            Plugin.Log("selling on desk");
            if (IsPlayerSelling && doomedPlayers.Count > 0)
            {
                Plugin.Log("doomedPlayersCount: " + doomedPlayers.Count);
                foreach (GrabbablePlayerObject gplayer in doomedPlayers)
                {
                    PlayerControllerB player = gplayer.grabbedPlayer;
                    Plugin.Log("Killing player: " + player.playerClientId);
                    //kill player then enable movement here!!
                    gplayer.SellKill();
                    gplayer.Unfreeze();
                    placedItems.Clear();
                }
            }
        }
    }
}
