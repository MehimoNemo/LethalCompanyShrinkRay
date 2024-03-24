using GameNetcodeStuff;
using LittleCompany.helper;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.VFX;
using static LittleCompany.modifications.Modification;

namespace LittleCompany.components
{
    public class ShrinkRayFX : MonoBehaviour
    {
        #region Properties
        private static GameObject shrinkRayFX { get; set; }

        private VisualEffect defaultVisualEffect { get; set; }
        private VisualEffect activeVisualEffect { get; set; }

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

        // Bez 1 is the start, 4 is the end

        // Transform & Position properties
        private const float bezier2YPoint = 0.5f;  // at 50%
        private const float bezier2YOffset = 0.5f; // Height offset

        private const float bezier3YPoint= 0.80f;  // at 80%
        private const float bezier3YOffset = 1.5f; // Height offset

        public static float beamDuration = 2f; // If changed then adjust PlayerShrinkAnimation formula!!!
                                               //private const Color beamColor = Color.blue;

        public static AudioClip beamSFX;
        #endregion

        ShrinkRayFX()
        {
            if (shrinkRayFX != null) return;

            Plugin.Log("Adding ShrinRayFX asset.");

            // The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            shrinkRayFX = AssetLoader.fxAsset?.LoadAsset<GameObject>("Shrink Ray VFX");
            if (shrinkRayFX == null)
            {
                Plugin.Log("ShrinkRayFX Null Error: Tried to get shrinkRayFXPrefab but couldn't", Plugin.LogType.Error);
                return;
            }

            //NetworkManager.Singleton.AddNetworkPrefab(shrinkRayFX);
            
            // Get the visual effect unity component if it's not set yet
            if (!defaultVisualEffect)
            {
                defaultVisualEffect = shrinkRayFX.GetComponentInChildren<VisualEffect>();
                if (!defaultVisualEffect) Plugin.Log("Shrink Ray VFX Null Error: Couldn't get VisualEffect component", Plugin.LogType.Error);
            }

            // Customize the ShrinkRayFX (I just found some good settings by tweaking in game. Easier done here than in the prefab, which is why I made properties on the script)
            noiseSpeed = 5;
            noisePower = 0.1f;
            sparksSize = 1f;
            thickness = 0.1f;
        }

        public IEnumerator RenderRayBeam(Transform holderCamera, Transform target, ModificationType type, AudioSource shrinkRayAudio, Action onComplete = null)
        {
            if (!TryCreateNewBeam(out GameObject fxObject))
            {
                Plugin.Log("FX Object Null", Plugin.LogType.Error);

                if (onComplete != null)
                    onComplete();
                yield break;
            }

            bool beamCreated = false;
            try
            {
                activeVisualEffect = fxObject.GetComponentInChildren<VisualEffect>();
                if (!activeVisualEffect)
                {
                    Plugin.Log("Shrink Ray VFX Null Error: Couldn't get VisualEffect component", Plugin.LogType.Error);
                }
                else
                {
                    switch (type)
                    {
                        case ModificationType.Shrinking:
                            colorPrimary = new Color(0.61f, 0.04f, 0.04f); // red like shrinkray
                            colorSecondary = new Color(1f, 1f, 0f); // yellow
                            break;
                        case ModificationType.Enlarging:
                            colorPrimary = new Color(0f, 0.3f, 0f); // darkgreen
                            colorSecondary = new Color(1f, 1f, 0f); // yellow
                            break;
                        case ModificationType.Normalizing:
                            colorPrimary = Color.white;
                            colorSecondary = Color.gray;
                            break;
                    }
                }

                Transform bezier1 = fxObject.transform.GetChild(0)?.Find("Pos1");
                Transform bezier2 = fxObject.transform.GetChild(0)?.Find("Pos2");
                Transform bezier3 = fxObject.transform.GetChild(0)?.Find("Pos3");
                Transform bezier4 = fxObject.transform.GetChild(0)?.Find("Pos4");

                if (!bezier1) Plugin.Log("bezier1 Null", Plugin.LogType.Error);
                if (!bezier2) Plugin.Log("bezier2 Null", Plugin.LogType.Error);
                if (!bezier3) Plugin.Log("bezier3 Null", Plugin.LogType.Error);
                if (!bezier4) Plugin.Log("bezier4 Null", Plugin.LogType.Error);

                if(target.gameObject.TryGetComponent(out PlayerControllerB targetPlayer)) // For players target the head
                {
                    Transform targetHeadTransform = targetPlayer?.gameplayCamera?.transform?.Find("HUDHelmetPosition")?.transform;
                    if (targetHeadTransform == null)
                    {
                        Plugin.Log("Failed to get target players helmet position for shrink ray vfx", Plugin.LogType.Warning);

                        if (onComplete != null)
                            onComplete();
                        yield break;
                    }

                    target = targetHeadTransform;
                }

                // Stole this from above, minor adjustments to where the beam comes from
                Vector3 beamStartPos = this.transform.position + (Vector3.up * 0.25f) + (holderCamera.forward * -0.1f);

                // Set bezier 1 (start point)
                bezier1.transform.position = beamStartPos;
                bezier1.transform.SetParent(this.transform, true);

                // Set bezier 2 (curve)
                bezier2.transform.position = Vector3.Lerp(beamStartPos, target.position, bezier2YPoint) + (Vector3.up * bezier2YOffset);
                bezier2.transform.SetParent(this.transform, true);

                // Set bezier 3 (curve)
                bezier3.transform.position = Vector3.Lerp(beamStartPos, target.position, bezier3YPoint) + (Vector3.up * bezier3YOffset);
                bezier3.transform.SetParent(target, true);

                // Set Bezier 4 (final endpoint)
                Vector3 beamEndPos = (target.position);
                bezier4.transform.position = beamEndPos;
                bezier4.transform.SetParent(target, true);

                beamCreated = true;
            }
            catch (Exception e)
            {
                Plugin.Log("error trying to render beam: " + e.Message, Plugin.LogType.Error);
                Plugin.Log("error source: " + e.Source);
                Plugin.Log("error stack: " + e.StackTrace);

                if (onComplete != null)
                    onComplete();
                yield break;
            }

            if (shrinkRayAudio != null)
            {
                shrinkRayAudio.Stop();
                shrinkRayAudio.PlayOneShot(beamSFX);
            }
            yield return new WaitForSeconds(beamDuration);

            if (beamCreated)
            {
                Destroy(fxObject.transform.GetChild(0)?.Find("Pos1")?.gameObject);
                Destroy(fxObject.transform.GetChild(0)?.Find("Pos2")?.gameObject);
                Destroy(fxObject.transform.GetChild(0)?.Find("Pos3")?.gameObject);
                Destroy(fxObject.transform.GetChild(0)?.Find("Pos4")?.gameObject);
                Destroy(fxObject);
            }

            if (onComplete != null)
                onComplete();
        }

        public static bool TryCreateNewBeam(out GameObject beam)
        {
            if (shrinkRayFX == null)
            {
                beam = null;
                return false;
            }

            beam = Instantiate(shrinkRayFX);
            DontDestroyOnLoad(beam);
            return beam != null;
        }

        public static bool TryCreateDeathPoofAt(out GameObject deathPoof, Vector3 position)
        {
            deathPoof = Effects.DeathPoof;
            if (deathPoof == null)
                return false;

            deathPoof.transform.position = position;
            Destroy(deathPoof, 3f);

            return deathPoof != null;
        }

        private void SetFloat(string name, float value)
        {
            if (activeVisualEffect == null)
                defaultVisualEffect.SetFloat(name, value);
            else
                activeVisualEffect.SetFloat(name, value);
        }
        
        private void SetVector4(string name, Vector4 value)
        {
            if (activeVisualEffect == null)
                defaultVisualEffect.SetVector4(name, value);
            else
                activeVisualEffect.SetVector4(name, value);
        }
        
        private void SetInt(string name, int value)
        {
            if (activeVisualEffect == null)
                defaultVisualEffect.SetInt(name, value);
            else
                activeVisualEffect.SetInt(name, value);
        }
    }
}