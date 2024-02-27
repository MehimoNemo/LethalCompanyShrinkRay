using System;
using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;
using LCShrinkRay.helper;
using static LCShrinkRay.helper.PlayerModification;
using System.IO;
using System.Reflection;

namespace LCShrinkRay.comp
{
    public class LittlePotion : GrabbableObject
    {
        #region Properties
        public const string itemname = "Little Potion";

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
        public static void LoadAsset()
        {
            if (networkPrefab != null) return; // Already loaded

            string assetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(assetDir, "littlecompanyasset"));
            Item assetItem = null;
            if(assetBundle != null)
                assetItem = assetBundle.LoadAsset<Item>("Assets/ShrinkRay/Potion/PotionItem.asset");

            if(assetItem == null )
            {
                Plugin.Log("PotionItem.asset not found!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;
            assetItem.canBeGrabbedBeforeGameStart = true;

            LittlePotion visScript = networkPrefab.AddComponent<LittlePotion>();

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            visScript.grabbable = true;
            visScript.grabbableToEnemies = true;
            visScript.fallTime = 0f;

            visScript.itemProperties = assetItem;
            visScript.itemProperties.itemIcon = Icon;
            visScript.itemProperties.itemName = itemname;
            visScript.itemProperties.name = itemname;
            visScript.itemProperties.toolTips = ["Consume: LMB"];
            visScript.itemProperties.syncUseFunction = true;
            visScript.itemProperties.requiresBattery = false;
            visScript.itemProperties.rotationOffset = new Vector3(0, 0, -90);
            visScript.itemProperties.positionOffset = new Vector3(-0.1f, 0f, 0f);

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            terminalNode.displayText = itemname + "\nA mysteric potion that glows in the dark. Rumours say that it affects the size of the consumer. Lightweight and tastes like potato..";
            Items.RegisterShopItem(assetItem, null, null, terminalNode, assetItem.creditsWorth);
            Items.RegisterScrap(assetItem, 10, Levels.LevelTypes.All);

            Plugin.Log("Added ShrinkPotion.");
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();

            if(isInFactory)
            {
                // Spawned as scrap -> choose modificationType randomly
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
                    m.color = Color.red;
                    m.SetColor("_EmissionColor", new Color(1f, 0f, 0f, 20f));
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
                Plugin.Log("Consuming " + itemname);
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
            ApplyModificationTo(playerHeldBy, modificationType);

            DestroyObjectInHand(playerHeldBy);
        }
        #endregion
    }
}
