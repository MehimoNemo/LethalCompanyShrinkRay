using GameNetcodeStuff;
using LC_API.GameInterfaceAPI.Events;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static LCShrinkRay.comp.Vents;

namespace LCShrinkRay.comp
{
    internal class Vents
    {
        private static bool sussification = false;

        private static EnemyVent[] getAllVents()
        {
            if (RoundManager.Instance.allEnemyVents != null && RoundManager.Instance.allEnemyVents.Length > 0)
                return RoundManager.Instance.allEnemyVents;

            return UnityEngine.Object.FindObjectsOfType<EnemyVent>();
        }

        public static void SussifyAll()
        {
            if (!GameNetworkManager.Instance.gameHasStarted)
                return;

            if(sussification) // Already sussified
                return;

            if (!ModConfig.Instance.values.canUseVents)
            {
                Plugin.log("Sussification of vents disabled.");
                return;
            }

            if(PlayerHelper.isCurrentPlayerShrunk())
            {
                Plugin.log("Sussification of vents only for shrunken players.");
                return;
            }    

            Plugin.log("SUSSIFYING VENTS");

            var vents = getAllVents();
            if (vents == null || vents.Length == 0)
            {
                Plugin.log("No vents to sussify.");
                return;
            }

            GameObject dungeonEntrance = GameObject.Find("EntranceTeleportA(Clone)");
            for (int i = 0; i < vents.Length; i++)
            {
                Plugin.log("SUSSIFYING VENT " + i);

                int siblingIndex = vents.Length - i - 1;
                if (siblingIndex == i) // maybe "while" instead of "if"?
                {
                    System.Random rnd = new System.Random();
                    siblingIndex = rnd.Next(0, vents.Length);
                }

                Plugin.log("\tPairing with vent " + siblingIndex);

                sussify(vents[i], vents[siblingIndex]);
            }

            sussification = true;
            coroutines.RenderVents.StartRoutine(dungeonEntrance);
        }

        public static void sussify(EnemyVent enemyVent, EnemyVent siblingVent)
        {
            GameObject vent = enemyVent.gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject;
            if (!vent)
            {
                Plugin.log("Vent has no cover to sussify");
                return;
            }

            vent.GetComponent<MeshRenderer>();
            vent.tag = "InteractTrigger";
            vent.layer = LayerMask.NameToLayer("InteractableObject");
            var sussifiedVent = enemyVent.gameObject.AddComponent<SussifiedVent>();
            sussifiedVent.siblingVent = siblingVent;
            var trigger = vent.AddComponent<InteractTrigger>();
            vent.AddComponent<BoxCollider>();

            // Add interaction
            trigger.hoverIcon = GameObject.Find("StartGameLever")?.GetComponent<InteractTrigger>()?.hoverIcon;
            trigger.hoverTip = "Enter : [LMB]";
            trigger.interactable = true;
            trigger.oneHandedItemAllowed = true;
            trigger.twoHandedItemAllowed = true;
            trigger.holdInteraction = true;
            trigger.timeToHold = 1.5f;
            trigger.timeToHoldSpeedMultiplier = 1f;

            // Create new instances of InteractEvent for each trigger
            trigger.holdingInteractEvent = new InteractEventFloat();
            trigger.onInteract = new InteractEvent();
            trigger.onInteractEarly = new InteractEvent();
            trigger.onStopInteract = new InteractEvent();
            trigger.onCancelAnimation = new InteractEvent();

            //checks that we don't set a vent to have itself as a sibling if their is an odd number

            trigger.onInteract.AddListener((player) => sussifiedVent.TeleportPlayer(player));
            trigger.enabled = true;
            vent.GetComponent<Renderer>().enabled = true;
            Plugin.log("VentCover Object: " + vent.name);
            Plugin.log("VentCover Renderer Enabled: " + vent.GetComponent<Renderer>().enabled);
            Plugin.log("Hover Icon: " + (trigger.hoverIcon != null ? trigger.hoverIcon.name : "null"));
        }

        // when unshrinking will be a thing
        public static void unsussifyAll()
        {
            return; // wip
            foreach (var vent in getAllVents())
                unsussify(vent);

            sussification = false;
        }

        public static void unsussify(EnemyVent enemyVent)
        {
            GameObject vent = enemyVent.gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject;
            if (!vent)
                return;

            Plugin.log("0");
            if (enemyVent.gameObject.AddComponent<SussifiedVent>() != null)
            {
                Plugin.log("1");
                UnityEngine.Object.Destroy(enemyVent.gameObject.AddComponent<SussifiedVent>());
            }
            if (vent.GetComponent<BoxCollider>() != null)
            {
                Plugin.log("2");
                UnityEngine.Object.Destroy(vent.GetComponent<BoxCollider>());
            }
            if (vent.GetComponent<InteractTrigger>() != null)
            {
                Plugin.log("3");
                UnityEngine.Object.Destroy(vent.GetComponent<InteractTrigger>());
            }
        }


        internal class SussifiedVent : NetworkBehaviour
        {
            public EnemyVent siblingVent { get; set; }

            internal void Start() { }

            internal void TeleportPlayer(PlayerControllerB player)
            {
                Transform transform = player.gameObject.transform;
                //teleport da playa to dis vent
                if (siblingVent != null)
                {
                    if (PlayerHelper.isShrunk(player.gameObject))
                    {
                        Plugin.log("\n⠀⠀⠀⠀⢀⣴⣶⠿⠟⠻⠿⢷⣦⣄⠀⠀⠀\r\n⠀⠀⠀⠀⣾⠏⠀⠀⣠⣤⣤⣤⣬⣿⣷⣄⡀\r\n⠀⢀⣀⣸⡿⠀⠀⣼⡟⠁⠀⠀⠀⠀⠀⠙⣷\r\n⢸⡟⠉⣽⡇⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⢀⣿\r\n⣾⠇⠀⣿⡇⠀⠀⠘⠿⢶⣶⣤⣤⣶⡶⣿⠋\r\n⣿⠂⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠃\r\n⣿⡆⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀\r\n⢿⡇⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠀\r\n⠘⠻⠷⢿⡇⠀⠀⠀⣴⣶⣶⠶⠖⠀⢸⡟⠀\r\n⠀⠀⠀⢸⣇⠀⠀⠀⣿⡇⣿⡄⠀⢀⣿⠇⠀\r\n⠀⠀⠀⠘⣿⣤⣤⣴⡿⠃⠙⠛⠛⠛⠋⠀⠀");
                        //StartCoroutine(OccupyVent(siblingVent));
                        //siblingVent.ventAudio.Play();
                        transform.position = siblingVent.floorNode.transform.position;
                    }
                }
                else
                {
                    //7.9186 0.286 -14.1901
                    transform.position = new Vector3(7.9186f, 0.286f, -14.1901f);
                }
            }

            private IEnumerator OccupyVent()
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
}
