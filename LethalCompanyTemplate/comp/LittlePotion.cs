using System;
using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;
using LCShrinkRay.helper;
using static LCShrinkRay.helper.PlayerModification;
using System.IO;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace LCShrinkRay.comp
{
    public class LittleShrinkPotion : LittlePotion
    {
        internal override string ItemName { get => "Little Shrink Potion"; }
        internal override string TerminalDescription { get => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a negative way. Lightweight and tastes like potato.."; }
        internal override int Rarity => 10;
        internal override Color potionColor => new Color(0.61f, 0.04f, 0.04f);

        public override void OnNetworkSpawn()
        {
            modificationType = ModificationType.Shrinking;
        }

        internal override void SetProperties()
        {
            base.SetProperties();
        }
    }
    public class LittleEnlargingPotion : LittlePotion
    {
        internal override string ItemName => "Little Enlarging Potion";
        internal override string TerminalDescription => ItemName + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer in a positive way. Heavy and tastes like cheesecake..";
        internal override int Rarity => 5;
        internal override Color potionColor => new Color(0f, 0.3f, 0f);

        public override void OnNetworkSpawn()
        {
            modificationType = ModificationType.Enlarging;
        }

        internal override void SetProperties()
        {
            base.SetProperties();
        }
    }

    public abstract class LittlePotion : GrabbableObject
    {
        #region Abstracts
        internal abstract string ItemName { get; }

        internal abstract string TerminalDescription { get; }

        internal abstract int Rarity { get; }

        internal abstract Color potionColor { get; }
        #endregion

        #region Properties
        private static GameObject networkPrefab { get; set; }

        public ModificationType modificationType = ModificationType.Shrinking;

        private static Sprite Icon
        {
            get
            {
                string assetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assetDir))
                {
                    Plugin.Log("LittlePotionIcon not found!", Plugin.LogType.Error);
                    return null;
                }

                var imagePath = Path.Combine(assetDir, "Potion.png");
                if (File.Exists(imagePath))
                {
                    var width = 223;
                    var height = 213;
                    byte[] bytes = File.ReadAllBytes(imagePath);
                    var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    texture.LoadImage(bytes);
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    return sprite;
                }
                return null;
            }
        }
        #endregion

        #region Networking
        public static void LoadAllPotionAssets()
        {
            if (networkPrefab != null) return; // Already loaded

            string assetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(assetDir, "littlecompanyasset"));
            Item assetItem = null;
            if (assetBundle != null)
                assetItem = assetBundle.LoadAsset<Item>("Assets/ShrinkRay/Potion/PotionItem.asset");

            if (assetItem == null)
            {
                Plugin.Log("PotionItem.asset not found!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;

            LittlePotion potion = networkPrefab.AddComponent<LittleShrinkPotion>();

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            potion.itemProperties = assetItem;
            potion.SetProperties();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            terminalNode.displayText = potion.TerminalDescription;
            Items.RegisterShopItem(potion.itemProperties, null, null, terminalNode, potion.itemProperties.creditsWorth);
            Items.RegisterScrap(potion.itemProperties, 10, Levels.LevelTypes.All);

            Plugin.Log("Adding ShrinkPotion asset.");
        }

        internal virtual void SetProperties()
        {
            grabbable = true;
            grabbableToEnemies = true;
            fallTime = 0f;

            itemProperties.itemName = ItemName;
            itemProperties.name = ItemName;
            itemProperties.itemIcon = Icon;
            itemProperties.toolTips = ["Consume: LMB"];
            itemProperties.syncUseFunction = true;
            itemProperties.requiresBattery = false;
            itemProperties.rotationOffset = new Vector3(0, 0, -90);
            itemProperties.positionOffset = new Vector3(-0.1f, 0f, 0f);
            itemProperties.canBeGrabbedBeforeGameStart = true;
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            if(itemProperties.minValue > itemProperties.maxValue) // todo: solve this later in unity
            {
                itemProperties.minValue = itemProperties.maxValue;
                itemProperties.maxValue++;
            }

            // Add emission
            Plugin.Log("Add emission");
            var liquidRenderer = gameObject?.transform?.Find("PotionSettings")?.Find("Liquid")?.GetComponent<MeshRenderer>();
            if (liquidRenderer != null)
            {
                Plugin.Log("Add emission2");
                if (liquidRenderer.sharedMaterial != null)
                {
                    Plugin.Log("Add emission3");
                    var m = new Material(liquidRenderer.sharedMaterial);
                    m.color = potionColor;
                    m.SetColor("_EmissionColor", new Color(potionColor.r, potionColor.g, potionColor.b, 20f));
                    m.SetFloat("_Metallic", 1f);
                    m.SetFloat("_Glossiness", 1f);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

                    liquidRenderer.sharedMaterial = m;
                }
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
            // todo: animation & sound
            ApplyModificationTo(playerHeldBy, modificationType);

            DestroyObjectInHand(playerHeldBy);
        }
        #endregion
    }
}
