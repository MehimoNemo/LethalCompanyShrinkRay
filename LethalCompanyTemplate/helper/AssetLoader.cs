using LCShrinkRay.comp;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace LCShrinkRay.helper
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
            Plugin.Log("icon path: " + iconPath);
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

        public static IEnumerator LoadAudioAsync(string filename, Action<AudioClip> onComplete, AudioType type = AudioType.WAV)
        {
            var audioPath = Path.Combine(Path.Combine(AssetDir, "audio"), filename);
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioPath, type))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Plugin.Log(www.error, Plugin.LogType.Error);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                Plugin.Log("Loaded audio \"" + filename + "\" -> " + clip);
                onComplete(clip);
            }
        }

        public static void LoadAllAssets()
        {
            GrabbablePlayerObject.LoadAsset();
            ShrinkRay.LoadAsset();
            LittlePotion.LoadPotionAssets();
        }
    }
}
