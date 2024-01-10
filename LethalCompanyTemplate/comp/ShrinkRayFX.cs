using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace LCShrinkRay.comp
{
    public class ShrinkRayFX
    {
        private GameObject shrinkRayFX;
        private string assetDir;
        private AssetBundle FXAssets;
        private VisualEffect visualEffect;

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

            // This is the method I was having an issue with, loading the asset bundle and getting it into game :(
        // I can instantiate the normal Unity way, but I'm worried LC_API / LethalLib have methods I'm unaware of
        // Still new to modding 😬
        public void AddShrinkRayFXToGame()
        {
            // Do `fxasset` loading here, tried copying Nemo's code in shrinking.cs
            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fxasset");
            AssetBundle FXAssets = AssetBundle.LoadFromFile(assetDir);

            // The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            shrinkRayFX = FXAssets.LoadAsset<GameObject>("Shrink Ray VFX");

            if(shrinkRayFX == null)
            {
                Plugin.log("\n\nFRICKIN HECK WHY IS IT NULL???\n\n");
            }
        }

        /// <summary>
        /// This should be set to the tip of the shrinkray most likely
        /// </summary>
        public void SetStartPoint(Vector3 point)
        {
            shrinkRayFX.transform.Find("Pos1").position = point;
            shrinkRayFX.transform.Find("Pos2").position = point;
        }
        
        /// <summary>
        /// On clientside, target should be set to half a unit below the camera so they don't get blinded by lights
        /// </summary>
        public void SetEndPoint(Vector3 point)
        {
            // Pos 3 and 4 are the final points of the bezier curve
            shrinkRayFX.transform.Find("Pos3").position = point;
            shrinkRayFX.transform.Find("Pos4").position = point;
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