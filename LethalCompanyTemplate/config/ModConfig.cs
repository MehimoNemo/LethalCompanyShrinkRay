using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using BepInEx.Configuration;
using HarmonyLib;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;
using Unity.Netcode;
using GameNetcodeStuff;
using static UnityEngine.InputSystem.InputRemoting;

namespace LCShrinkRay.Config
{
    public enum ThumperBehaviour
    {
        Default,
        OneShot,
        Bumper
    }

    public struct ConfigValues
    {

        // Mark client-sided options with [JsonIgnore] to ignore them when requesting host config
        public bool friendlyFlight { get; set; }

        public bool sellablePlayers { get; set; }

        public int shrinkRayCost { get; set; }

        public float movementSpeedMultiplier { get; set; }

        public float jumpHeightMultiplier { get; set; }

        public float weightMultiplier { get; set; }

        [JsonIgnore]
        public float pitchDistortionIntensity { get; set; }

        public bool canUseVents { get; set; }

        public bool jumpOnShrunkenPlayers { get; set; }

        public bool hoardingBugSteal { get; set; }

        public bool throwablePlayers { get; set; }

        public bool CanEscapeGrab { get; set; }

        public bool multipleShrinking { get; set; }

        public ThumperBehaviour thumperBehaviour { get; set; }
    }

    public sealed class ModConfig
    {
        private static ModConfig instance = null;
        private static readonly object padlock = new object();
        public static bool debugMode { get; set; }

        public ConfigValues values = new ConfigValues();

        ModConfig()
        {
        }

        public static ModConfig Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new ModConfig();

                    return instance;
                }
            }
        }

        public void setup()
        {
            values.shrinkRayCost            = Plugin.bepInExConfig().Bind("General", "ShrinkRayCost", 0, "Store cost of the shrink ray").Value;
            values.multipleShrinking        = Plugin.bepInExConfig().Bind("General", "MultipleShrinking", true, "If true, a player can shrink multiple times.. unfortunatly.").Value;

            values.movementSpeedMultiplier  = Plugin.bepInExConfig().Bind("Shrunken", "MovementSpeedMultiplier", 1.5f, new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (slow) to 2 (fast).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.jumpHeightMultiplier     = Plugin.bepInExConfig().Bind("Shrunken", "JumpHeightMultiplier", 1.5f, new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (lower) to 2 (higher).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.weightMultiplier         = Plugin.bepInExConfig().Bind("Shrunken", "WeightMultiplier", 1.5f, new ConfigDescription("Weight multiplier for shrunken players, ranging from 0.5 (lighter) to 2 (heavier).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.canUseVents              = Plugin.bepInExConfig().Bind("Shrunken", "CanUseVents", true, "If true, shrunken players can move between vents.").Value;
            values.pitchDistortionIntensity = Plugin.bepInExConfig().Bind("Shrunken", "PitchDistortionIntensity", 0.3f, new ConfigDescription("Intensity of the pitch distortion for shrunken players. 0 is the normal voice and 0.5 is very high.", new AcceptableValueRange<float>(0f, 0.5f))).Value;
            values.CanEscapeGrab            = Plugin.bepInExConfig().Bind("Shrunken", "CanEscapeGrab", true, "If true, a player who got grabbed can escape by jumping").Value;

            values.jumpOnShrunkenPlayers    = Plugin.bepInExConfig().Bind("Interactions", "JumpOnShrunkenPlayers", true, "If true, normal-sized players can harm shrunken players by jumping on them.").Value;
            values.throwablePlayers         = Plugin.bepInExConfig().Bind("Interactions", "ThrowablePlayers", true, "If true, shrunken players can be thrown by normal sized players.").Value;
            values.friendlyFlight           = Plugin.bepInExConfig().Bind("Interactions", "FriendlyFlight", true, "If true, held players can grab other players, causing comedic, but game breaking effects.").Value;
            values.sellablePlayers          = Plugin.bepInExConfig().Bind("Interactions", "sellablePlayers", true, "If true, held players can sell other players, causing comedic").Value;


            values.hoardingBugSteal         = Plugin.bepInExConfig().Bind("Enemies", "HoardingBugSteal", true, "If true, hoarding/loot bugs can treat a shrunken player like an item.").Value;
            values.thumperBehaviour         = Plugin.bepInExConfig().Bind("Enemies", "ThumperBehaviour", ThumperBehaviour.Default, "Defines the way Thumpers react on shrunken players.").Value;

            string json = JsonConvert.SerializeObject(ModConfig.Instance.values);
            Plugin.log("Using config:" + json);
        }

        public void updated()
        {
            // TODO: reload things if needed
        }

        [HarmonyPatch]
        public class SyncHandshake
        {
            [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
            [HarmonyPostfix]
            public static void Initialize()
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    Plugin.log("Current player is the host.");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(PluginInfo.PLUGIN_NAME + "_HostConfigRequested", new HandleNamedMessageDelegate(HostConfigRequested));
                }
                else
                {
                    Plugin.log("Current player is not the host.");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(PluginInfo.PLUGIN_NAME + "_HostConfigReceived", new HandleNamedMessageDelegate(HostConfigReceived));
                    RequestHostConfig();
                }
            }

            public static void RequestHostConfig()
            {
                if (NetworkManager.Singleton.IsClient)
                {
                    Plugin.log("Sending config request to host.");
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PluginInfo.PLUGIN_NAME + "_HostConfigRequested", 0uL, new FastBufferWriter(0, Allocator.Temp), NetworkDelivery.ReliableSequenced);
                }
                else
                    Plugin.log("Config request not required. No other player available."); // Shouldn't happen, but who knows..
            }

            public static void HostConfigRequested(ulong clientId, FastBufferReader reader)
            {
                if (!NetworkManager.Singleton.IsServer) // Current player is not the host and therefor not the one who should react
                    return;

                string json = JsonConvert.SerializeObject(ModConfig.Instance.values);
                Plugin.log("Client [" + clientId + "] requested host config. Sending own config: " + json);

                int writeSize = FastBufferWriter.GetWriteSize(json);
                using FastBufferWriter writer = new FastBufferWriter(writeSize, Allocator.Temp);
                writer.WriteValueSafe(json);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PluginInfo.PLUGIN_NAME + "_HostConfigReceived", clientId, writer, NetworkDelivery.ReliableSequenced);
            }

            public static void HostConfigReceived(ulong clientId, FastBufferReader reader)
            {
                reader.ReadValueSafe(out string json);
                Plugin.log("Received host config: " + json);
                ConfigValues hostValues = JsonConvert.DeserializeObject<ConfigValues>(json);

                ModConfig.Instance.values = hostValues;
                ModConfig.Instance.updated();
            }
        }
    }
}

