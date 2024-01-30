using System.IO;
using System.Reflection;
using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.VFX;

namespace LCShrinkRay.comp
{
    public class ShrinkRayFX : MonoBehaviour
    {
        private VisualEffect visualEffect;

        public static GameObject shrinkRayFX { get; private set; }
        public static GameObject deathPoofFX { get; private set; }

        // Bez 1 is the start, 4 is the end

        #region Properties
        public float thickness {
            set {
                SetFloat("Thickness", value);
            }
        }

        public Color colorPrimary {
            set {
                SetVector4("Color", value);
            }
        }
        
        public Color colorSecondary {
            set {
                SetVector4("SecondaryColor", value);
            }
        }
        
        public float noiseSpeed {
            set {
                SetFloat("NoiseSpeed", value);
            }
        }
        
        public float noisePower {
            set {
                SetFloat("NoisePower", value);
            }
        }
        
        public float sparksSize {
            set {
                SetFloat("SparksSize", value);
            }
        }
        
        /// <summary>
        /// Keep between 1 and 8
        /// </summary>
        public int noiseSmoothing {
            set {
                SetInt("NoiseSmoothing", value);
            }
        }

        // Transform & Position properties
        public float bezier3YOffset = 2.5f;
        public float bezier4YOffset = 0f;

        #endregion

        ShrinkRayFX()
        {
            if (shrinkRayFX != null) return;

            Plugin.log("Adding ShrinRayFX asset.");
            var assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fxasset");
            var fxAssets = AssetBundle.LoadFromFile(assetDir);

            // The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            shrinkRayFX = fxAssets.LoadAsset<GameObject>("Shrink Ray VFX");
            if (shrinkRayFX == null)
            {
                Plugin.log("ShrinkRayFX Null Error: Tried to get shrinkRayFXPrefab but couldn't", Plugin.LogType.Error);
                return;
            }

            //NetworkManager.Singleton.AddNetworkPrefab(shrinkRayFX);
            
            // Get the visual effect unity component if it's not set yet
            if (!visualEffect)
            {
                visualEffect = shrinkRayFX.GetComponentInChildren<VisualEffect>();
                if (!visualEffect) Plugin.log("Shrink Ray VFX Null Error: Couldn't get VisualEffect component", Plugin.LogType.Error);
            }

            // Load death poof asset
            deathPoofFX = fxAssets.LoadAsset<GameObject>("Poof FX");
            if (deathPoofFX == null)
                Plugin.log("AssetBundle Loading Error: Death Poof VFX", Plugin.LogType.Error);
        }

        public GameObject CreateNewBeam(Transform parent)
        {
            return Instantiate(shrinkRayFX);
        }

        private void SetFloat(string name, float value)
        {
            visualEffect.SetFloat(name, value);
        }
        
        private void SetVector4(string name, Vector4 value)
        {
            visualEffect.SetVector4(name, value);
        }
        
        private void SetInt(string name, int value)
        {
            visualEffect.SetInt(name, value);
        }
    }
}