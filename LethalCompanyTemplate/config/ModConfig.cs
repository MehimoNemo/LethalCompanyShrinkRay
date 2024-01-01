using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

namespace LCShrinkRay.Config
{
    public sealed class ModConfig
    {
        /*public enum SizeDecrease // Divide 1 by the value
        {
            Half = 2,
            Third = 3,
            Fourth = 4,
            VeryTiny = 10
        }*/

        public ConfigEntry<int> shrinkRayCost;
        public ConfigEntry<float> movementSpeedMultiplier, jumpHeightMultiplier, pitchDistortionIntensity;

        public ConfigEntry<bool> canUseVents, jumpOnShrunkenPlayers, hoardingBugSteal, throwablePlayers, multipleShrinking, thumperOneShot;
        //public ConfigEntry<SizeDecrease> sizeDecrease;

        private static ModConfig instance = null;
        private static readonly object padlock = new object();
        private bool loaded = false;

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
            if( loaded)
                return;

            shrinkRayCost           = Plugin.bepInExConfig().Bind("General", "ShrinkRayCost", 200, "Store cost of the shrink ray");
            //sizeDecrease            = Plugin.bepInExConfig().Bind("General", "SizeDecrease", SizeDecrease.Half, "Defines how tiny shrunken players will become.\"");
            multipleShrinking       = Plugin.bepInExConfig().Bind("General", "MultipleShrinking", true, "If true, a player can shrink multiple times.. unfortunatly.\"");
                                                          
            movementSpeedMultiplier = Plugin.bepInExConfig().Bind("Shrunken", "MovementSpeedMultiplier", 1.5f, new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (slow) to 2 (fast).", new AcceptableValueRange<float>(0.5f, 2f)));
            jumpHeightMultiplier    = Plugin.bepInExConfig().Bind("Shrunken", "JumpHeightMultiplier", 1.5f, new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (lower) to 2 (higher).\"", new AcceptableValueRange<float>(0.5f, 2f)));
            canUseVents             = Plugin.bepInExConfig().Bind("Shrunken", "CanUseVents", true, "If true, shrunken players can move between vents.");
            pitchDistortionIntensity= Plugin.bepInExConfig().Bind("Shrunken", "PitchDistortionIntensity", 0.3f, new ConfigDescription("Intensity of the pitch distortion for shrunken players. 0 is the normal voice and 0.5 is very high.\"", new AcceptableValueRange<float>(0f, 0.5f)));

            jumpOnShrunkenPlayers   = Plugin.bepInExConfig().Bind("Interactions", "JumpOnShrunkenPlayers", true, "If true, normal-sized players can harm shrunken players by jumping on them.");
            throwablePlayers        = Plugin.bepInExConfig().Bind("Interactions", "ThrowablePlayers", true, "If true, shrunken players can be thrown by normal sized players.");
                                                          
            hoardingBugSteal        = Plugin.bepInExConfig().Bind("Enemies", "HoardingBugSteal", true, "If true, hoarding/loot bugs can treat a shrunken player like an item.");
            thumperOneShot          = Plugin.bepInExConfig().Bind("Enemies", "ThumperOneShot", true, "If true, getting hit by a thumper will one-shot shrunken players.");

            loaded = true;
        }
    }
}

