using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using System.Collections.Generic;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class TurretPatch
    {
        static List<ulong> FixedTurretNetworkIDs = new List<ulong>();

        [HarmonyPatch(typeof(Turret), "Start")]
        [HarmonyPostfix]
        public static void Start(Turret __instance)
        {
            if (FixedTurretNetworkIDs.Contains(__instance.NetworkObjectId)) return;

            __instance.centerPoint.position += Vector3.down * 1.5f;
            FixedTurretNetworkIDs.Add(__instance.NetworkObjectId);
        }

        [HarmonyPatch(typeof(StartOfRound), "EndOfGame")]
        [HarmonyPrefix()]
        public static void ShipHasLeftPrefix()
        {
            FixedTurretNetworkIDs.Clear();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        public static void Disconnect()
        {
            FixedTurretNetworkIDs.Clear();
        }

        [HarmonyPatch(typeof(Turret), "TurnTowardsTargetIfHasLOS")]
        [HarmonyPostfix()]
        public static void TurnTowardsTargetIfHasLOS(Turret __instance, bool ___hasLineOfSight)
        {
            if (!___hasLineOfSight || PlayerInfo.IsDefaultVanillaSize(__instance.targetPlayerWithRotation)) return;

            var heightDiff = PlayerInfo.SizeOf(__instance.targetPlayerWithRotation) - PlayerInfo.VanillaPlayerSize;
            if (heightDiff > 0f)
                heightDiff *= -1f;

            __instance.tempTransform.position += Vector3.up * heightDiff;
            __instance.turnTowardsObjectCompass.LookAt(__instance.tempTransform);
        }
    }
}
