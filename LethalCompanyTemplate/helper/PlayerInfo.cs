using GameNetcodeStuff;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.helper
{
    internal class PlayerInfo // maybe find better name
    {
        public static bool IsHost
        {
            get
            {
                return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
            }
        }

        public static bool IsCurrentPlayerGrabbed()
        {
            var gpo = GrabbablePlayerList.findGrabbableObjectForPlayer(CurrentPlayer.playerClientId);
            return gpo != null && gpo.playerHeldBy != null;
        }

        public static float CalculateWeightFor(PlayerControllerB player, bool playerWeightIncluded = false)
        {
            float baseValue = 1f;
            float weight = playerWeightIncluded ? (0.1f * player.transform.localScale.x) : 0;

            if (player != null && player.ItemSlots != null)
            {
                foreach (var item in player.ItemSlots)
                    if (item != null)
                        weight += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);
            }

            if(IsShrunk(player))
                weight *= ModConfig.Instance.values.weightMultiplier;

            return baseValue + weight;
        }

        public static List<GameObject> AllPlayers
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Where(pcb => pcb.isPlayerControlled).Select(pcb => pcb.gameObject).ToList();
            }
        }

        public static PlayerControllerB ControllerFromID(ulong playerID)
        {
            foreach(var pcb in StartOfRound.Instance.allPlayerScripts)
            {
                if (pcb.playerClientId == playerID)
                    return pcb;
            }
            return null;
        }

        public static ulong? IDFromObject(GameObject gameObject)
        {
            if (!gameObject.name.Contains('('))
                return null;

            int startIndex = gameObject.name.IndexOf("(");
            int endIndex = gameObject.name.IndexOf(")");
            return ulong.Parse(gameObject.name.Substring(startIndex + 1, endIndex - startIndex - 1));
        }

        public static PlayerControllerB CurrentPlayer
        {
            get
            {
                return StartOfRound.Instance.localPlayerController;
            }
        }

        public static float CurrentPlayerScale
        {
            get
            {
                return PlayerScale(CurrentPlayer);
            }
        }

        public static float PlayerScale(PlayerControllerB player)
        {
            if (!player || !player.gameObject)
            {
                Plugin.log("unable to retrieve currentPlayerScale!");
                return 1f;
            }

            return PlayerScale(player.gameObject);
        }

        public static float PlayerScale(GameObject playerObject)
        {
            if (playerObject == null) return 1f;

            return Rounded(playerObject.transform.localScale.x);
        }

        public static float Rounded(float unroundedValue)
        {
            return Mathf.Round(unroundedValue * 100f) / 100f; // round to 2 digits
        }

        public static bool IsShrunk(PlayerControllerB player)
        {
            return IsShrunk(player.gameObject);
        }

        public static bool IsShrunk(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            return IsShrunk(playerObject.transform.localScale.x);
        }

        public static bool IsShrunk(float size)
        {
            return Rounded(size) < 1f;
        }

        public static bool IsNormalSize(PlayerControllerB player)
        {
            return IsNormalSize(player.gameObject);
        }

        public static bool IsNormalSize(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            return IsNormalSize(playerObject.transform.localScale.x);
        }

        public static bool IsNormalSize(float size)
        {
            return Rounded(size) == 1f;
        }

        public static bool IsCurrentPlayerShrunk
        {
            get
            {
                return IsShrunk(CurrentPlayer);
            }
        }

        public static GrabbableObject HeldItem(PlayerControllerB pcb)
        {
            if (pcb != null && pcb.isHoldingObject && pcb.ItemSlots[pcb.currentItemSlot] != null)
                return pcb.ItemSlots[pcb.currentItemSlot];

            return null;
        }

        public static GrabbableObject CurrentPlayerHeldItem
        {
            get
            {
                return HeldItem(CurrentPlayer);
            }
        }
    }
}
