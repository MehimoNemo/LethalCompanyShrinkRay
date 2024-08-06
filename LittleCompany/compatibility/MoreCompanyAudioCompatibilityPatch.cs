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

        static Dictionary<ulong, List<AudioMixerGroup>> listOfVoicePlayer = new Dictionary<ulong, List<AudioMixerGroup>>();

        //We joined a lobby
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject()
        {
            listOfVoicePlayer.Clear();
            foreach (AudioMixerGroup a in Resources.FindObjectsOfTypeAll(typeof(AudioMixerGroup)).Cast<AudioMixerGroup>())
            {
                if (a.name.Contains("VoicePlayer"))
                {
                    var playerIDText = a.name.Replace("VoicePlayer", "");
                    var playerID = ulong.Parse(playerIDText.Length > 0 ? playerIDText : "0");
                    if(listOfVoicePlayer.ContainsKey(playerID))
                        listOfVoicePlayer[playerID].Add(a);
                    else
                        listOfVoicePlayer.Add(playerID, [a]);
                }
            }
        }

        public static void CompatUpdatePitchInAudioMixers(ulong playerClientId, float pitch)
        {
            if (compatEnabled && listOfVoicePlayer.ContainsKey(playerClientId))
            {
                var voicePlayer = listOfVoicePlayer[playerClientId];
                if (voicePlayer == null) return;

                for (int i = voicePlayer.Count - 1; i >= 0; i--)
                {
                    if (voicePlayer[i]?.audioMixer == null || !voicePlayer[i].audioMixer.SetFloat("pitch", pitch))
                        voicePlayer.RemoveAt(i);
                }
            }
        }

        [HarmonyPatch(typeof(AudioMixer), "SetFloat")]
        [HarmonyPrefix]
        public static bool SetFloatPrefix(string name, float value)
        {
            if (name.StartsWith("PlayerVolume") || name.StartsWith("PlayerPitch"))
            {
                string cutName = name.Replace("PlayerVolume", "").Replace("PlayerPitch", "");
                int playerObjectNumber = int.Parse(cutName);
                if (playerObjectNumber < 4) return true;

                PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObjectNumber];
                if (playerControllerB != null)
                {
                    AudioSource voiceSource = playerControllerB.currentVoiceChatAudioSource;
                    if (voiceSource)
                    {
                        if (name.StartsWith("PlayerVolume"))
                        {
                            voiceSource.volume = value;
                        }
                        else if (name.StartsWith("PlayerPitch"))
                        {
                            voiceSource.pitch = value;
                        }
                    }

                    return false;
                }
            }

            return true;
        }
        
    }
}
