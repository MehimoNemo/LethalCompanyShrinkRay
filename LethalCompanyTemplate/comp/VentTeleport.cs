using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class VentTeleport : NetworkBehaviour
    {
        internal void Start() { }

        internal void TeleportPlayer(PlayerControllerB player, EnemyVent siblingVent)
        {
            Transform transform = player.gameObject.transform;
            //teleport da playa to dis vent
            if(siblingVent != null) {
                if (Shrinking.isShrunk(player.gameObject))
                {
                    Plugin.log("\n⠀⠀⠀⠀⢀⣴⣶⠿⠟⠻⠿⢷⣦⣄⠀⠀⠀\r\n⠀⠀⠀⠀⣾⠏⠀⠀⣠⣤⣤⣤⣬⣿⣷⣄⡀\r\n⠀⢀⣀⣸⡿⠀⠀⣼⡟⠁⠀⠀⠀⠀⠀⠙⣷\r\n⢸⡟⠉⣽⡇⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⢀⣿\r\n⣾⠇⠀⣿⡇⠀⠀⠘⠿⢶⣶⣤⣤⣶⡶⣿⠋\r\n⣿⠂⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠃\r\n⣿⡆⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀\r\n⢿⡇⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠀\r\n⠘⠻⠷⢿⡇⠀⠀⠀⣴⣶⣶⠶⠖⠀⢸⡟⠀\r\n⠀⠀⠀⢸⣇⠀⠀⠀⣿⡇⣿⡄⠀⢀⣿⠇⠀\r\n⠀⠀⠀⠘⣿⣤⣤⣴⡿⠃⠙⠛⠛⠛⠋⠀⠀");
                    //StartCoroutine(OccupyVent(siblingVent));
                    //siblingVent.ventAudio.Play();
                    transform.position = siblingVent.floorNode.transform.position;
                }
            }
            else {
                //7.9186 0.286 -14.1901
                transform.position = new Vector3(7.9186f, 0.286f, -14.1901f);
            }
        }

        private IEnumerator OccupyVent(EnemyVent siblingVent)
        {
            EnemyVent thisVent = this.transform.parent.gameObject.transform.GetComponent<EnemyVent>();
            thisVent.OpenVentClientRpc();
            thisVent.occupied = true;
            siblingVent.occupied = true;
            float delay = 0.2f;
            yield return new WaitForSeconds(delay);
            thisVent.occupied = false;
            siblingVent.occupied = false;
            siblingVent.OpenVentClientRpc();
        }
    }
}