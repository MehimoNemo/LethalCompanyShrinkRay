using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class VentTeleport : NetworkBehaviour
    {
        private static ManualLogSource mls;

        internal void Start()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
        }

        internal void TeleportPlayer(PlayerControllerB player, EnemyVent siblingVent)
        {
            mls.LogMessage("\n⠀⠀⠀⠀⢀⣴⣶⠿⠟⠻⠿⢷⣦⣄⠀⠀⠀\r\n⠀⠀⠀⠀⣾⠏⠀⠀⣠⣤⣤⣤⣬⣿⣷⣄⡀\r\n⠀⢀⣀⣸⡿⠀⠀⣼⡟⠁⠀⠀⠀⠀⠀⠙⣷\r\n⢸⡟⠉⣽⡇⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⢀⣿\r\n⣾⠇⠀⣿⡇⠀⠀⠘⠿⢶⣶⣤⣤⣶⡶⣿⠋\r\n⣿⠂⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠃\r\n⣿⡆⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀\r\n⢿⡇⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠀\r\n⠘⠻⠷⢿⡇⠀⠀⠀⣴⣶⣶⠶⠖⠀⢸⡟⠀\r\n⠀⠀⠀⢸⣇⠀⠀⠀⣿⡇⣿⡄⠀⢀⣿⠇⠀\r\n⠀⠀⠀⠘⣿⣤⣤⣴⡿⠃⠙⠛⠛⠛⠋⠀⠀");
            //teleport da playa to dis vent
            if(siblingVent != null) {

            }
            else {
                //7.9186 0.286 -14.1901
                player.gameObject.transform.position = new Vector3(7.9186f, 0.286f, -14.1901f);
            }
        }
    }
}