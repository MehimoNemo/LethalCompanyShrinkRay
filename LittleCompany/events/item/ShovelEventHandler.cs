using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;
using static UnityEngine.GraphicsBuffer;

namespace LittleCompany.events.item
{
    [HarmonyPatch]
    internal class ShovelEventHandler : ItemEventHandler
    {
        int shovelHitDefaultForce = 1;
        Shovel shovel = null;

        public override void OnAwake()
        {
            base.OnAwake();
            shovel = item as Shovel;
            if(shovel != null)
                shovelHitDefaultForce = shovel.shovelHitForce;
        }

        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaling(from, to, playerShrunkenBy);
            
            if (shovel != null)
                shovel.shovelHitForce = Mathf.Max((int)(shovelHitDefaultForce * to * 2), 0);

            // Handanimation
            //shovel.itemProperties.rotationOffset = shovel.itemProperties.twoHandedAnimation ? Vector3.zero : new Vector3(-120f, -90f, 0f);
        }

        [HarmonyPatch(typeof(Shovel), "HitShovel")]
        [HarmonyPostfix]
        public static void HitShovel(bool cancel, RaycastHit[] ___objectsHitByShovel, Shovel __instance)
        {
            if (cancel) return;

            Plugin.Log("Shovel hitforce: " + __instance.shovelHitForce);
            foreach (var obj in ___objectsHitByShovel)
            {
                if (obj.transform != null && obj.transform.TryGetComponent(out GrabbablePlayerObject gpo))
                {
                    if (__instance.playerHeldBy == null || __instance.playerHeldBy.playerClientId != gpo.grabbedPlayerID.Value)
                        gpo.OnGoombaServerRpc(gpo.grabbedPlayerID.Value);
                }
            }
        }
    }
}
