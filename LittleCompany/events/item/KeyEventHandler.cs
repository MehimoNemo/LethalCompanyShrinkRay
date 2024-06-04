using DunGen;
using HarmonyLib;
using LittleCompany.helper;
using LittleCompany.modifications;
using System;
using System.Collections.Generic;
using System.Text;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    [HarmonyPatch]
    internal class KeyEventHandler : ItemEventHandler
    {
        internal static readonly string SCALED_TIP = "Scaling it was not the key to success.. it won't fit.";

        [HarmonyPatch(typeof(DoorLock), "Update")]
        [HarmonyPostfix()]
        public static void Update(DoorLock __instance, InteractTrigger ___doorTrigger)
        {
            SetUnableToUnlockIfNeeded(__instance, ___doorTrigger);
        }

        [HarmonyPatch(typeof(DoorLock), "LockDoor")]
        [HarmonyPostfix()]
        public static void LockDoor(DoorLock __instance, InteractTrigger ___doorTrigger)
        {
            SetUnableToUnlockIfNeeded(__instance, ___doorTrigger);
        }

        internal static void SetUnableToUnlockIfNeeded(DoorLock door, InteractTrigger doorTrigger)
        {
            if (!door.isLocked) return;

            var heldItem = PlayerInfo.CurrentPlayer.currentlyHeldObjectServer;
            if (heldItem == null || heldItem.itemProperties.itemId != 14) return;

            if (ObjectModification.ScalingOf(heldItem).Unchanged) return;

            doorTrigger.interactable = false;
            doorTrigger.hoverTip = SCALED_TIP;
            doorTrigger.disabledHoverTip = SCALED_TIP;
        }

        [HarmonyPatch(typeof(KeyItem), "ItemActivate")]
        [HarmonyPrefix()]
        public static bool ItemActivate(KeyItem __instance)
        {
            if (!ObjectModification.ScalingOf(__instance).Unchanged)
                return false; // Skip original method

            return true;
        }
    }
}
