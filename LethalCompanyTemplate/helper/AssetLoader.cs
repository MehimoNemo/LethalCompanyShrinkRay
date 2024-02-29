using LCShrinkRay.comp;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LCShrinkRay.helper
{
    internal class AssetLoader
    {
        internal static readonly string AssetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static readonly int IconWidth = 223;
        internal static readonly int IconHeight = 213;

        public static readonly AssetBundle shrinkAsset = AssetBundle.LoadFromFile(Path.Combine(AssetDir, "shrinkasset"));
        public static readonly AssetBundle fxAsset = AssetBundle.LoadFromFile(Path.Combine(AssetDir, "fxasset"));
        public static readonly AssetBundle littleCompanyAsset = AssetBundle.LoadFromFile(Path.Combine(AssetDir, "littlecompanyasset"));

        public static Sprite LoadIcon(string filename)
        {
            var iconPath = Path.Combine(AssetDir, filename);
            if (!File.Exists(iconPath))
            {
                Plugin.Log("Icon \"" +  iconPath + "\" not found in plugin directory!", Plugin.LogType.Error);
                return null;
            }

            byte[] bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(IconWidth, IconHeight, TextureFormat.RGB24, false);
            texture.LoadImage(bytes);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

        public static AudioClip LoadAudio(string filename)
        {
            Plugin.Log("Loading audio for " + filename);
            var audioPath = Path.Combine(Path.Combine(AssetDir, "audio"), filename);
            var audioRequest = new WWW(audioPath);
            var audioClip = audioRequest.GetAudioClip();
            if (audioClip != null)
                Plugin.Log("Loaded audio for " + filename);
            return audioRequest.GetAudioClip();
        }

        public static void LoadAllAssets()
        {
            GrabbablePlayerObject.LoadAsset();
            ShrinkRay.LoadAsset();
            LittlePotion.LoadPotionAssets();
        }
    }
}
