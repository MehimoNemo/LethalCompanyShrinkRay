using LittleCompany.Config;
using LittleCompany.helper;
using System;
using System.Collections;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.modifications.Modification;
using LittleCompany.modifications;
using LittleCompany.dependency;
using LethalLib.Modules;

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
    public class LittleShrinkingPotion : LittlePotion
    {
        internal const string AssetPath = "ShrinkingPotionItem.asset";
        internal static int StorePriceConfig = ModConfig.Instance.values.ShrinkPotionStorePrice;
        internal static int RarityConfig = ModConfig.Instance.values.ShrinkPotionScrapRarity;

        internal override string ItemName => "Light Potion";
        internal override string TerminalDescription => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a negative way. Lightweight and tastes like potato..";
        internal override int StorePrice => StorePriceConfig;
        internal override int Rarity => RarityConfig;
        internal override ModificationType modificationType => ModificationType.Shrinking;
        public static GameObject NetworkPrefab { get; private set; }

        internal static void LoadAsset()
        {
            if (!TryLoadItem(AssetLoader.littleCompanyAsset, AssetPath, out Item item) || item.spawnPrefab == null)
                return;

            NetworkPrefab = RegisterPrefab<LittleShrinkingPotion>(item);
        }

        public override void Start()
        {
            PotionTransform = transform.Find("ShrinkingPotionSettings");
            base.Start();
        }
    }

    public class LittleEnlargingPotion : LittlePotion
    {
        internal const string AssetPath = "EnlargingPotionItem.asset";
        internal static int StorePriceConfig = ModConfig.Instance.values.EnlargePotionStorePrice;
        internal static int RarityConfig = ModConfig.Instance.values.EnlargePotionScrapRarity;

        internal override string ItemName => "Heavy Potion";
        internal override string TerminalDescription => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a positive way. Heavy and tastes like cheesecake..";
        internal override int StorePrice => StorePriceConfig;
        internal override int Rarity => RarityConfig;
        internal override ModificationType modificationType => ModificationType.Enlarging;
        public static GameObject NetworkPrefab { get; private set; }

        internal static void LoadAsset()
        {
            if (!TryLoadItem(AssetLoader.littleCompanyAsset, AssetPath, out Item item) || item.spawnPrefab == null)
                return;

            NetworkPrefab = RegisterPrefab<LittleEnlargingPotion>(item);
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

        internal AudioSource audioSource;

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

        internal static GameObject RegisterPrefab<T>(Item item) where T : LittlePotion
        {
            T potion = ConfigurePotion<T>(item);
            item.creditsWorth = potion.StorePrice;
            item.minValue = Math.Max(potion.StorePrice / 2, 0);
            item.maxValue = Math.Max(potion.StorePrice, 0);
            ScrapManagementFacade.RegisterItem(item, potion.Rarity > 0, potion.StorePrice > 0, potion.Rarity, potion.TerminalDescription);
            return item.spawnPrefab;
        }

        internal static T ConfigurePotion<T>(Item item) where T : LittlePotion
        {
            T potion = item.spawnPrefab.AddComponent<T>();
            ScrapManagementFacade.FixMixerGroups(item.spawnPrefab);
            NetworkManager.Singleton.AddNetworkPrefab(item.spawnPrefab);
            Destroy(item.spawnPrefab.GetComponent<PhysicsProp>());
            potion.itemProperties = item;
            potion.SetProperties();
            return potion;
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

            itemProperties.syncUseFunction = true;
            itemProperties.syncGrabFunction = false;
            itemProperties.syncDiscardFunction = false;
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
                ScanNodeProperties.scrapValue = scrapValue;
            }

            if (!TryGetComponent(out audioSource)) // fallback that likely won't happen nowadays
            {
                Plugin.Log("AudioSource of " + gameObject.name + " was null. Adding a new one..", Plugin.LogType.Error);
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (PotionTransform != null)
            {
                Glass = PotionTransform.GetComponent<MeshRenderer>();
                Cap = PotionTransform.Find("Cap")?.GetComponent<MeshRenderer>();
                Liquid = PotionTransform.Find("Liquid")?.GetComponent<MeshRenderer>();
                if (Liquid != null)
                {
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
                Plugin.Log("Error while consuming potion: " + e.Message, Plugin.LogType.Error);
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
            base.EquipItem();
        }

        public override void GrabItem()
        {
            if (grabSFX != null && audioSource != null)
                audioSource.PlayOneShot(grabSFX);
            base.GrabItem();
        }

        public override void DiscardItem()
        {
            if (dropSFX != null && audioSource != null)
                audioSource.PlayOneShot(dropSFX);
            base.DiscardItem();
        }

        public override void GrabItemFromEnemy(EnemyAI enemyAI)
        {
            if (grabSFX != null && audioSource != null)
                audioSource.PlayOneShot(grabSFX);
            base.GrabItemFromEnemy(enemyAI);
        }

        public override void DiscardItemFromEnemy()
        {
            if (dropSFX != null && audioSource != null)
                audioSource.PlayOneShot(dropSFX);
            base.DiscardItemFromEnemy();
        }
        #endregion

        #region Methods
        private IEnumerator Consume()
        {
            if (Consuming || playerHeldBy == null)
                yield break;

            if (Consumed.Value || playerHeldBy.isClimbingLadder || !PlayerModification.CanApplyModificationTo(playerHeldBy, modificationType, playerHeldBy))
            {
                if (IsOwner && noConsumeSFX != null && audioSource != null)
                    audioSource.PlayOneShot(noConsumeSFX);
                yield break;
            }

            PlayerModification.ApplyModificationTo(playerHeldBy, modificationType, playerHeldBy);

            Consuming = true;
            isBeingUsed = true;

            if (audioSource == null)
            {
                SetConsumed();
                yield break; ;
            }

            if (IsOwner && consumeSFX != null && audioSource != null)
                audioSource.PlayOneShot(consumeSFX);

            if (Cap != null)
                Destroy(Cap);

            var duration = ShrinkRayFX.DefaultBeamDuration;
            yield return new WaitForSeconds(duration / 5);
            duration -= duration / 5;

            if (Liquid != null) // drink that!
            {
                var time = 0f;
                while (time < duration)
                {
                    var percentageFilled = 100 * Mathf.Lerp(InitialLiquidScale, 0f, time / duration) / InitialLiquidScale;
                    SetLiquidLevel(percentageFilled);
                    time += Time.deltaTime;
                    yield return null;
                };
            }
            else
                yield return new WaitWhile(() => audioSource.isPlaying); // In case the audio clip length didn't match..

            SetConsumed();
        }

        internal void SetLiquidLevel(float percentage)
        {
            if (Liquid == null) return;

            var remainingLiquid = InitialLiquidScale * percentage / 100;
            var consumedLiquid = InitialLiquidScale - remainingLiquid;
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
