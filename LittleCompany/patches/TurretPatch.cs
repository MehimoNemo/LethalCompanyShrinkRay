using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static LittleCompany.helper.LayerMasks;

namespace LittleCompany.patches
{
    internal class TurretPatch
    {
        [HarmonyPatch(typeof(Turret), "CheckForPlayersInLineOfSight")]
        [HarmonyPostfix]
        public static PlayerControllerB CheckForPlayersInLineOfSight(PlayerControllerB __result, Turret __instance, float radius = 2f, bool angleRangeCheck = false)
        {
            if (__result != null) return __result;

            // Check lower ranges 0.25 0.5 0.75
            var forward = Quaternion.Euler(0f, (0f - __instance.rotationRange) / radius, 0f) * __instance.aimPoint.forward;

            for (int i = 1; i < 6; i++)
            {
                var adjustedForward = forward + Vector3.down * 0.10f * i;

                /*var lineObject = new GameObject("Line");
                var line = lineObject.AddComponent<LineRenderer>();
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.positionCount = 2;
                line.material = Materials.BurntMaterial;
                line.SetPosition(0, __instance.centerPoint.position);
                line.SetPosition(1, __instance.centerPoint.position + adjustedForward * 30f);*/

                if (Physics.Raycast(new Ray(__instance.centerPoint.position, adjustedForward), out RaycastHit hit, 30f, ToInt([Mask.Player]), QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.TryGetComponent(out PlayerControllerB player))
                        return player;
                }
            }

            return null;
        }
    }
}
