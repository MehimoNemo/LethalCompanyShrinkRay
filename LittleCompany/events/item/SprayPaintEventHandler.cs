using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.modifications;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    internal class SprayPaintEventHandler : ItemEventHandler
    {
        // Sadly not working
        /*public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaling(from, to, playerShrunkenBy);
            (item as SprayPaintItem).sprayPaintPrefab.GetComponent<DecalProjector>().size = Vector3.one * to;
        }*/

        [HarmonyPatch(typeof(SprayPaintItem), "AddSprayPaintLocal")]
        [HarmonyPostfix]
        public static bool AddSprayPaintLocal(bool __result, SprayPaintItem __instance, ref DecalProjector ___delayedSprayPaintDecal)
        {
            if (!__result) return false;

            var scaling = ItemModification.ScalingOf(__instance);
            if (!scaling.Unchanged)
                ___delayedSprayPaintDecal.size *= Mathf.Max(1f + (scaling.RelativeScale - 1f) * 0.5f, 0.1f);

            return true;
        }
    }
}
