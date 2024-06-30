using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.components;
using LittleCompany.coroutines;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.HighDefinition;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    internal class ShotgunEventHandler : ItemEventHandler
    {
        ShotgunItem shotgun = null;
        int compatibleAmmoID = -1;
        public override void OnAwake()
        {
            base.OnAwake();
            shotgun = item as ShotgunItem;
            if (shotgun == null) return;

            compatibleAmmoID = shotgun.gunCompatibleAmmoID;
            var main = shotgun.gunShootParticle.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaling(from, to, playerShrunkenBy);

            if (shotgun == null) return;

            shotgun.gunCompatibleAmmoID = compatibleAmmoID + (int)(to * 100);
            shotgun.gunShootParticle.transform.localScale = Vector3.one * to;
        }

        #region Patches
        // Damage
        [HarmonyPatch(typeof(EnemyAI), "HitEnemyOnLocalClient")]
        [HarmonyPrefix]
        public static void HitEnemy(ref int force, PlayerControllerB playerWhoHit)
        {
            if (playerWhoHit?.currentlyHeldObjectServer is ShotgunItem && playerWhoHit.currentlyHeldObjectServer.TryGetComponent(out ItemScaling scaling))
                force = (int)(force * scaling.RelativeScale);
        }
        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayerFromOtherClientServerRpc")]
        [HarmonyPrefix]
        public static void HitPlayer(ref int damageAmount, int playerWhoHit)
        {
            var player = PlayerInfo.ControllerFromID((ulong)playerWhoHit);
            if (player?.currentlyHeldObjectServer is ShotgunItem && player.currentlyHeldObjectServer.TryGetComponent(out ItemScaling scaling))
                damageAmount = (int)(damageAmount * scaling.RelativeScale);
        }

        // Recoil
        [HarmonyPatch(typeof(ShotgunItem), "ShootGun")]
        [HarmonyPostfix]
        public static void ShootGun(ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            if (__instance.playerHeldBy == null || __instance.playerHeldBy != PlayerInfo.CurrentPlayer) return;

            var scalingDiff = ItemModification.ScalingOf(__instance).RelativeScale - PlayerInfo.SizeOf(__instance.playerHeldBy);
            if (scalingDiff <= 0f) return; // Smaller or equally sized relative to the gun

            PlayerThrowAnimation.StartRoutine(__instance.playerHeldBy, shotgunForward * -1f, scalingDiff * 10f);
        }
        #endregion
    }

    internal class ShotgunAmmoEventHandler : ItemEventHandler
    {
        int ammoType = -1;
        public override void OnAwake()
        {
            base.OnAwake();
            ammoType = (item as GunAmmo).ammoType;
        }
        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaling(from, to, playerShrunkenBy);
            (item as GunAmmo).ammoType = ammoType + (int)(to * 100);
        }
    }
}
