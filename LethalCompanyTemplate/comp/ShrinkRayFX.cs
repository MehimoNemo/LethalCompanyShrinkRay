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
        
        public GameObject CreateNewBeam(Transform startTransform, Transform endTransform, float duration)
        {
            
            
            GameObject gameObject = Instantiate(prefab);
            
            Transform bezier1 = gameObject.transform.Find("Pos1");
            Transform bezier2 = gameObject.transform.Find("Pos2");
            Transform bezier3 = gameObject.transform.Find("Pos3");
            Transform bezier4 = gameObject.transform.Find("Pos4");
            
            // Set the start pos of the beam
            bezier1.SetParent(startTransform);
            bezier1.localPosition = Vector3.zero;
            bezier2.SetParent(startTransform);
            bezier2.localPosition = Vector3.zero;
            
            // Set the end pos of the beam
            bezier3.SetParent(endTransform);
            bezier3.localPosition = Vector3.zero;
            bezier4.SetParent(endTransform);
            bezier4.localPosition = Vector3.zero;

            Destroy(gameObject, duration);

            return gameObject;
        }
        
        public GameObject CreateNewBeam(Transform parent)
        {
            GameObject gameObject = Instantiate(prefab);
            
            // Get the visual effect unity component if it's not set yet
            if (!visualEffect)
            {
                if (prefab.TryGetComponent(out visualEffect) == false)
                {
                    Plugin.log("Shrink Ray VFX: Couldn't get VisualEffect component", Plugin.LogType.Error);   
                }
            }

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