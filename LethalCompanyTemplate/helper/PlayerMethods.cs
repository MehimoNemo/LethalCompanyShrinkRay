using GameNetcodeStuff;
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

        public static List<GameObject> getAllPlayers()
        {
            return StartOfRound.Instance.allPlayerScripts.Where(pcb => pcb.isPlayerControlled).Select(pcb => pcb.gameObject).ToList();
        }

        public static GameObject GetPlayerObject(ulong playerID)
        {
            string myPlayerObjectName = "Player";
            if (playerID != 0ul)
                myPlayerObjectName = "Player (" + playerID.ToString() + ")";

            return GameObject.Find(myPlayerObjectName);
        }

        public static PlayerControllerB currentPlayer()
        {
            return StartOfRound.Instance.localPlayerController;
        }

        public static float currentPlayerScale()
        {
            if (!currentPlayer() || !currentPlayer().gameObject)
            {
                Plugin.log("unable to retrieve currentPlayerScale!");
                return 1f;
            }

            return currentPlayer().gameObject.transform.localScale.x;
        }

        public static bool isShrunk(GameObject playerObject)
        {
            if (playerObject == null)
                return false;

            return isShrunk(playerObject.transform.localScale.x);
        }
        public static bool isCurrentPlayerShrunk() { return isShrunk(currentPlayerScale()); }
        public static bool isShrunk(float size) { return size < 1f; }
    }
}
