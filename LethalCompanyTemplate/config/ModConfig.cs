﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using BepInEx.Configuration;
using HarmonyLib;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;
using Unity.Netcode;
using GameNetcodeStuff;

namespace LCShrinkRay.Config
{
    public enum ThumperBehaviour
    {
        Default,
        OneShot,
        Bounce
    }

    public struct ConfigValues
    {
        // Mark client-sided options with [JsonIgnore] to ignore them when requesting host config
        public int shrinkRayCost { get; internal set; }

        public float movementSpeedMultiplier { get; internal set; }

        public float jumpHeightMultiplier { get; internal set; }

        [JsonIgnore]
        public float pitchDistortionIntensity { get; internal set; }

        [JsonIgnore]
        public bool canUseVents { get; internal set; }

        public bool jumpOnShrunkenPlayers { get; internal set; }

        public bool hoardingBugSteal { get; internal set; }

        public bool throwablePlayers { get; internal set; }

        public bool multipleShrinking { get; internal set; }

        public ThumperBehaviour thumperBehaviour { get; internal set; }
    }

    public sealed class ModConfig
    {
        private static ModConfig instance = null;
        private static readonly object padlock = new object();
        private bool loaded = false;

        private ConfigValues values = new ConfigValues();
        public void setConfigValues(ConfigValues newValues)
        {
            values = newValues;
            updated();
        }

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

        public void load()
        {
            values.shrinkRayCost            = Plugin.bepInExConfig().Bind("General", "ShrinkRayCost", 200, "Store cost of the shrink ray").Value;
            //sizeDecrease                  = Plugin.bepInExConfig().Bind("General", "SizeDecrease", SizeDecrease.Half, "Defines how tiny shrunken players will become.\"").Value;
            values.multipleShrinking        = Plugin.bepInExConfig().Bind("General", "MultipleShrinking", true, "If true, a player can shrink multiple times.. unfortunatly.\"").Value;

            values.movementSpeedMultiplier  = Plugin.bepInExConfig().Bind("Shrunken", "MovementSpeedMultiplier", 1.5f, new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (slow) to 2 (fast).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.jumpHeightMultiplier     = Plugin.bepInExConfig().Bind("Shrunken", "JumpHeightMultiplier", 1.5f, new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (lower) to 2 (higher).\"", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.canUseVents              = Plugin.bepInExConfig().Bind("Shrunken", "CanUseVents", true, "If true, shrunken players can move between vents.").Value;
            values.pitchDistortionIntensity = Plugin.bepInExConfig().Bind("Shrunken", "PitchDistortionIntensity", 0.3f, new ConfigDescription("Intensity of the pitch distortion for shrunken players. 0 is the normal voice and 0.5 is very high.\"", new AcceptableValueRange<float>(0f, 0.5f))).Value;

            values.jumpOnShrunkenPlayers    = Plugin.bepInExConfig().Bind("Interactions", "JumpOnShrunkenPlayers", true, "If true, normal-sized players can harm shrunken players by jumping on them.").Value;
            values.throwablePlayers         = Plugin.bepInExConfig().Bind("Interactions", "ThrowablePlayers", true, "If true, shrunken players can be thrown by normal sized players.").Value;
                                                          
            values.hoardingBugSteal         = Plugin.bepInExConfig().Bind("Enemies", "HoardingBugSteal", true, "If true, hoarding/loot bugs can treat a shrunken player like an item.").Value;
            values.thumperBehaviour         = Plugin.bepInExConfig().Bind("Enemies", "ThumperOneShot", ThumperBehaviour.Default, "If true, getting hit by a thumper will one-shot shrunken players.").Value;
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
                    Plugin.log("Config request not required. No other player available.", Plugin.LogType.Info); // Shouldn't happen, but who knows..
            }

            public static void HostConfigRequested(ulong clientId, FastBufferReader reader)
            {
                if (!NetworkManager.Singleton.IsServer) // Current player is not the host and therefor not the one who should react
                    return;

                string json = JsonSerializer.Serialize(ModConfig.Instance.values);
                Plugin.log("Client [" + clientId + "] requested host config. Sending own config: " + json);

                using FastBufferWriter writer = new FastBufferWriter(json.Length, Allocator.Temp);
                writer.WriteValueSafe(json);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PluginInfo.PLUGIN_NAME + "_HostConfigReceived", clientId, writer, NetworkDelivery.ReliableSequenced);
            }

            public static void HostConfigReceived(ulong clientId, FastBufferReader reader)
            {
                if (!reader.TryBeginRead(4))
                {
                    Plugin.log("Error while trying to receive host config", Plugin.LogType.Error);
                    return;
                }

                reader.ReadValueSafe(out string json);
                Plugin.log("Received host config: " + json);
                ConfigValues hostValues = JsonSerializer.Deserialize<ConfigValues>(json);
                ModConfig.Instance.setConfigValues(hostValues);
            }
        }
    }
}

