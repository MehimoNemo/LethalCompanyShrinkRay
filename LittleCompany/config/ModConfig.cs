using Newtonsoft.Json;
using BepInEx.Configuration;
using HarmonyLib;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;
using Unity.Netcode;
using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.components;
using LittleCompany.compatibility;

namespace LittleCompany.Config
{
    public sealed class ModConfig
    {
        #region Properties
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

        public enum ShrinkRayTargetHighlighting
        {
            Off,
            OnHit,
            OnLoading
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

            public bool throwablePlayers { get; set; }

            public bool CanEscapeGrab { get; set; }

            public bool deathShrinking { get; set; }

            public float sizeChangeStep { get; set; }

            public float maximumPlayerSize { get; set; }

            public ShrinkRayTargetHighlighting shrinkRayTargetHighlighting { get; set; }

            public ThumperBehaviour thumperBehaviour { get; set; }

            public HoardingBugBehaviour hoardingBugBehaviour { get; set; }

            // Potions
            public int ShrinkPotionStorePrice { get; set; }
            public int ShrinkPotionScrapRarity { get; set; }

            public int EnlargePotionStorePrice { get; set; }
            public int EnlargePotionScrapRarity { get; set; }
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
            values.shrinkRayCost                = Plugin.BepInExConfig().Bind("General", "ShrinkRayCost", 0, "Store cost of the shrink ray").Value;
            values.deathShrinking               = Plugin.BepInExConfig().Bind("General", "DeathShrinking", false, "If true, a player can be shrunk below 0.2f, resulting in an instant death.").Value;
            values.sizeChangeStep               = Plugin.BepInExConfig().Bind("General", "SizeChangeStep", 0.4f, new ConfigDescription("Defines how much a player shrinks/enlarges in one step (>0.8 will instantly shrink to death if DeathShrinking is on, otherwise fail!).", new AcceptableValueRange<float>(0.05f, 10f))).Value;
            values.shrinkRayTargetHighlighting  = Plugin.BepInExConfig().Bind("General", "ShrinkRayTargetHighlighting", ShrinkRayTargetHighlighting.OnHit, "Defines, when a target gets highlighted. Set to OnLoading if you encounter performance issues.").Value;
            values.maximumPlayerSize            = Plugin.BepInExConfig().Bind("General", "MaximumPlayerSize", 1.8f, new ConfigDescription("Defines, how tall a player can become (1.8 is the last fitting height for the ship inside!)", new AcceptableValueRange<float>(1f, 10f))).Value;

            values.movementSpeedMultiplier      = Plugin.BepInExConfig().Bind("Shrunken", "MovementSpeedMultiplier", 1.3f, new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (very slow) to 1.5 (very fast).", new AcceptableValueRange<float>(0.5f, 1.5f))).Value;
            values.jumpHeightMultiplier         = Plugin.BepInExConfig().Bind("Shrunken", "JumpHeightMultiplier", 1.3f, new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (very low) to 2 (very high).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.weightMultiplier             = Plugin.BepInExConfig().Bind("Shrunken", "WeightMultiplier", 1.5f, new ConfigDescription("Weight multiplier on held items for shrunken players, ranging from 0.5 (lighter) to 2 (heavier).", new AcceptableValueRange<float>(0.5f, 2f))).Value;
            values.canUseVents                  = Plugin.BepInExConfig().Bind("Shrunken", "CanUseVents", true, "If true, shrunken players can move between vents.").Value;
            values.pitchDistortionIntensity     = Plugin.BepInExConfig().Bind("Shrunken", "PitchDistortionIntensity", 0.3f, new ConfigDescription("Intensity of the pitch distortion for shrunken players. 0 is the normal voice and 0.5 is very high.", new AcceptableValueRange<float>(0f, 0.5f))).Value;
            values.CanEscapeGrab                = Plugin.BepInExConfig().Bind("Shrunken", "CanEscapeGrab", true, "If true, a player who got grabbed can escape by jumping").Value;

            values.jumpOnShrunkenPlayers        = Plugin.BepInExConfig().Bind("Interactions", "JumpOnShrunkenPlayers", true, "If true, normal-sized players can harm shrunken players by jumping on them.").Value;
            values.throwablePlayers             = Plugin.BepInExConfig().Bind("Interactions", "ThrowablePlayers", true, "If true, shrunken players can be thrown by normal sized players.").Value;
            values.friendlyFlight               = Plugin.BepInExConfig().Bind("Interactions", "FriendlyFlight", false, "If true, held players can grab other players, causing comedic, but game breaking effects.").Value;
            values.sellablePlayers              = Plugin.BepInExConfig().Bind("Interactions", "sellablePlayers", true, "If true, held players can sell other players, causing comedic").Value;


            values.hoardingBugBehaviour         = Plugin.BepInExConfig().Bind("Enemies", "HoarderBugBehaviour", HoardingBugBehaviour.Default, "Defines if hoarding bugs should be able to grab you and how likely that is.").Value;
            values.thumperBehaviour             = Plugin.BepInExConfig().Bind("Enemies", "ThumperBehaviour", ThumperBehaviour.Bumper, "Defines the way Thumpers react on shrunken players.").Value;
            
            values.ShrinkPotionStorePrice       = Plugin.BepInExConfig().Bind("Potions", "ShrinkPotionShopPrice", 30, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500))).Value;
            values.ShrinkPotionScrapRarity      = Plugin.BepInExConfig().Bind("Potions", "ShrinkPotionScrapRarity", 10, new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100))).Value;

            values.EnlargePotionStorePrice      = Plugin.BepInExConfig().Bind("Potions", "EnlargePotionStorePrice", 50, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500))).Value;
            values.EnlargePotionScrapRarity     = Plugin.BepInExConfig().Bind("Potions", "EnlargePotionScrapRarity", 5, new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100))).Value;

            DebugLog                            = Plugin.BepInExConfig().Bind("Beta-only", "DebugLog", true, "Additional logging to help identifying issues in the beta version of this mod.").Value;

#if DEBUG
            Plugin.Log("Initial config: " + JsonConvert.SerializeObject(Instance.values));
#endif
        }

        public void Synced()
        {
            LittlePotion.ConfigSynced(); // Add or remove potions from store / as scrap
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
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RECEIVE_MESSAGE, clientId, writer, NetworkDelivery.ReliableSequenced);
            }

            public static void HostConfigReceived(ulong clientId, FastBufferReader reader)
            {
                reader.ReadValueSafe(out string json);
                Plugin.Log("Received host config: " + json);
                ConfigValues hostValues = JsonConvert.DeserializeObject<ConfigValues>(json);

                // Adjust client-sided options (WIP -> replace later with e.g. custom JsonConverter)
                hostValues.pitchDistortionIntensity = Instance.values.pitchDistortionIntensity;

                PlayerCosmetics.RegularizeCosmetics();

                Instance.values = hostValues;
                Instance.Synced();
            }
            #endregion
        }
    }
}

