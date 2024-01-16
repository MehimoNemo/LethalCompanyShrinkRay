using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace LCShrinkRay.comp
{
    public class ShrinkRayFX : MonoBehaviour
    {
        private GameObject shrinkRayFX;
        private VisualEffect visualEffect;

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
        #endregion
        
        public ShrinkRayFX()
        {
            var assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fxasset");
            var FXAssets = AssetBundle.LoadFromFile(assetDir);

            // The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            prefab = FXAssets.LoadAsset<GameObject>("Shrink Ray VFX");
            NetworkManager.Singleton.AddNetworkPrefab(shrinkRayFX);

            if (shrinkRayFX == null)
            {
                Plugin.log("\n\nLOAD ASSET ERROR: Shrink Ray VFX\n\n");
                return;
            }
        }
        
        public GameObject CreateNewBeam(Transform parent)
        {
            // Get the visual effect unity component if it's not set yet
            if (!visualEffect)
            {
                visualEffect = visualEffect.GetComponentInChildren<VisualEffect>();
                
                if (!visualEffect)
                {
                    Plugin.log("Shrink Ray VFX: Couldn't get VisualEffect component", Plugin.LogType.Error);   
                }
            }
            
            GameObject gameObject = Instantiate(prefab);

            return gameObject;
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