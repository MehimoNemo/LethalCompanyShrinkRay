using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using static LCShrinkRay.helper.PlayerModification;

namespace LCShrinkRay.comp
{
    public class LittleShrinkingPotion : LittlePotion
    {
        internal override string ItemName => "Light Potion";
        internal override string TerminalDescription => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a negative way. Lightweight and tastes like potato..";
        internal override int StorePrice => ModConfig.Instance.values.ShrinkPotionStorePrice;
        internal override int Rarity => ModConfig.Instance.values.ShrinkPotionScrapRarity;
        internal override ModificationType modificationType => ModificationType.Shrinking;
        public static GameObject networkPrefab { get; private set; }

        internal static void LoadAsset()
        {
            if(networkPrefab != null || !TryLoadItem(AssetLoader.littleCompanyAsset, "ShrinkingPotionItem.asset", out Item item) || item.spawnPrefab == null)
                return;

            var potion = item.spawnPrefab.AddComponent<LittleShrinkingPotion>();
            networkPrefab = item.spawnPrefab;
            networkPrefab = item.spawnPrefab;
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            Destroy(networkPrefab.GetComponent<PhysicsProp>());
            potion.itemProperties = item;
            potion.SetProperties();
            potion.RegisterPotion();
        }
    }

    public class LittleEnlargingPotion : LittlePotion
    {
        internal override string ItemName => "Heavy Potion";
        internal override string TerminalDescription => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a positive way. Heavy and tastes like cheesecake..";
        internal override int StorePrice => ModConfig.Instance.values.EnlargePotionStorePrice;
        internal override int Rarity => ModConfig.Instance.values.EnlargePotionScrapRarity;
        internal override ModificationType modificationType => ModificationType.Enlarging;
        public static GameObject networkPrefab { get; private set; }

        internal static void LoadAsset()
        {
            if (networkPrefab != null || !TryLoadItem(AssetLoader.littleCompanyAsset, "EnlargingPotionItem.asset", out Item item) || item.spawnPrefab == null)
                return;

            var potion = item.spawnPrefab.AddComponent<LittleEnlargingPotion>();
            networkPrefab = item.spawnPrefab;
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            Destroy(networkPrefab.GetComponent<PhysicsProp>());
            potion.itemProperties = item;
            potion.SetProperties();
            potion.RegisterPotion();
        }
    }

    public abstract class LittlePotion : GrabbableObject
    {
        #region Abstracts
        internal abstract string ItemName { get; }

        internal abstract string TerminalDescription { get; }

        internal abstract int StorePrice { get; }

        internal abstract int Rarity { get; }

        internal abstract ModificationType modificationType { get; }
        #endregion

        #region Properties
        internal static string BaseAssetPath = "Assets/ShrinkRay/Potion";

        internal static AudioClip grabSFX = AssetLoader.LoadAudio("potionGrab.wav");

        internal static AudioClip dropSFX = AssetLoader.LoadAudio("potionDrop.wav");

        internal static AudioClip consumeSFX = AssetLoader.LoadAudio("potionConsume.wav");

        internal static AudioClip noConsumeSFX = AssetLoader.LoadAudio("potionNoConsume.wav");
        #endregion

        #region Networking
        public static void LoadPotionAssets()
        {
            Plugin.Log("Adding potion assets.");
            LittleShrinkingPotion.LoadAsset();
            LittleEnlargingPotion.LoadAsset();
        }
        #endregion

        #region Abstract Methods
        internal static bool TryLoadItem(AssetBundle assetBundle, string assetName, out Item item)
        {
            Item assetItem = null;
            if (assetBundle != null)
                assetItem = assetBundle.LoadAsset<Item>(Path.Combine(BaseAssetPath, assetName));

            if (assetItem == null)
            {
                Plugin.Log(assetName + " not found!", Plugin.LogType.Error);
                item = null;
                return false;
            }

            item = assetItem;
            return true;
        }

        internal void RegisterPotion()
        {
            if (StorePrice > 0)
            {
                itemProperties.creditsWorth = Math.Max(StorePrice - 5, 0);
                itemProperties.minValue = Math.Max(StorePrice / 2, 0);
                itemProperties.maxValue = Math.Max(StorePrice, 0);
                var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                terminalNode.displayText = TerminalDescription;
                Items.RegisterShopItem(itemProperties, null, null, terminalNode, itemProperties.creditsWorth);
            }

            if(Rarity > 0)
            {
                Items.RegisterScrap(itemProperties, Rarity, Levels.LevelTypes.All);
            }
        }

        internal virtual void SetProperties()
        {
            grabbable = true;
            grabbableToEnemies = true;
            fallTime = 0f;

            itemProperties.itemIcon = AssetLoader.LoadIcon("Potion.png"); ;
            itemProperties.itemName = ItemName;
            itemProperties.name = ItemName;
            itemProperties.toolTips = ["Consume: LMB"];
            itemProperties.syncUseFunction = true;
            itemProperties.requiresBattery = false;
            itemProperties.rotationOffset = new Vector3(0, 0, -90);
            //itemProperties.positionOffset = new Vector3(-0.1f, 0f, 0f);
            itemProperties.canBeGrabbedBeforeGameStart = true;


            // Audio
            itemProperties.grabSFX = grabSFX;
            itemProperties.dropSFX = dropSFX;
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            Plugin.Log("Potion network spawn");

            var scanNodeProperties = GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null)
            {
                scanNodeProperties.headerText += " [" + modificationType.ToString() + "]";
                scanNodeProperties.scrapValue = itemProperties.creditsWorth;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.Log("Consuming " + ItemName);
                base.ItemActivate(used, buttonDown);

                Consume();
            }
            catch (Exception e) {
                Plugin.Log("Error while shooting ray: " + e.Message, Plugin.LogType.Error);
                Plugin.Log($"Stack Trace: {e.StackTrace}");
            }
        }

        public override void Update()
        {
            base.Update();
        }

        public override void EquipItem()
        {
            // idea: play a fading-in sound
            base.EquipItem();
        }

        public override void PocketItem()
        {
            // idea: play a fading-out sound
            base.PocketItem();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
        }
        #endregion

        #region Consumption
        private void Consume()
        {
           if (playerHeldBy == null || playerHeldBy.isClimbingLadder)
                return;

            Plugin.Log("Consuming.");
            if(!CanApplyModificationTo(playerHeldBy, modificationType) || !ApplyModificationTo(playerHeldBy, modificationType))
            {
                Plugin.Log("That wouldn't do anything..");
                playerHeldBy.itemAudio?.PlayOneShot(noConsumeSFX);
                return;
            }

            playerHeldBy.itemAudio?.PlayOneShot(consumeSFX);
            DestroyObjectInHand(playerHeldBy);
        }
        #endregion
    }
}
