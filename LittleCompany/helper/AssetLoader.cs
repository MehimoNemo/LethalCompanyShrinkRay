using LittleCompany.components;
using LittleCompany.modifications;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace LittleCompany.helper
{
    internal class AssetLoader
    {
        internal static readonly string AssetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static readonly string BaseAssetPath = "Assets/ShrinkRay";
        internal static readonly int IconWidth = 223;
        internal static readonly int IconHeight = 213;

        public static readonly AssetBundle fxAsset = AssetBundle.LoadFromFile(Path.Combine(AssetDir, "fxasset"));
        public static readonly AssetBundle littleCompanyAsset = AssetBundle.LoadFromFile(Path.Combine(AssetDir, "littlecompanyasset"));

        public static Sprite LoadIcon(string filename)
        {
            var iconPath = Path.Combine(Path.Combine(AssetDir, "icons"), filename);
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(AssetDir, filename);
                if (!File.Exists(iconPath))
                {
                    Plugin.Log("Icon \"" + iconPath + "\" not found in plugin directory!", Plugin.LogType.Error);
                    return null;
                }
            }

            byte[] bytes = File.ReadAllBytes(iconPath);
            var texture = new Texture2D(IconWidth, IconHeight, TextureFormat.RGB24, false);
            texture.LoadImage(bytes);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

        public static AudioClip LoadAudio(string filename, AudioType type = AudioType.WAV)
        {
            var audioPath = Path.Combine(Path.Combine(AssetDir, "audio"), filename);
            if (!File.Exists(audioPath))
            {
                audioPath = Path.Combine(AssetDir, filename);
                if (!File.Exists(audioPath))
                {
                    Plugin.Log("Audio \"" + audioPath + "\" not found in plugin directory!", Plugin.LogType.Error);
                    return null;
                }
            }

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioPath, type))
            {
                var request = www.SendWebRequest();
                while( !request.isDone ) { }

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Plugin.Log(www.error, Plugin.LogType.Error);
                    return null;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                Plugin.Log("Loaded audio \"" + filename);
                return clip;
            }
        }

        public static void LoadAllAssets()
        {
#if DEBUG
            foreach (var assetName in littleCompanyAsset?.GetAllAssetNames())
                Plugin.Log("Found asset: " + assetName);
#endif

            GrabbablePlayerObject.LoadAsset();
            ShrinkRay.LoadAsset();
            LittlePotion.LoadPotionAssets();
            Modification.deathPoofSFX = LoadAudio("deathPoof.wav");
        }
    }
}
