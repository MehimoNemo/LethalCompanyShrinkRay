using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LCShrinkRay.patches
{

    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        static float smallestPlayerSize = 0.2f; // smallest the player is allowed to be
        static Vector3 SmallPosition = new Vector3(-0.4795f, 0.2694f, 0.085f); // used when the player is 0.2f tall
        static Vector3 NormalPosition = new Vector3(-0.84F, -1.49F, 0.09F); // used when the player is 1f tall

        [HarmonyPatch("BeginUsingTerminal")]
        [HarmonyPostfix]
        public static void MovePlayerPos()
        {
            GameObject playerPos = GameObject.Find("Environment/HangarShip/Terminal/TerminalTrigger/playerPos");
            PlayerControllerB playerUsingTerminal = null;

            // check which player is using terminal
            if (StartOfRound.Instance.localPlayerController.inTerminalMenu)
            {
                // player using terminal is local client
                playerUsingTerminal = StartOfRound.Instance.localPlayerController;
            }
            else
            {
                // player using terminal is not local client
                foreach (PlayerControllerB player in StartOfRound.Instance.OtherClients)
                {
                    if (player.inTerminalMenu)
                    {
                        playerUsingTerminal = player;
                        break;
                    }
                }
            }
            // checking if player is set
            if (playerUsingTerminal == null)
            {
                Plugin.log(string.Format("No player using terminal"), Plugin.LogType.Warning);
                return;
            }
            //((1 - y) / (1 - smallestSize))
            // making sure the value is between 0 and 1
            float size = Mathf.Clamp01((1 - playerUsingTerminal.transform.localScale.y) / (1 - smallestPlayerSize));
            Plugin.log(string.Format("size value: {0}", size), Plugin.LogType.Warning);

            playerPos.transform.localPosition = Vector3.Lerp(NormalPosition, SmallPosition, size);
        }
    }
}
