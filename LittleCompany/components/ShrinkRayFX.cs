using GameNetcodeStuff;
using LittleCompany.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using static LittleCompany.modifications.Modification;

namespace LittleCompany.components
{
    public class ShrinkRayFX : MonoBehaviour
    {
        #region Properties
        public const float DefaultBeamDuration = 2f;

        private static GameObject fxPrefab { get; set; }

        private static VisualEffect defaultVisualEffect { get; set; }

        private VisualEffect visualEffect { get; set; }

        public float thickness
        {
            set
            {
                SetFloat("Thickness", value);
            }
        }

        public Color colorPrimary
        {
            set
            {
                SetVector4("Color", value);
            }
        }

        public Color colorSecondary
        {
            set
            {
                SetVector4("SecondaryColor", value);
            }
        }

        public float noiseSpeed
        {
            set
            {
                SetFloat("NoiseSpeed", value);
            }
        }

        public float noisePower
        {
            set
            {
                SetFloat("NoisePower", value);
            }
        }

        public float sparksSize
        {
            set
            {
                SetFloat("SparksSize", value);
            }
        }

        /// <summary>
        /// Keep between 1 and 8
        /// </summary>
        public int noiseSmoothing
        {
            set
            {
                SetInt("NoiseSmoothing", value);
            }
        }

        // Bez 1 is the start, 4 is the end

        // Transform & Position properties
        public float bezier2YPoint = 0.5f;  // at 50%
        public float bezier2YOffset = 0.5f; // Height offset

        public float bezier3YPoint = 0.80f;  // at 80%
        public float bezier3YOffset = 1.5f; // Height offset

        public bool usingOffsets = true;

        public float beamDuration = DefaultBeamDuration;    // If changed then adjust PlayerShrinkAnimation formula!!!
                                                            //private const Color beamColor = Color.blue;

        public static AudioClip beamSFX;

        private Dictionary<GameObject, Coroutine> activeBeams = new Dictionary<GameObject, Coroutine>();
        #endregion

        #region Networking
        public static void LoadAsset()
        {
            if (fxPrefab != null) return;

            Plugin.Log("Adding ShrinRayFX asset.");

            // The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            fxPrefab = AssetLoader.fxAsset?.LoadAsset<GameObject>("Shrink Ray VFX");
            if (fxPrefab == null)
            {
                Plugin.Log("ShrinkRayFX Null Error: Tried to get shrinkRayFXPrefab but couldn't", Plugin.LogType.Error);
                return;
            }

            // Get the visual effect unity component if it's not set yet
            defaultVisualEffect = fxPrefab.GetComponentInChildren<VisualEffect>();
            if (!defaultVisualEffect)
                Plugin.Log("Shrink Ray VFX Null Error: Couldn't get VisualEffect component", Plugin.LogType.Error);
        }
        #endregion

        #region Methods
        void OnDestroy()
        {
            for (int i = activeBeams.Count - 1; i >= 0; i--)
                RemoveBeam(activeBeams.ElementAt(i).Key);
        }

        void OnDisable()
        {
            for (int i = activeBeams.Count - 1; i >= 0; i--)
                RemoveBeam(activeBeams.ElementAt(i).Key);
        }

        public void RemoveBeam(GameObject beam)
        {
            if (!activeBeams.TryGetValue(beam, out Coroutine coroutine))
                return;

            if(coroutine != null)
                StopCoroutine(coroutine);
            Destroy(beam);
        }

        public GameObject RenderRayBeam(Transform holderCamera, Transform target, ModificationType? type = null, AudioSource shrinkRayAudio = null, Action onComplete = null)
        {
            var beam = Instantiate(fxPrefab);
            //DontDestroyOnLoad(fx);

            var coroutine = StartCoroutine(RenderRayBeamCoroutine(beam, holderCamera, target, type, shrinkRayAudio, onComplete));
            activeBeams.Add(beam, coroutine);

            return beam;
        }

        private IEnumerator RenderRayBeamCoroutine(GameObject beam, Transform holderCamera, Transform target, ModificationType? type = null, AudioSource shrinkRayAudio = null, Action onComplete = null)
        {
            visualEffect = beam.GetComponentInChildren<VisualEffect>();
            if (!visualEffect)
            {
                Plugin.Log("Shrink Ray VFX Null Error: Couldn't get VisualEffect component", Plugin.LogType.Error);
                RemoveBeam(beam);
                yield break;
            }

            switch (type.GetValueOrDefault())
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
                default:
                    break;
            }

            if (usingOffsets && target.gameObject.TryGetComponent(out PlayerControllerB targetPlayer)) // For players target the head
            {
                Transform targetHeadTransform = targetPlayer?.gameplayCamera?.transform?.Find("HUDHelmetPosition")?.transform;
                if (targetHeadTransform != null)
                    target = targetHeadTransform;
                else
                    Plugin.Log("Failed to get target players helmet position for shrink ray vfx", Plugin.LogType.Warning);
            }

            // Stole this from above, minor adjustments to where the beam comes from
            Vector3 beamStartPos = this.transform.position;
            if (usingOffsets)
                beamStartPos += (Vector3.up * 0.25f) + (holderCamera.forward * -0.1f);

            // Set bezier 1 (start point)
            Transform bezier1 = beam.transform.GetChild(0)?.Find("Pos1");
            if (bezier1 != null)
            {
                bezier1.transform.position = beamStartPos;
                bezier1.transform.SetParent(this.transform, true);
            }

            // Set bezier 2 (curve)
            Transform bezier2 = beam.transform.GetChild(0)?.Find("Pos2");
            if (bezier2 != null)
            {
                bezier2.transform.position = Vector3.Lerp(beamStartPos, target.position, bezier2YPoint) + (Vector3.up * bezier2YOffset);
                bezier2.transform.SetParent(this.transform, true);
            }

            // Set bezier 3 (curve)
            Transform bezier3 = beam.transform.GetChild(0)?.Find("Pos3");
            if (bezier3 != null)
            {
                bezier3.transform.position = Vector3.Lerp(beamStartPos, target.position, bezier3YPoint) + (Vector3.up * bezier3YOffset);
                bezier3.transform.SetParent(target, true);
            }

            // Set Bezier 4 (final endpoint)
            Vector3 beamEndPos = target.position;
            Transform bezier4 = beam.transform.GetChild(0)?.Find("Pos4");
            if (bezier4 != null)
            {
                bezier4.transform.position = beamEndPos;
                bezier4.transform.SetParent(target, true);
            }

            Plugin.Log("Beam created from " + beamStartPos + " to " + beamEndPos);

            if (shrinkRayAudio != null)
            {
                shrinkRayAudio.Stop();
                shrinkRayAudio.PlayOneShot(beamSFX);
            }

            if (beamDuration > 0f)
            {
                yield return new WaitForSeconds(beamDuration);
                RemoveBeam(beam);
            }

            if (onComplete != null)
                onComplete();
        }
        #endregion

        #region Setter
        private void SetFloat(string name, float value)
        {
            if (visualEffect == null)
                defaultVisualEffect.SetFloat(name, value);
            else
                visualEffect.SetFloat(name, value);
        }

        private void SetVector4(string name, Vector4 value)
        {
            if (visualEffect == null)
                defaultVisualEffect.SetVector4(name, value);
            else
                visualEffect.SetVector4(name, value);
        }

        private void SetInt(string name, int value)
        {
            if (visualEffect == null)
                defaultVisualEffect.SetInt(name, value);
            else
                visualEffect.SetInt(name, value);
        }
        #endregion
    }
}