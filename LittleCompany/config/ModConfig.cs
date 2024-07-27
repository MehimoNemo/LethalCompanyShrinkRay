using Newtonsoft.Json;
using BepInEx.Configuration;
using HarmonyLib;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;
using Unity.Netcode;
using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.compatibility;
using UnityEngine;
using LittleCompany.patches;

namespace LittleCompany.Config
{
    public sealed class ModConfig
    {
        #region Properties
        internal static readonly float SmallestSizeChange = 0.05f;
        public static bool DebugLog { get; set; }

        public enum ThumperBehaviour
        {
            Default,
            OneShot,
            Bumper
        }

        public enum HoardingBugBehaviour
        {
            Default,
            NoGrab,
            Addicted
        }

        public struct ConfigValues
        {
            // Mark client-sided options with [JsonIgnore] to ignore them when requesting host config
            public bool sellablePlayers { get; set; }

            public int shrinkRayCost { get; set; }

            public int shrinkRayShotsPerCharge { get; set; }

            public bool shrinkRayNoRecharge { get; set; }

            public bool logicalMultiplier { get; set; }

            public float movementSpeedMultiplier { get; set; }

            public float jumpHeightMultiplier { get; set; }

            public float weightMultiplier { get; set; }

            [JsonIgnore]
            public float pitchDistortionIntensity { get; set; }

            public float enemyPitchDistortionIntensity { get; set; }

            public bool canUseVents { get; set; }

            public bool jumpOnShrunkenPlayers { get; set; }

            public bool throwablePlayers { get; set; }

            public bool CanEscapeGrab { get; set; }

            public bool deathShrinking { get; set; }

            public float playerSizeChangeStep { get; set; }

            public bool playerSizeStopAtDefault { get; set; }

            public float itemSizeChangeStep { get; set; }

            public float itemSizeChangeSpeed { get; set; }

            public bool itemGlassify { get; set; }

            public float shipObjectSizeChangeStep { get; set; }

            public float vehicleSizeChangeStep { get; set; }

            public bool itemScalingVisualOnly { get; set; }

            public float enemySizeChangeStep { get; set; }

            public float maximumPlayerSize { get; set; }

            public float defaultPlayerSize { get; set; }

            public bool cantOpenStorageCloset { get; set; }

            public int deathShrinkEventChance { get; set; }

            public ThumperBehaviour thumperBehaviour { get; set; }

            public HoardingBugBehaviour hoardingBugBehaviour { get; set; }

            // Potions
            public int shrinkPotionStorePrice { get; set; }
            public int shrinkPotionScrapRarity { get; set; }

            public int enlargePotionStorePrice { get; set; }
            public int enlargePotionScrapRarity { get; set; }

            // Experimental

            public bool useLethalLevelLoaderForItemRegistration { get; set; }
        }

        public ConfigValues values = new ConfigValues();

        private static ModConfig instance = null;
        public static ModConfig Instance
        {
            get
            {
                if (instance == null)
                    instance = new ModConfig();

                return instance;
            }
        }
        #endregion

        #region Methods
        public void Setup()
        {
            values.shrinkRayCost                 = Plugin.BepInExConfig().Bind("General", "ShrinkRayCost", 1000, "Store cost of the shrink ray.").Value;
            values.shrinkRayShotsPerCharge       = Plugin.BepInExConfig().Bind("General", "ShrinkRayShotsPerCharge", 7, "Amount of shots per charge for the shrink ray. Set to 0 for unlimited.").Value;
            values.shrinkRayNoRecharge           = Plugin.BepInExConfig().Bind("General", "ShrinkRayNoRecharge", false, "If true, the shrink ray can't be recharged and will overheat once battery is at zero.").Value;
            values.deathShrinking                = Plugin.BepInExConfig().Bind("General", "DeathShrinking", false, "If true, a player can be shrunk below 0.2f, resulting in an instant death.").Value;
            values.logicalMultiplier             = Plugin.BepInExConfig().Bind("General", "LogicalMultiplier", false, new ConfigDescription("If true, jump height & speed multiplier will be set according to what would be logical for the given player size. This overrides MovementSpeedMultiplier and JumpHeightMultiplier.")).Value;
                                                 
            values.defaultPlayerSize             = Plugin.BepInExConfig().Bind("Sizing", "DefaultPlayerSize", 1f, new ConfigDescription("The default player size when joining a lobby or reviving.", new AcceptableValueRange<float>(0.2f, 1.7f))).Value;
            values.maximumPlayerSize             = Plugin.BepInExConfig().Bind("Sizing", "MaximumPlayerSize", 1.7f, "Defines, how tall a player can become (1.7 is the last fitting height for the ship inside and doors!)").Value;
            values.playerSizeChangeStep          = Plugin.BepInExConfig().Bind("Sizing", "PlayerSizeChangeStep", 0.4f, new ConfigDescription("Defines how much a player shrinks/enlarges in one step (>0.8 will instantly shrink to death if DeathShrinking is on, otherwise fail!).", new AcceptableValueRange<float>(SmallestSizeChange, 10f))).Value;
            values.playerSizeStopAtDefault       = Plugin.BepInExConfig().Bind("Sizing", "PlayerSizeStopAtDefault", false, "If true, when scaling would go through the DefaultPlayerSize it will stop there instead.").Value;
            values.itemSizeChangeStep            = Plugin.BepInExConfig().Bind("Sizing", "ItemSizeChangeStep", 0.4f, new ConfigDescription("Defines how much an item shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f))).Value;
            values.itemSizeChangeSpeed           = Plugin.BepInExConfig().Bind("Sizing", "ItemSizeChangeSpeed", 1f, new ConfigDescription("Defines how fast an item shrinks/enlarges.", new AcceptableValueRange<float>(0.1f, 1000f))).Value;
            values.itemScalingVisualOnly         = Plugin.BepInExConfig().Bind("Sizing", "ItemScalingVisualOnly", false, "If true, scaling items has no special effects.").Value;
            values.itemGlassify = Plugin.BepInExConfig().Bind("Sizing", "ItemGlassify", true, "If true, items you are holding will be translucent when they are big and covering too much of your screen.").Value;
            values.shipObjectSizeChangeStep      = Plugin.BepInExConfig().Bind("Sizing", "ShipObjectSizeChangeStep", 0.2f, new ConfigDescription("Defines how much a ship object shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f))).Value;
            values.vehicleSizeChangeStep         = Plugin.BepInExConfig().Bind("Sizing", "VehicleSizeChangeStep", 0.2f, new ConfigDescription("Defines how much a vehicle shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f))).Value;
            values.enemySizeChangeStep           = Plugin.BepInExConfig().Bind("Sizing", "EnemySizeChangeStep", 0.5f, new ConfigDescription("Defines how much an enemy shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f))).Value;
            
            values.movementSpeedMultiplier       = Plugin.BepInExConfig().Bind("Shrunken", "MovementSpeedMultiplier", 1.3f, new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (very slow) to 1.5 (very fast).", new AcceptableValueRange<float>(0.5f, 1.5f))).Value;
            values.jumpHeightMultiplier          = Plugin.BepInExConfig().Bind("Shrunken", "JumpHeightMultiplier", 1.3f, new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (very low) to 2 (very high).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.weightMultiplier              = Plugin.BepInExConfig().Bind("Shrunken", "WeightMultiplier", 1.5f, new ConfigDescription("Weight multiplier on held items for shrunken players, ranging from 0.5 (lighter) to 2 (heavier).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.canUseVents                   = Plugin.BepInExConfig().Bind("Shrunken", "CanUseVents", true, "If true, shrunken players can move between vents.").Value;
            values.pitchDistortionIntensity      = Plugin.BepInExConfig().Bind("Shrunken", "PitchDistortionIntensity", 0.3f, new ConfigDescription("Intensity of the pitch distortion for players with a different size than the local player, from 0 (unchanged) to 0.5 (strong).", new AcceptableValueRange<float>(0f, 0.5f))).Value;
            values.CanEscapeGrab                 = Plugin.BepInExConfig().Bind("Shrunken", "CanEscapeGrab", true, "If true, a player who got grabbed can escape by jumping.").Value;
            values.cantOpenStorageCloset         = Plugin.BepInExConfig().Bind("Shrunken", "CantOpenStorageCloset", false, "If true, a shrunken player can't open or close the storage closet anymore.").Value;
                                                 
            values.jumpOnShrunkenPlayers         = Plugin.BepInExConfig().Bind("Interactions", "JumpOnShrunkenPlayers", true, "If true, normal-sized players can harm shrunken players by jumping on them.").Value;
            values.throwablePlayers              = Plugin.BepInExConfig().Bind("Interactions", "ThrowablePlayers", true, "If true, shrunken players can be thrown by normal sized players.").Value;
            values.sellablePlayers               = Plugin.BepInExConfig().Bind("Interactions", "SellablePlayers", true, "If true, shrunken players can be sold to the company.").Value;

            values.enemyPitchDistortionIntensity = Plugin.BepInExConfig().Bind("Enemies", "EnemyPitchDistortionIntensity", 0.2f, new ConfigDescription("Intensity of the pitch distortion for enemies with a different size than the local player, from 0 (unchanged) to 0.5 (strong).", new AcceptableValueRange<float>(0f, 0.5f))).Value;
            values.deathShrinkEventChance        = Plugin.BepInExConfig().Bind("Enemies", "DeathShrinkEventChance", 100, new ConfigDescription("Chance for an event when shrinking an enemy to death.", new AcceptableValueRange<int>(0, 100))).Value;
            values.hoardingBugBehaviour          = Plugin.BepInExConfig().Bind("Enemies", "HoarderBugBehaviour", HoardingBugBehaviour.Default, "Defines if hoarding bugs should be able to grab you and how likely that is.").Value;
            values.thumperBehaviour              = Plugin.BepInExConfig().Bind("Enemies", "ThumperBehaviour", ThumperBehaviour.Bumper, "Defines the way Thumpers react on shrunken players.").Value;
                                                 
            values.shrinkPotionStorePrice        = Plugin.BepInExConfig().Bind("Potions", "ShrinkPotionShopPrice", 30, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500))).Value;
            values.shrinkPotionScrapRarity       = Plugin.BepInExConfig().Bind("Potions", "ShrinkPotionScrapRarity", 10, new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100))).Value;
            values.enlargePotionStorePrice       = Plugin.BepInExConfig().Bind("Potions", "EnlargePotionStorePrice", 50, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500))).Value;
            values.enlargePotionScrapRarity      = Plugin.BepInExConfig().Bind("Potions", "EnlargePotionScrapRarity", 5, new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100))).Value;
                                    
            values.useLethalLevelLoaderForItemRegistration = Plugin.BepInExConfig().Bind("Experimental", "UseLethalLevelLoaderForItemRegistration", false, new ConfigDescription("Allow LittleCompany to use LethalLevelLoader for item registration instead of LethalLib.")).Value;

            DebugLog                             = Plugin.BepInExConfig().Bind("Beta-only", "DebugLog", false, "Additional logging to help identifying issues of this mod.").Value;

#if DEBUG
            Plugin.Log("Initial config: " + JsonConvert.SerializeObject(Instance.values));
#endif
            FixWrongEntries();
        }

        public void FixWrongEntries()
        {
            values.maximumPlayerSize = Mathf.Max(values.maximumPlayerSize, values.defaultPlayerSize);
        }
        #endregion

        [HarmonyPatch]
        public class SyncHandshake
        {
            #region Constants
            private const string REQUEST_MESSAGE = PluginInfo.PLUGIN_NAME + "_" + "HostConfigRequested";
            private const string RECEIVE_MESSAGE = PluginInfo.PLUGIN_NAME + "_" + "HostConfigReceived";
            #endregion

            #region Networking
            [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
            [HarmonyPostfix]
            public static void Initialize()
            {
                if (PlayerInfo.IsHost)
                {
                    Plugin.Log("Current player is the host.");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(REQUEST_MESSAGE, new HandleNamedMessageDelegate(HostConfigRequested));
                }
                else
                {
                    Plugin.Log("Current player is not the host.");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RECEIVE_MESSAGE, new HandleNamedMessageDelegate(HostConfigReceived));
                    RequestHostConfig();
                }
            }

            public static void RequestHostConfig()
            {
                if (NetworkManager.Singleton.IsClient)
                {
                    Plugin.Log("Sending config request to host.");
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(REQUEST_MESSAGE, 0uL, new FastBufferWriter(0, Allocator.Temp), NetworkDelivery.ReliableSequenced);
                }
                else
                    Plugin.Log("Config request not required. No other player available."); // Shouldn't happen, but who knows..
            }

            public static void HostConfigRequested(ulong clientId, FastBufferReader reader)
            {
                if (!PlayerInfo.IsHost) // Current player is not the host and therefor not the one who should react
                    return;

                string json = JsonConvert.SerializeObject(Instance.values);
                Plugin.Log("Client [" + clientId + "] requested host config. Sending own config: " + json);

                int writeSize = FastBufferWriter.GetWriteSize(json);
                using FastBufferWriter writer = new FastBufferWriter(writeSize, Allocator.Temp);
                writer.WriteValueSafe(json);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RECEIVE_MESSAGE, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
            }

            public static void HostConfigReceived(ulong clientId, FastBufferReader reader)
            {
                reader.ReadValueSafe(out string json);
                Plugin.Log("Received host config: " + json);
                ConfigValues hostValues = JsonConvert.DeserializeObject<ConfigValues>(json);

                // Adjust client-sided options (WIP -> replace later with e.g. custom JsonConverter)
                hostValues.pitchDistortionIntensity = Instance.values.pitchDistortionIntensity;
                hostValues.enemyPitchDistortionIntensity = Instance.values.enemyPitchDistortionIntensity;

                PlayerCosmetics.RegularizeAllCosmeticsWhenLoading();

                Instance.values = hostValues;
                Instance.FixWrongEntries();

                PlayerCountChangeDetection.ConfigSyncedOnConnect();
            }
            #endregion
        }
    }
}

