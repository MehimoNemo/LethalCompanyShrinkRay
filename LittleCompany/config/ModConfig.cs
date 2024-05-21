using BepInEx.Configuration;
using UnityEngine;
using CSync.Lib;
using System.Runtime.Serialization;
using CSync.Util;

namespace LittleCompany.Config
{
    [DataContract]
    [KnownType(typeof(ShrinkRayTargetHighlighting))]
    [KnownType(typeof(HoardingBugBehaviour))]
    [KnownType(typeof(ThumperBehaviour))]
    public class ModConfig : SyncedConfig<ModConfig>
    {
        #region Properties
        internal static readonly float SmallestSizeChange = 0.05f;

        // General
        [DataMember] public SyncedEntry<int> SHRINK_RAY_COST { get; private set; }
        [DataMember] public SyncedEntry<bool> DEATH_SHRINKING { get; private set; }

        [DataContract]
        public enum ShrinkRayTargetHighlighting
        {
            [EnumMember]
            Off,
            [EnumMember]
            OnHit,
            [EnumMember]
            OnLoading
        }
        [DataMember] public SyncedEntry<ShrinkRayTargetHighlighting> SHRINK_TAY_TARGET_HIGHLIGHTING { get; private set; }

        // Sizing
        [DataMember] public SyncedEntry<float> DEFAULT_PLAYER_SIZE { get; private set; }
        [DataMember] public SyncedEntry<float> MAXIMUM_PLAYER_SIZE { get; private set; }
        [DataMember] public SyncedEntry<float> PLAYER_SIZE_STEP_CHANGE { get; private set; }
        [DataMember] public SyncedEntry<float> ITEM_SIZE_STEP_CHANGE { get; private set; }
        [DataMember] public SyncedEntry<bool> ITEM_SCALING_VISUAL_ONLY { get; private set; }
        [DataMember] public SyncedEntry<float> ENEMY_SIZE_STEP_CHANGE { get; private set; }

        // Shrunken
        [DataMember] public SyncedEntry<float> MOVEMENT_SPEED_MULTIPLIER { get; private set; }
        [DataMember] public SyncedEntry<float> JUMP_HEIGHT_MULTIPLIER { get; private set; }
        [DataMember] public SyncedEntry<float> WEIGHT_MULTIPLIER { get; private set; }
        [DataMember] public SyncedEntry<bool> CAN_USE_VENTS { get; private set; }
        public ConfigEntry<float> PITCH_DISTORTION_INTENSITY { get; private set; }
        [DataMember] public SyncedEntry<bool> CAN_ESCAPE_GRAB { get; private set; }
        [DataMember] public SyncedEntry<bool> CANT_OPEN_STORAGE_CLOSET { get; private set; }

        // Interactions
        [DataMember] public SyncedEntry<bool> JUMP_ON_SHRUNKEN_PLAYERS { get; private set; }
        [DataMember] public SyncedEntry<bool> THROWABLE_PLAYERS { get; private set; }
        [DataMember] public SyncedEntry<bool> SELLABLE_PLAYERS { get; private set; }

        // Enemies
        public ConfigEntry<float> ENEMY_PITCH_DISTORTION_INTENSITY { get; private set; }

        [DataContract]
        public enum HoardingBugBehaviour
        {
            [EnumMember]
            Default,
            [EnumMember]
            NoGrab,
            [EnumMember]
            Addicted
        }
        [DataMember] public SyncedEntry<HoardingBugBehaviour> HOARDING_BUG_BEHAVIOUR { get; private set; }

        [DataContract]
        public enum ThumperBehaviour
        {
            [EnumMember]
            Default,
            [EnumMember]
            OneShot,
            [EnumMember]
            Bumper
        }
        [DataMember] public SyncedEntry<ThumperBehaviour> THUMPER_BEHAVIOUR { get; private set; }

        // Potions
        [DataMember] public SyncedEntry<int> SHRINK_POTION_STORE_PRICE { get; private set; }
        [DataMember] public SyncedEntry<int> SHRINK_POTION_SCRAP_RARITY { get; private set; }
        [DataMember] public SyncedEntry<int> ENLARGE_POTION_STORE_PRICE { get; private set; }
        [DataMember] public SyncedEntry<int> ENLARGE_POTION_SCRAP_RARITY { get; private set; }

        // Debug
        public ConfigEntry<bool> DEBUG_LOG { get; private set; }
        #endregion

        public ModConfig(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
        {
            ConfigManager.Register(this);

            SyncReceived += OnReceive;

            SHRINK_RAY_COST                = cfg.BindSyncedEntry("General", "ShrinkRayCost",               1000,                              new ConfigDescription("Store cost of the shrink ray"));
            DEATH_SHRINKING                = cfg.BindSyncedEntry("General", "DeathShrinking",              false,                             new ConfigDescription("If true, a player can be shrunk below 0.2f, resulting in an instant death."));
            SHRINK_TAY_TARGET_HIGHLIGHTING = cfg.BindSyncedEntry("General", "ShrinkRayTargetHighlighting", ShrinkRayTargetHighlighting.OnHit, new ConfigDescription("Defines, when a target gets highlighted. Set to OnLoading if you encounter performance issues."));

            DEFAULT_PLAYER_SIZE      = cfg.BindSyncedEntry("Sizing", "DefaultPlayerSize",     1f,    new ConfigDescription("The default player size when joining a lobby or reviving.", new AcceptableValueRange<float>(0.2f, 1.7f)));
            MAXIMUM_PLAYER_SIZE      = cfg.BindSyncedEntry("Sizing", "MaximumPlayerSize",     1.7f,  new ConfigDescription("Defines, how tall a player can become (1.7 is the last fitting height for the ship inside and doors!)"));
            PLAYER_SIZE_STEP_CHANGE  = cfg.BindSyncedEntry("Sizing", "PlayerSizeChangeStep",  0.4f,  new ConfigDescription("Defines how much a player shrinks/enlarges in one step (>0.8 will instantly shrink to death if DeathShrinking is on, otherwise fail!).", new AcceptableValueRange<float>(SmallestSizeChange, 10f)));
            ITEM_SIZE_STEP_CHANGE    = cfg.BindSyncedEntry("Sizing", "ItemSizeChangeStep",    0.5f,  new ConfigDescription("Defines how much an item shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f)));
            ITEM_SCALING_VISUAL_ONLY = cfg.BindSyncedEntry("Sizing", "ItemScalingVisualOnly", false, new ConfigDescription("If true, scaling items has no special effects."));
            ENEMY_SIZE_STEP_CHANGE   = cfg.BindSyncedEntry("Sizing", "EnemySizeChangeStep",   0.5f,  new ConfigDescription("Defines how much an enemy shrinks/enlarges in one step. Set to 0 to disable this feature.", new AcceptableValueRange<float>(0, 10f)));

            MOVEMENT_SPEED_MULTIPLIER       = cfg.BindSyncedEntry("Shrunken", "MovementSpeedMultiplier",  1.3f,  new ConfigDescription("Speed multiplier for shrunken players, ranging from 0.5 (very slow) to 1.5 (very fast).", new AcceptableValueRange<float>(0.5f, 1.5f)));
            JUMP_HEIGHT_MULTIPLIER          = cfg.BindSyncedEntry("Shrunken", "JumpHeightMultiplier",     1.3f,  new ConfigDescription("Jump-height multiplier for shrunken players, ranging from 0.5 (very low) to 2 (very high).", new AcceptableValueRange<float>(0.5f, 2f)));
            WEIGHT_MULTIPLIER               = cfg.BindSyncedEntry("Shrunken", "WeightMultiplier",         1.5f,  new ConfigDescription("Weight multiplier on held items for shrunken players, ranging from 0.5 (lighter) to 2 (heavier).", new AcceptableValueRange<float>(0.5f, 2f)));
            CAN_USE_VENTS                   = cfg.BindSyncedEntry("Shrunken", "CanUseVents",              true,  new ConfigDescription("If true, shrunken players can move between vents."));
            PITCH_DISTORTION_INTENSITY      = cfg.Bind(           "Shrunken", "PitchDistortionIntensity", 0.3f,  new ConfigDescription("Intensity of the pitch distortion for players with a different size than the local player, from 0 (unchanged) to 0.5 (strong).", new AcceptableValueRange<float>(0f, 0.5f)));
            CAN_ESCAPE_GRAB                 = cfg.BindSyncedEntry("Shrunken", "CanEscapeGrab",            true,  new ConfigDescription("If true, a player who got grabbed can escape by jumping"));
            CANT_OPEN_STORAGE_CLOSET        = cfg.BindSyncedEntry("Shrunken", "CantOpenStorageCloset",    false, new ConfigDescription("If true, a shrunken player can't open or close the storage closet anymore."));

            JUMP_ON_SHRUNKEN_PLAYERS     = cfg.BindSyncedEntry("Interactions", "JumpOnShrunkenPlayers", true, new ConfigDescription("If true, normal-sized players can harm shrunken players by jumping on them."));
            THROWABLE_PLAYERS            = cfg.BindSyncedEntry("Interactions", "ThrowablePlayers",      true, new ConfigDescription("If true, shrunken players can be thrown by normal sized players."));
            SELLABLE_PLAYERS             = cfg.BindSyncedEntry("Interactions", "SellablePlayers",       true, new ConfigDescription("If true, shrunken players can be sold to the company"));

            ENEMY_PITCH_DISTORTION_INTENSITY = cfg.Bind(           "Enemies", "EnemyPitchDistortionIntensity", 0.2f,                         new ConfigDescription("Intensity of the pitch distortion for enemies with a different size than the local player, from 0 (unchanged) to 0.5 (strong).", new AcceptableValueRange<float>(0f, 0.5f)));
            HOARDING_BUG_BEHAVIOUR           = cfg.BindSyncedEntry("Enemies", "HoarderBugBehaviour",           HoardingBugBehaviour.Default, new ConfigDescription("Defines if hoarding bugs should be able to grab you and how likely that is."));
            THUMPER_BEHAVIOUR                = cfg.BindSyncedEntry("Enemies", "ThumperBehaviour",              ThumperBehaviour.Bumper,      new ConfigDescription("Defines the way Thumpers react on shrunken players.")    );

            SHRINK_POTION_STORE_PRICE   = cfg.BindSyncedEntry("Potions", "ShrinkPotionShopPrice",    30, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500)));
            SHRINK_POTION_SCRAP_RARITY  = cfg.BindSyncedEntry("Potions", "ShrinkPotionScrapRarity",  10, new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100)));
            ENLARGE_POTION_STORE_PRICE  = cfg.BindSyncedEntry("Potions", "EnlargePotionStorePrice",  50, new ConfigDescription("Sets the store price. 0 to removed potion from store.", new AcceptableValueRange<int>(0, 500)));
            ENLARGE_POTION_SCRAP_RARITY = cfg.BindSyncedEntry("Potions", "EnlargePotionScrapRarity", 5,  new ConfigDescription("Sets the scrap rarity. 0 makes it unable to spawn inside.", new AcceptableValueRange<int>(0, 100)));
                                                   
            DEBUG_LOG = cfg.Bind( "Beta-only", "DebugLog", false, new ConfigDescription("Additional logging to help identifying issues of this mod."));

            FixWrongEntries();
        }

        void OnReceive()
        {
            Plugin.Log("Received host config.");
            FixWrongEntries();

			PlayerCountChangeDetection.ConfigSyncedOnConnect();
        }

        public void FixWrongEntries()
        {
            MAXIMUM_PLAYER_SIZE.Value = Mathf.Max(MAXIMUM_PLAYER_SIZE.Value, DEFAULT_PLAYER_SIZE.Value);
        }
    }
}

