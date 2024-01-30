using GameNetcodeStuff;
using System;
using System.IO;
using System.Reflection;
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
        private const float bezier2YOffset = 2.5f;
        private const float bezier3YOffset = 2f;

        private const float beamDuration = 2f;
        //private const Color beamColor = Color.blue;

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

            // Load death poof asset (WIP)
            //deathPoofFX = fxAssets.LoadAsset<GameObject>("Poof FX");
            //if (deathPoofFX == null)
            //    Plugin.log("AssetBundle Loading Error: Death Poof VFX", Plugin.LogType.Error);
        }

        public void RenderRayBeam(Transform holderCamera, Transform target, ShrinkRay.ModificationType type)
        {
            try
            {
                GameObject fxObject = CreateNewBeam(holderCamera);

                if (!fxObject) Plugin.log("FX Object Null", Plugin.LogType.Error);

                Transform bezier1 = fxObject.transform.GetChild(0).Find("Pos1");
                Transform bezier2 = fxObject.transform.GetChild(0).Find("Pos2");
                Transform bezier3 = fxObject.transform.GetChild(0).Find("Pos3");
                Transform bezier4 = fxObject.transform.GetChild(0).Find("Pos4");

                if (!bezier1) Plugin.log("bezier1 Null", Plugin.LogType.Error);
                if (!bezier2) Plugin.log("bezier2 Null", Plugin.LogType.Error);
                if (!bezier3) Plugin.log("bezier3 Null", Plugin.LogType.Error);
                if (!bezier4) Plugin.log("bezier4 Null", Plugin.LogType.Error);

                Transform targetHeadTransform = target.gameObject.GetComponent<PlayerControllerB>().gameplayCamera.transform.Find("HUDHelmetPosition").transform;

                // Stole this from above, minor adjustments to where the beam comes from
                Vector3 beamStartPos = this.transform.position + (Vector3.up * 0.25f) + (holderCamera.forward * -0.1f);

                // Set bezier 1 (start point)
                bezier1.transform.position = beamStartPos;
                bezier1.transform.SetParent(this.transform, true);

                // Set bezier 2 (curve)
                bezier2.transform.position = Vector3.Lerp(beamStartPos, targetHeadTransform.position, 1f/3f) + (Vector3.up * bezier2YOffset);
                bezier2.transform.SetParent(this.transform, true);

                // Set bezier 3 (curve)
                bezier3.transform.position = Vector3.Lerp(beamStartPos, targetHeadTransform.position, 2f/3f) + (Vector3.up * bezier3YOffset);
                bezier3.transform.SetParent(targetHeadTransform, true);

                // Set Bezier 4 (final endpoint)
                Vector3 beamEndPos = (targetHeadTransform.position);
                bezier4.transform.position = beamEndPos;
                bezier4.transform.SetParent(targetHeadTransform, true);

                // Destroy the beziers before the fxObject, just barely
                Destroy(bezier1.gameObject, beamDuration - 0.05f);
                Destroy(bezier2.gameObject, beamDuration - 0.05f);
                Destroy(bezier3.gameObject, beamDuration - 0.05f);
                Destroy(bezier4.gameObject, beamDuration - 0.05f);
                Destroy(fxObject, beamDuration);
            }
            catch (Exception e)
            {
                Plugin.log("error trying to render beam: " + e.Message, Plugin.LogType.Error);
                Plugin.log("error source: " + e.Source);
                Plugin.log("error stack: " + e.StackTrace);
            }
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