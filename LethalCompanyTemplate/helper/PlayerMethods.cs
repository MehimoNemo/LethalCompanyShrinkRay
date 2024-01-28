using GameNetcodeStuff;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.helper
{
    internal class PlayerHelper // maybe find better name
    {
        public static bool isHost()
        {
            return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        }

        public static bool IsCurrentPlayerGrabbed()
        {
            var gpo = GrabbablePlayerList.findGrabbableObjectForPlayer(PlayerHelper.currentPlayer().playerClientId);
            return gpo != null && gpo.playerHeldBy != null;
        }

        public static float calculatePlayerWeightFor(PlayerControllerB player, bool playerWeightIncluded = false)
        {
            float baseValue = 1f;
            float weight = playerWeightIncluded ? (0.1f * player.transform.localScale.x) : 0;

            if (player != null && player.ItemSlots != null)
            {
                foreach (var item in player.ItemSlots)
                    if (item != null)
                        weight += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);
            }

            if(PlayerHelper.isShrunk(player.gameObject))
                weight *= ModConfig.Instance.values.weightMultiplier;

            return baseValue + weight;
        }

        public static List<GameObject> getAllPlayers()
        {
            return StartOfRound.Instance.allPlayerScripts.Where(pcb => pcb.isPlayerControlled).Select(pcb => pcb.gameObject).ToList();
        }

        public static PlayerControllerB GetPlayerController(ulong playerID)
        {
            foreach(var pcb in StartOfRound.Instance.allPlayerScripts)
            {
                if (pcb.playerClientId == playerID)
                    return pcb;
            }
            return null;
        }

        public static GameObject GetPlayerObject(ulong playerID)
        {
            string myPlayerObjectName = "Player";
            if (playerID != 0ul)
                myPlayerObjectName = "Player (" + playerID.ToString() + ")";

            return GameObject.Find(myPlayerObjectName);
        }

        public static ulong? GetPlayerID(GameObject gameObject)
        {
            if (gameObject.name.Contains('('))
            {
                int startIndex = gameObject.name.IndexOf("(");
                int endIndex = gameObject.name.IndexOf(")");
                return ulong.Parse(gameObject.name.Substring(startIndex + 1, endIndex - startIndex - 1));
            }
            return null;
        }

        public static PlayerControllerB currentPlayer()
        {
            return StartOfRound.Instance.localPlayerController;
        }

        public static float currentPlayerScale()
        {
            var player = currentPlayer();
            if (!player || !player.gameObject)
            {
                Plugin.log("unable to retrieve currentPlayerScale!");
                return 1f;
            }

            return Mathf.Round(player.gameObject.transform.localScale.x * 100f) / 100f; // round to 2 digits
        }

        public static bool isShrunk(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            return isShrunk(playerObject.transform.localScale.x);
        }
        public static bool isCurrentPlayerShrunk() { return isShrunk(currentPlayerScale()); }
        public static bool isShrunk(float size) {
            var roundedSize = Mathf.Round(size * 100f) / 100f; // round to 2 digits
            return roundedSize < 1f;
        }

        public static bool isNormalSize(float size)
        {
            var roundedSize = Mathf.Round(size * 100f) / 100f; // round to 2 digits
            return roundedSize == 1f;
        }

        public static GrabbableObject HeldItem(PlayerControllerB pcb)
        {
            if (pcb.isHoldingObject && pcb.ItemSlots[pcb.currentItemSlot] != null)
                return pcb.ItemSlots[pcb.currentItemSlot];

            return null;
        }
    }
}
