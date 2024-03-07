using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LethalLib.Modules;
using System;
using System.Collections;
using System.IO;
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

        internal static bool IsStoreItem = false;

        internal static bool IsScrapItem = false;

        internal static void LoadAsset()
        {
            if(networkPrefab != null || !TryLoadItem(AssetLoader.littleCompanyAsset, "ShrinkingPotionItem.asset", out Item item) || item.spawnPrefab == null)
                return;

            var potion = item.spawnPrefab.AddComponent<LittleShrinkingPotion>();
            networkPrefab = item.spawnPrefab;
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            Destroy(networkPrefab.GetComponent<PhysicsProp>());
            potion.itemProperties = item;
            potion.SetProperties();
            potion.RegisterPotion(ref IsScrapItem, ref IsStoreItem);
        }

        public static void Sync()
        {
            if (networkPrefab == null || networkPrefab.TryGetComponent(out LittleShrinkingPotion potion)) return;

            potion.RegisterPotion(ref IsScrapItem, ref IsStoreItem);
            if(IsScrapItem || IsStoreItem)
                potion.AdjustStoreAndScrapValues();
        }

        public override void Start()
        {
            PotionTransform = transform.Find("ShrinkingPotionSettings");
            base.Start();
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

        internal static bool IsStoreItem = false;

        internal static bool IsScrapItem = false;

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
            potion.RegisterPotion(ref IsScrapItem, ref IsStoreItem);
        }

        public static void Sync()
        {
            if (networkPrefab == null || networkPrefab.TryGetComponent(out LittleEnlargingPotion potion)) return;

            potion.RegisterPotion(ref IsScrapItem, ref IsStoreItem);

            if (IsScrapItem || IsStoreItem)
                potion.AdjustStoreAndScrapValues();
        }

        public override void Start()
        {
            PotionTransform = transform.Find("EnlargingPotionSettings");
            base.Start();
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
        internal static string BaseAssetPath = Path.Combine(AssetLoader.BaseAssetPath, "Potion");

        internal static bool AudioLoaded = false;

        internal static int ScrapValueWhenConsumed = 5;

        internal static AudioSource audioSource;

        internal static AudioClip grabSFX;

        internal static AudioClip dropSFX;

        internal static AudioClip consumeSFX;

        internal static AudioClip noConsumeSFX;

        internal static Sprite Icon = AssetLoader.LoadIcon("Potion.png");

        internal Transform PotionTransform = null;
        internal MeshRenderer Glass = null;
        internal MeshRenderer Liquid = null;
        internal MeshRenderer Cap = null;

        internal bool Consuming = false;
        internal NetworkVariable<bool> Consumed = new NetworkVariable<bool>(false);

        internal float InitialLiquidScale = 0.7f;
        internal float InitialLiquidPosition = -0.2f;

        internal ScanNodeProperties ScanNodeProperties = null;
        #endregion

        #region Networking
        public static void LoadPotionAssets()
        {
            Plugin.Log("Adding potion assets.");
            LittleShrinkingPotion.LoadAsset();
            LittleEnlargingPotion.LoadAsset();

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionGrab.wav", (item) => grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionDrop.wav", (item) => dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionConsume.wav", (item) => consumeSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionNoConsume.wav", (item) => noConsumeSFX = item));
        }

        public static void ConfigSynced()
        {
            LittleShrinkingPotion.Sync();
            LittleEnlargingPotion.Sync();
        }
        #endregion

        #region Initializing
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

        internal void RegisterPotion(ref bool IsScrapItem, ref bool IsStoreItem)
        {
            if (Rarity > 0 && !IsScrapItem) // Add as scrap
            {
                Items.RegisterScrap(itemProperties, Rarity, Levels.LevelTypes.All);
                IsScrapItem = true;
            }
            else if (IsScrapItem) // Remove from scrap
            {
                Items.RemoveScrapFromLevels(itemProperties, Levels.LevelTypes.All);
                IsScrapItem = false;
            }

            if (StorePrice > 0 && !IsStoreItem) // Add to store
            {
                AdjustStoreAndScrapValues();
                var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                terminalNode.displayText = TerminalDescription;
                Items.RegisterShopItem(itemProperties, null, null, terminalNode, itemProperties.creditsWorth);
                IsStoreItem = true;
            }
            else if(IsStoreItem) // Remove from store
            {
                Items.RemoveShopItem(itemProperties);
                IsStoreItem = false;
            }
        }

        internal void SetProperties()
        {
            grabbable = true;
            grabbableToEnemies = true;
            fallTime = 0f;

            itemProperties.itemIcon = Icon;
            itemProperties.toolTips = ["Consume: LMB"];

            itemProperties.rotationOffset = new Vector3(0f, 0f, -70f);
            itemProperties.positionOffset = new Vector3(0f, 0.12f, 0f);
        }

        internal void AdjustStoreAndScrapValues()
        {
            itemProperties.creditsWorth = StorePrice;
            itemProperties.minValue = Math.Max(StorePrice / 2, 0);
            itemProperties.maxValue = Math.Max(StorePrice, 0);
            System.Random rnd = new System.Random();
            SetScrapValue(rnd.Next(StorePrice / 2, StorePrice + 1));
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            Plugin.Log("Potion network spawn");

            ScanNodeProperties = GetComponentInChildren<ScanNodeProperties>();
            if (ScanNodeProperties != null)
            {
                ScanNodeProperties.headerText = ItemName;
                ScanNodeProperties.scrapValue = itemProperties.creditsWorth;
            }

            audioSource = GetComponent<AudioSource>();

            if(PotionTransform != null)
            {
                Glass = PotionTransform.GetComponent<MeshRenderer>();
                Cap = PotionTransform.Find("Cap")?.GetComponent<MeshRenderer>();
                Liquid = PotionTransform.Find("Liquid")?.GetComponent<MeshRenderer>();
                if (Liquid != null && Glass != null)
                {
                    var liquidColor = Liquid.material.color;
                    liquidColor.a = 2f;

                    Glass?.material.SetColor("_EmissionColor", liquidColor);
                    Glass?.material.EnableKeyword("_EMISSION");
                    
                    InitialLiquidScale = Liquid.transform.localScale.z; // 0.7
                    InitialLiquidPosition = Liquid.transform.localPosition.y; // -0.2
                }
            }

            if (Consumed.Value)
                SetConsumed();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.Log("Consuming " + ItemName);
                base.ItemActivate(used, buttonDown);

                StartCoroutine(Consume());
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

        public override void PocketItem()
        {
            base.PocketItem();
        }

        public override void EquipItem()
        {
            audioSource.PlayOneShot(grabSFX);
            base.EquipItem();
        }

        public override void GrabItem()
        {
            //transform.localScale = Vector3.one * 0.05f;
            base.GrabItem();
        }

        public override void DiscardItem()
        {
            //transform.localScale = Vector3.one * 0.1f;
            audioSource.PlayOneShot(dropSFX);
            base.DiscardItem();
        }

        public override void GrabItemFromEnemy(EnemyAI enemyAI)
        {
            audioSource.PlayOneShot(grabSFX);
            base.GrabItemFromEnemy(enemyAI);
        }

        public override void DiscardItemFromEnemy()
        {
            audioSource.PlayOneShot(dropSFX);
            base.DiscardItemFromEnemy();
        }
        #endregion

        #region Methods
        private IEnumerator Consume()
        {
            if (Consuming || playerHeldBy == null)
                yield break;

            if (Consumed.Value || playerHeldBy.isClimbingLadder)
            {
                if (IsOwner)
                    audioSource?.PlayOneShot(noConsumeSFX);
                yield break;
            }

            if(!CanApplyModificationTo(playerHeldBy, modificationType) || !ApplyModificationTo(playerHeldBy, modificationType))
            {
                if (IsOwner)
                    audioSource?.PlayOneShot(noConsumeSFX);
                yield break;
            }

            Consuming = true;
            isBeingUsed = true;

            if (IsOwner && audioSource != null)
            {
                audioSource.PlayOneShot(consumeSFX);

                if (Cap != null)
                    Destroy(Cap);

                var duration = ShrinkRayFX.beamDuration;
                yield return new WaitForSeconds(duration / 5);
                duration -= duration / 5;

                if (Liquid != null) // drink that!
                {
                    var time = 0f;
                    while (time < duration)
                    {
                        var percentageFilled = 100 * Mathf.Lerp(InitialLiquidScale, 0f, time / duration) / InitialLiquidScale;
                        Plugin.Log("percentageFilled: " + percentageFilled);
                        SetLiquidLevel(percentageFilled);
                        time += Time.deltaTime;
                        yield return null;
                    };

                    SetConsumed();
                }
                else
                {
                    yield return new WaitWhile(() => audioSource.isPlaying); // In case the audio clip length didn't match..
                    SetConsumed();
                }
            }
        }

        internal void SetLiquidLevel(float percentage)
        {
            if (Liquid == null) return;

            var remainingLiquid = InitialLiquidScale * percentage / 100;
            var consumedLiquid = InitialLiquidScale - remainingLiquid;
            Plugin.Log("Drank already " + consumedLiquid + " out of " + InitialLiquidScale);
            Liquid.transform.localPosition = new Vector3(Liquid.transform.localPosition.x, InitialLiquidPosition - consumedLiquid, Liquid.transform.localPosition.z);
            Liquid.transform.localScale = new Vector3(Liquid.transform.localScale.x, Liquid.transform.localScale.y, remainingLiquid);
        }

        internal void SetConsumed()
        {
            Consuming = false;
            isBeingUsed = false;
            if (PlayerInfo.IsHost)
                Consumed.Value = true;

            if (Cap != null)
                Destroy(Cap);
            SetLiquidLevel(0f);

            itemProperties.toolTips = [];

            if (scrapValue > ScrapValueWhenConsumed)
                SetScrapValue(ScrapValueWhenConsumed);
            if (ScanNodeProperties != null)
                ScanNodeProperties.headerText = "Empty Potion";
        }
        #endregion
    }
}
