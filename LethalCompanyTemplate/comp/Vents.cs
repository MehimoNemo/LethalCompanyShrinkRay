using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LCShrinkRay.comp
{
    [HarmonyPatch]
    internal class Vents
    {
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
        public static void AfterFinishGeneratingNewLevelClient()
        {
            Vents.SussifyAll();
        }


        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        public static void Initialize()
        {
            if(!StartOfRound.Instance.inShipPhase)
                SussifyAll(); // In case someone joins late
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        public static void EndRound()
        {
            Vents.UnsussifyAll();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            Vents.UnsussifyAll();
        }

        #region Properties
        private static bool sussification = false;
        private static List<InteractTrigger> ventTrigger = new List<InteractTrigger>();
        #endregion

        #region Methods
        private static EnemyVent[] GetAllVents()
        {
            if (RoundManager.Instance.allEnemyVents != null && RoundManager.Instance.allEnemyVents.Length > 0)
                return RoundManager.Instance.allEnemyVents;

            return Object.FindObjectsOfType<EnemyVent>();
        }

        public static void SussifyAll()
        {
            if (sussification) // Already sussified
                return;

            if (!ModConfig.Instance.values.canUseVents)
            {
                Plugin.Log("Sussification of vents disabled.");
                return;
            }

            Plugin.Log("SUSSIFYING VENTS");

            var vents = GetAllVents();
            if (vents == null || vents.Length == 0)
            {
                Plugin.Log("No vents to sussify.");
                return;
            }

            GameObject dungeonEntrance = GameObject.Find("EntranceTeleportA(Clone)");
            for (int i = 0; i < vents.Length; i++)
            {
                int siblingIndex = vents.Length - i - 1;
                if (siblingIndex == i) // maybe "while" instead of "if"?
                {
                    System.Random rnd = new System.Random();
                    siblingIndex = rnd.Next(0, vents.Length);
                }

                Plugin.Log("\tPairing vent " + i + " with vent " + siblingIndex);

                Sussify(vents[i], vents[siblingIndex]);
            }

            sussification = true;
            coroutines.RenderVents.StartRoutine(dungeonEntrance);

            if (!PlayerInfo.IsCurrentPlayerShrunk)
                DisableVents();
        }

        public static void Sussify(EnemyVent enemyVent, EnemyVent siblingVent)
        {
            GameObject vent;
            try
            {
                vent = enemyVent.gameObject.transform.Find("ventTunnel").gameObject; // Hinge -> VentCover
            }
            catch
            {
                Plugin.Log("Vent has no cover to sussify");
                return;
            }

            vent.tag = "InteractTrigger";
            vent.layer = LayerMask.NameToLayer("InteractableObject");
            var sussifiedVent = enemyVent.gameObject.AddComponent<SussifiedVent>();
            sussifiedVent.thisVent = enemyVent;
            sussifiedVent.siblingVent = siblingVent;
            var trigger = vent.AddComponent<InteractTrigger>();

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
            ventTrigger.Add(trigger);
        }

        // when unshrinking will be a thing
        public static void UnsussifyAll()
        {
            Plugin.Log("Vents.unsussifyAll");
            foreach (var vent in GetAllVents())
                Unsussify(vent);

            ventTrigger.Clear();
            sussification = false;
        }

        public static void Unsussify(EnemyVent enemyVent)
        {
            if (enemyVent.gameObject.TryGetComponent(out SussifiedVent sussifiedVent))
                Object.Destroy(sussifiedVent);

            var hinge = enemyVent.gameObject.transform.Find("Hinge");
            if (hinge == null) return;

            var ventCover = hinge.gameObject.transform.Find("VentCover");
            if (ventCover == null) return;

            var vent = ventCover.gameObject;
            if (vent == null) return;

            if (vent.TryGetComponent(out BoxCollider collider))
                Object.Destroy(collider);

            if (vent.TryGetComponent(out InteractTrigger trigger))
                Object.Destroy(trigger);
        }
        
        public static void EnableVents(bool enable = true)
        {
            Plugin.Log((enable ? "Enabling" : "Disabling") + " vents!");
            foreach (var trigger in ventTrigger)
            {
                trigger.touchTrigger = enable;
                trigger.holdInteraction = enable;
                trigger.isPlayingSpecialAnimation = !enable;
            }
        }

        public static void DisableVents()
        {
            EnableVents(false);
        }
        #endregion


        internal class SussifiedVent : NetworkBehaviour
        {
            #region Properties
            public EnemyVent thisVent { get; set; }
            public EnemyVent siblingVent { get; set; }
            #endregion

            #region Methods
            internal void Start() { }

            internal void TeleportPlayer(PlayerControllerB player)
            {
                if (player == null || player.gameObject == null)
                {
                    Plugin.Log("Can't teleport player. GameObject not found.", Plugin.LogType.Error);
                    return;
                }

                Transform transform = player.gameObject.transform;
                //teleport da playa to dis vent
                if (thisVent != null && siblingVent != null)
                {
                    Plugin.Log("\n⠀⠀⠀⠀⢀⣴⣶⠿⠟⠻⠿⢷⣦⣄⠀⠀⠀\r\n⠀⠀⠀⠀⣾⠏⠀⠀⣠⣤⣤⣤⣬⣿⣷⣄⡀\r\n⠀⢀⣀⣸⡿⠀⠀⣼⡟⠁⠀⠀⠀⠀⠀⠙⣷\r\n⢸⡟⠉⣽⡇⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⢀⣿\r\n⣾⠇⠀⣿⡇⠀⠀⠘⠿⢶⣶⣤⣤⣶⡶⣿⠋\r\n⣿⠂⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠃\r\n⣿⡆⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀\r\n⢿⡇⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠀\r\n⠘⠻⠷⢿⡇⠀⠀⠀⣴⣶⣶⠶⠖⠀⢸⡟⠀\r\n⠀⠀⠀⢸⣇⠀⠀⠀⣿⡇⣿⡄⠀⢀⣿⠇⠀\r\n⠀⠀⠀⠘⣿⣤⣤⣴⡿⠃⠙⠛⠛⠛⠋⠀⠀");
                    if(!thisVent.ventIsOpen || !siblingVent.ventIsOpen)
                        StartCoroutine(OccupyVent());
                    transform.position = siblingVent.floorNode.transform.position;
                }
                else
                {
                    transform.position = new Vector3(7.9186f, 0.286f, -14.1901f);
                }
            }

            private IEnumerator OccupyVent()
            {
                var thisVentOccupied = thisVent.occupied;
                thisVent.OpenVentClientRpc();
                thisVent.occupied = thisVentOccupied;

                yield return new WaitForSeconds(0.2f);

                var siblingVentOccupied = siblingVent.occupied;
                siblingVent.OpenVentClientRpc();

                siblingVent.ventAudio.Play();
                siblingVent.occupied = false; // To skip the audio handling in EnemyVent.Update()

                yield return new WaitForSeconds(1f);
                siblingVent.occupied = siblingVentOccupied;
            }
            #endregion
        }
    }
}
