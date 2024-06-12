using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace LittleCompany.compatibility
{
    [HarmonyPatch]
    internal class MoreCompanyAudioCompatibilityPatch
    {
        public const string MoreCompanyReferenceChain = "me.swipez.melonloader.morecompany";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MoreCompanyReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        static List<AudioMixerGroup> listOfVoicePlayer;

        //We joined a lobby
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            listOfVoicePlayer = [];
            foreach (AudioMixerGroup a in Resources.FindObjectsOfTypeAll(typeof(AudioMixerGroup)).Cast<AudioMixerGroup>())
            {
                if (a.name.Contains("VoicePlayer"))
                {
                    listOfVoicePlayer.Add(a);
                }
            }
        }

        public static void CompatUpdatePitchInAudioMixers(ulong playerClientId, float pitch)
        {
            if (compatEnabled)
            {
                foreach (AudioMixerGroup voicePlayer in listOfVoicePlayer)
                {
                    if (voicePlayer != null && voicePlayer.name == "VoicePlayer" + playerClientId)
                    {
                        voicePlayer.audioMixer.SetFloat("pitch", pitch);
                    }
                }
            }
        }
    }
}
