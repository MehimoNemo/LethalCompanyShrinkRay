using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.patches
{

    internal class DeskPatch
    {
        

        private static List<GrabbablePlayerObject> doomedPlayers = new List<GrabbablePlayerObject>();
        public static bool isPlayerSelling;
        private static PlayerControllerB playerWhoTriggered;
        private static GrabbableObject placedItem;
        private static List<GrabbableObject> placedItems = new List<GrabbableObject>();

        [HarmonyPatch(typeof(DepositItemsDesk), "Start")]
        [HarmonyPostfix()]
        public static void Start()
        {
            
            Plugin.log("STARTING PLAYER SELLING PATCH", Plugin.LogType.Error);
            isPlayerSelling = Config.ModConfig.Instance.values.sellablePlayers;
            Plugin.log("isPlayerSelling: " + isPlayerSelling.ToString(), Plugin.LogType.Error);
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "PlaceItemOnCounter")]
        [HarmonyPrefix()]
        public static void PlaceItemOnCounterPrefix(PlayerControllerB playerWhoTriggered)
        {
            Plugin.log("PlaceItemOnCounterPrefix", Plugin.LogType.Error);
            if (playerWhoTriggered == null)
            {
                Plugin.log("Player is null", Plugin.LogType.Error);
            }
            DeskPatch.playerWhoTriggered = playerWhoTriggered;
            placedItem = playerWhoTriggered.currentlyHeldObjectServer;
            placedItems.Add(placedItem);
            if (placedItem == null)
            {
                Plugin.log("placedItem is null", Plugin.LogType.Error);
            }
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "AddObjectToDeskServerRpc")]
        [HarmonyPrefix()]
        public static void AddObjectToDeskServerRpcPrefix()
        {
            Plugin.log("addin Object to desk", Plugin.LogType.Error);
            if (playerWhoTriggered == null)
            {
                Plugin.log("Oh my god it's null.....", Plugin.LogType.Error);
            }
            if (isPlayerSelling && placedItem != null)
            {
                Plugin.log("Running sellable player code now", Plugin.LogType.Error);
                GrabbableObject item = placedItem;
                if (item is GrabbablePlayerObject)
                {
                    Plugin.log("Item is sellable player >:)", Plugin.LogType.Error);
                    GrabbablePlayerObject gPlayerObject = (GrabbablePlayerObject)item;
                    doomedPlayers.Add(gPlayerObject);
                    //freeze player here!
                    gPlayerObject.Freeze();
                    //Set player value to value of held items??? here

                    int scrapValue = 5;
                    foreach (GrabbableObject valuableItem in gPlayerObject.grabbedPlayer.ItemSlots)
                    {
                        if (valuableItem != null)
                            scrapValue += valuableItem.scrapValue;
                        else
                            Plugin.log("the item is null....");
                    }
                    gPlayerObject.scrapValue = scrapValue;
                }
                else
                {
                    Plugin.log("Fuck you, idiot, grabbable object is null");
                }
            }
            if (!isPlayerSelling) {
                foreach (GrabbableObject item in placedItems)
                {
                    if (item is GrabbablePlayerObject)
                    {
                        Plugin.log("Nuh uh honey bear! We are not selling this fella!!!", Plugin.LogType.Warning);
                    }
                }
            }
            else
            {
                Plugin.log("isPlayerSelling is uhh....false... or placedItem is null??", Plugin.LogType.Error);
            }
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "AddObjectToDeskServerRpc")]
        [HarmonyPostfix()]
        public static void AddToDeskServer(DepositItemsDesk __instance, NetworkObjectReference grabbableObjectNetObject)
        {
            NetworkObject thisObject;
            grabbableObjectNetObject.TryGet(out thisObject);
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
            Plugin.log("selling on desk", Plugin.LogType.Error);
            if (isPlayerSelling && doomedPlayers.Count > 0)
            {

                Plugin.log("doomedPlayersCount: " + doomedPlayers.Count, Plugin.LogType.Error);
                foreach (GrabbablePlayerObject gplayer in doomedPlayers)
                {
                    PlayerControllerB player = gplayer.grabbedPlayer;
                    Plugin.log("Killing player: " + player.playerClientId, Plugin.LogType.Error);
                    //kill player then enable movement here!!
                    gplayer.SellKill();
                    gplayer.Unfreeze();
                    placedItems.Clear();
                }
            }
        }
    }
}
