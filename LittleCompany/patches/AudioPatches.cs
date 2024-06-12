using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.compatibility;
using LittleCompany.Config;
using LittleCompany.helper;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LittleCompany.patches
{
    internal class AudioPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
        [HarmonyPostfix]
        public static void SetPlayerVoiceFilters()
        {
            if (SoundManager.Instance == null) return;

            foreach (var pcb in PlayerInfo.AllPlayers)
                AdjustPitchIntensityOf(pcb);
        }

        public static void AdjustPitchIntensityOf(PlayerControllerB player)
        {
            float playerScale = PlayerInfo.SizeOf(player);
            float intensity = (float)ModConfig.Instance.values.pitchDistortionIntensity;
            if(intensity != 0)
            {
                float modifiedPitch = (float)(-1f * intensity * (playerScale - PlayerInfo.CurrentPlayerScale) + 1f);
                SoundManager.Instance.playerVoicePitchTargets[player.playerClientId] = modifiedPitch;
                SoundManager.Instance.SetPlayerPitch(modifiedPitch, (int)player.playerClientId);
                MoreCompanyAudioCompatibilityPatch.CompatUpdatePitchInAudioMixers(player.playerClientId, modifiedPitch);
            }
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnEnemyGameObject")]
        [HarmonyPostfix]
        public static NetworkObjectReference SpawnEnemyGameObject(NetworkObjectReference __result)
        {
            if (__result.TryGet(out NetworkObject networkObject))
            {
                var enemyAI = networkObject.GetComponentInParent<EnemyAI>();
                if (enemyAI != null)
                    AdjustPitchIntensityOf(enemyAI);
            }

            return __result;
        }

        public static void UpdateEnemyPitches()
        {
            foreach(var enemy in RoundManager.Instance.SpawnedEnemies)
                AdjustPitchIntensityOf(enemy);
        }

        public static void AdjustPitchIntensityOf(EnemyAI enemyAI)
        {
            if (PlayerInfo.CurrentPlayer == null || enemyAI == null) return;

            var sizeDifference = PlayerInfo.CurrentPlayerScale - EnemyInfo.SizeOf(enemyAI);
            var pitchIntensity = ModConfig.Instance.values.enemyPitchDistortionIntensity;
            var pitch = Mathf.Clamp(1f + (sizeDifference * pitchIntensity), 0.5f, 1.5f);

            foreach (var audioSource in enemyAI.GetComponents<AudioSource>())
                audioSource.pitch = pitch;

            foreach (var audioSource in enemyAI.GetComponentsInChildren<AudioSource>())
                audioSource.pitch = pitch;

            if (enemyAI.creatureVoice != null) enemyAI.creatureVoice.pitch = pitch;
            if (enemyAI.creatureSFX != null) enemyAI.creatureSFX.pitch = pitch;
        }
    }
}
