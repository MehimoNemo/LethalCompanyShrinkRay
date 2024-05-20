using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
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

            var forward = Quaternion.Euler(0f, (0f - __instance.rotationRange) / radius, 0f) * __instance.aimPoint.forward;

            var loweredPosition = __instance.centerPoint.position + Vector3.down * 1.5f;
            float num = __instance.rotationRange / radius * 2f;

            for (int i = 0; i <= 6; i++)
            {
                var lineObject = new GameObject("Line");
                /*var line = lineObject.AddComponent<LineRenderer>();
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.positionCount = 2;
                line.material = Materials.BurntMaterial;
                line.SetPosition(0, loweredPosition);
                line.SetPosition(1, loweredPosition + forward * 30f);*/

                if (Physics.Raycast(new Ray(loweredPosition, forward), out RaycastHit hit, 30f, ToInt([Mask.Player]), QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.TryGetComponent(out PlayerControllerB player))
                        return player;
                }

                forward = Quaternion.Euler(0f, num / 6f, 0f) * forward;
            }

            return null;
        }
    }
}
