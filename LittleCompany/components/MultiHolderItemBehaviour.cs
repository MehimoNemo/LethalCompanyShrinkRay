using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using System.Collections.Generic;
using Unity.Netcode;

namespace LittleCompany.components
{
    internal class MultiHolderItemBehaviour : NetworkBehaviour
    {
        #region Patches
        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void AddBehaviours()
        {
            foreach(var item in ItemInfo.SpawnableItems)
                item.spawnPrefab.AddComponent<MultiHolderItemBehaviour>();
        }

        [HarmonyPatch(typeof(GrabbableObject), "GrabItem")]
        [HarmonyPostfix()]
        public static void GrabItem(GrabbableObject __instance)
        {
            if (!__instance.TryGetComponent(out MultiHolderItemBehaviour behaviour)) return;

            behaviour.playersHeldBy.Add(__instance.playerHeldBy);
            if (behaviour.playersHeldBy.Count < behaviour.requiredHolderAmount)
            {
                __instance.isHeld = false; // So that others can grab it too
                __instance.EnablePhysics(true);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DiscardHeldObject")]
        [HarmonyPrefix()]
        public static void DiscardHeldObject(PlayerControllerB __instance)
        {
            if (__instance.currentlyHeldObjectServer == null || !__instance.currentlyHeldObjectServer.TryGetComponent(out MultiHolderItemBehaviour behaviour)) return;

            behaviour.playersHeldBy.Remove(__instance);
        }

        [HarmonyPatch(typeof(GrabbableObject), "DiscardItem")]
        [HarmonyPostfix()]
        public static void DiscardItem(GrabbableObject __instance)
        {
            if (!__instance.TryGetComponent(out MultiHolderItemBehaviour behaviour)) return;
        }
        #endregion

        #region Properties
        private int requiredHolderAmount { get; set; } = 1;
        private List<PlayerControllerB> playersHeldBy = new List<PlayerControllerB>();
        #endregion

        #region Base Methods
        void Awake()
        {
            Plugin.Log("MultiHolderItemBehaviour.Awake");
        }

        void Update()
        {
            if (requiredHolderAmount <= 1) return;
        }

        void OnDestroy()
        {

        }
        #endregion

        #region Methods
        public void SetHolderRequired(int amount)
        {
            if(requiredHolderAmount == amount) return;

            requiredHolderAmount = amount;
            // do smth
        }
        #endregion
    }
}
