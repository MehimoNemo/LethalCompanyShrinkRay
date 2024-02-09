using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

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
            Vents.unsussifyAll();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Uninitialize()
        {
            Vents.unsussifyAll();
        }

        #region Properties
        private static bool sussification = false;
        private static List<InteractTrigger> ventTrigger = new List<InteractTrigger>();
        #endregion

        #region Methods
        private static EnemyVent[] getAllVents()
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
                Plugin.log("Sussification of vents disabled.");
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
                int siblingIndex = vents.Length - i - 1;
                if (siblingIndex == i) // maybe "while" instead of "if"?
                {
                    System.Random rnd = new System.Random();
                    siblingIndex = rnd.Next(0, vents.Length);
                }

                Plugin.log("\tPairing vent " + i + " with vent " + siblingIndex);

                sussify(vents[i], vents[siblingIndex]);
            }

            sussification = true;
            coroutines.RenderVents.StartRoutine(dungeonEntrance);

            if (!PlayerInfo.IsCurrentPlayerShrunk)
                DisableVents();
        }

        public static void sussify(EnemyVent enemyVent, EnemyVent siblingVent)
        {
            GameObject vent;
            try
            {
                vent = enemyVent.gameObject.transform.Find("ventTunnel").gameObject; // Hinge -> VentCover
            }
            catch
            {
                Plugin.log("Vent has no cover to sussify");
                return;
            }

            vent.tag = "InteractTrigger";
            vent.layer = LayerMask.NameToLayer("InteractableObject");
            var sussifiedVent = enemyVent.gameObject.AddComponent<SussifiedVent>();
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
        public static void unsussifyAll()
        {
            Plugin.log("Vents.unsussifyAll");
            foreach (var vent in getAllVents())
                unsussify(vent);

            ventTrigger.Clear();
            sussification = false;
        }

        public static void unsussify(EnemyVent enemyVent)
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
            Plugin.log((enable ? "Enabling" : "Disabling") + " vents!");
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
            public EnemyVent siblingVent { get; set; }
            #endregion

            #region Methods
            internal void Start() { }

            internal void TeleportPlayer(PlayerControllerB player)
            {
                Transform transform = player.gameObject.transform;
                //teleport da playa to dis vent
                if (siblingVent != null)
                {
                    if (PlayerInfo.IsShrunk(player))
                    {
                        Plugin.log("\n⠀⠀⠀⠀⢀⣴⣶⠿⠟⠻⠿⢷⣦⣄⠀⠀⠀\r\n⠀⠀⠀⠀⣾⠏⠀⠀⣠⣤⣤⣤⣬⣿⣷⣄⡀\r\n⠀⢀⣀⣸⡿⠀⠀⣼⡟⠁⠀⠀⠀⠀⠀⠙⣷\r\n⢸⡟⠉⣽⡇⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⢀⣿\r\n⣾⠇⠀⣿⡇⠀⠀⠘⠿⢶⣶⣤⣤⣶⡶⣿⠋\r\n⣿⠂⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠃\r\n⣿⡆⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⠀\r\n⢿⡇⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⠀\r\n⠘⠻⠷⢿⡇⠀⠀⠀⣴⣶⣶⠶⠖⠀⢸⡟⠀\r\n⠀⠀⠀⢸⣇⠀⠀⠀⣿⡇⣿⡄⠀⢀⣿⠇⠀\r\n⠀⠀⠀⠘⣿⣤⣤⣴⡿⠃⠙⠛⠛⠛⠋⠀⠀");
                        //StartCoroutine(OccupyVent());
                        //siblingVent.ventAudio.Play();
                        transform.position = siblingVent.floorNode.transform.position;
                    }
                }
                else
                {
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
            #endregion
        }
    }
}
