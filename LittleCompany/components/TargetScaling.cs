using GameNetcodeStuff;
using System;
using System.Collections;
using UnityEngine;

using LittleCompany.helper;
using LittleCompany.modifications;
using LittleCompany.patches;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine.PlayerLoop;

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
    internal abstract class TargetScaling<T> : MonoBehaviour where T : Component
    {
        #region Properties
        internal Vector3 OriginalOffset = Vector3.zero;
        internal Vector3 OriginalScale = Vector3.one;

        public float RelativeScale = 1f;
        internal HashSet<IScalingListener> scalingListeners;

        public bool GettingScaled => ScaleRoutine != null;
        private Coroutine ScaleRoutine = null;

        internal T target;

        public float ScalingProgress { get; private set; } = 0f; // progress from 0 to 1

        public enum Mode
        {
            Wave = 0,
            Linear
        }
        #endregion

        #region Base Methods
        void Awake()
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            target = gameObject.GetComponent<T>();

            OriginalScale = gameObject.transform.localScale;

            scalingListeners = [];
            DetectAndLoadCurrentListeningComponent();

            OnAwake();
        }

        void OnDestroy() => Reset();
        #endregion

        #region Methods
        internal virtual void OnAwake() { }
        public virtual void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            scale = Mathf.Max(scale, 0f);

            float previousScale = RelativeScale;
            gameObject.transform.localScale = OriginalScale * scale;
            RelativeScale = scale;
            CallListenersAfterEachScale(previousScale, scale, scaledBy);
        }

        // TODO: Rework.. this has too many parameters..
        public virtual void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float ? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            Plugin.Log("Duration: " + duration);
            ScaleRoutine = StartCoroutine(ScaleOverTimeToCoroutine(scale, scaledBy, duration.GetValueOrDefault(ShrinkRayFX.DefaultBeamDuration), mode.GetValueOrDefault(Mode.Wave), startingFromScale, () =>
            {
                ScaleRoutine = null;

                // Ensure final scale is set to the desired value
                ScaleTo(scale, scaledBy);

                CallListenersAtEndOfScaling();

                if (onComplete != null)
                    onComplete();

                ScalingProgress = 0f;
            }));
        }

        private IEnumerator ScaleOverTimeToCoroutine(float scale, PlayerControllerB scaledBy, float duration, Mode mode, float? startingFromScale = null, Action onComplete = null)
        {
            float elapsedTime = 0f;

            var startingScale = startingFromScale.GetValueOrDefault(RelativeScale);
            var direction = scale < startingScale ? -1f : 1f;
            float scaleDiff = Mathf.Abs(startingScale - scale); // difference

            while (elapsedTime < duration)
            {
                ScalingProgress = Mathf.Min(1f / duration * elapsedTime, 1f);
                float newScale = 0f;
                switch(mode)
                {
                    case Mode.Wave:
                        // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                        newScale = direction * (scaleDiff + 1f) * Mathf.Pow(elapsedTime / 2f, 2f) + (elapsedTime * -0.5f * direction) + startingScale; // todo: duration not accurate rn
                        break;
                    case Mode.Linear:
                        newScale = Mathf.Lerp(startingScale, scale, elapsedTime / duration);
                        break;
                }

                ScaleTo(newScale, scaledBy);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            ScalingProgress = 1f;

            if (onComplete != null)
                onComplete();
        }

        public void StopScaling()
        {
            if (GettingScaled)
            {
                StopCoroutine(ScaleRoutine);
                ScalingProgress = 0f;
            }
        }

        public virtual Vector3 SizeAt(float percentage)
        {
            return OriginalScale * percentage;
        }

        public bool Unchanged => OriginalScale == gameObject.transform.localScale;

        public virtual void Reset()
        {
            gameObject.transform.localScale = OriginalScale;
            RelativeScale = 1f;
        }

        private void DetectAndLoadCurrentListeningComponent()
        {
            foreach (Component component in GetComponents(typeof(IScalingListener)))
            {
                AddListener((IScalingListener)component);
            }
        }

        public void AddListener(IScalingListener listener)
        {
            scalingListeners?.Add(listener);
        }

        public void RemoveListener(IScalingListener listener)
        {
            scalingListeners?.Remove(listener);
        }

        public void CallListenersAfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            foreach (IScalingListener listener in scalingListeners)
            {
                listener.AfterEachScale(from, to, playerBy);
            }
        }

        public void CallListenersAtEndOfScaling()
        {
            foreach (IScalingListener listener in scalingListeners)
            {
                listener.AtEndOfScaling();
            }
        }
        #endregion
    }

    internal class PlayerScaling : TargetScaling<PlayerControllerB>
    {
        #region Methods
        internal Vector3 armOffset = Vector3.zero;

        internal override void OnAwake()
        {
            OriginalScale = Vector3.one;
            RelativeScale = PlayerInfo.SizeOf(target);
        }

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            if (target == null || target.transform == null)
                return;

            var wasShrunkenBefore = PlayerInfo.IsShrunk(target);
            base.ScaleTo(scale, scaledBy);
            if (PlayerInfo.IsCurrentPlayer(target))
            {
                // scale arms & visor
                PlayerInfo.ScaleLocalPlayerBodyParts();

                var heldItem = PlayerInfo.CurrentPlayerHeldItem;
                if (heldItem != null)
                {
                    if (!GettingScaled) // Only check at the very end
                        ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
            }

            var isShrunk = PlayerInfo.IsShrunk(target);
            if (wasShrunkenBefore != isShrunk)
            {
                if (isShrunk)
                    PlayerModification.TransitionedToShrunk(target);
                else
                    PlayerModification.TransitionedFromShrunk(target);
            }

            if (!GettingScaled) // Execute at the very end
            {
                GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(target);
                PlayerInfo.RebuildRig(target);
            }
        }
        #endregion
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        public Item originalItemProperties;

        public GameObject hologram = null;
        public Coroutine hologramCoroutine = null;

        public float DesiredScale = 0f;
        public PlayerControllerB playerLastScaledBy = null;

        public int originalScrapValue = 0;

        internal override void OnAwake()
        {
            DesiredScale = RelativeScale;
            originalItemProperties = target.itemProperties;
            originalScrapValue = target.scrapValue;

            // Hologram
            Plugin.Log("Instantiating hologram of " + target.name);
            hologram = ItemInfo.visualCopyOf(originalItemProperties);
            //hologram.transform.SetParent(target.transform, true);

            Materials.ReplaceAllMaterialsWith(hologram, (Material _) => Materials.Wireframe);

            hologram.SetActive(false);
        }

        private void Update()
        {
            if (hologram != null && target != null)
            {
                hologram.transform.position = target.transform.position;
                hologram.transform.rotation = target.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            Plugin.Log("TargetScaling.ondestroy");
            RemoveHologram();
        }

        public void RemoveHologram()
        {
            if (hologramCoroutine == null) return; // Not getting scaled

            DestroyImmediate(hologram);
            StopCoroutine(hologramCoroutine);
            hologramCoroutine = null;
            DesiredScale = RelativeScale;
        }

        #region Methods
        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            scale = Mathf.Max(scale, 0f);
            playerLastScaledBy = scaledBy;

            DesiredScale = scale;
            hologram.transform.localScale = OriginalScale * DesiredScale;

            if (DesiredScale > RelativeScale)
            {
                if (hologramCoroutine == null)
                    hologramCoroutine = StartCoroutine(HologramScaleCoroutine());
            }
            else
            {
                base.ScaleTo(scale, scaledBy);
                UpdatePropertiesBasedOnScale();
            }
        }

        private void UpdatePropertiesBasedOnScale()
        {
            //Plugin.Log("UpdatePropertiesBasedOnScale");

            // Item properties
            if (originalItemProperties != null)
            {
                if (Modification.Rounded(RelativeScale) == 1)
                {
                    // If normalized, reset to original item properties
                    ResetItemProperties();
                }
                else
                {
                    OverrideItemProperties();
                    RecalculateOffset(RelativeScale);
                }
            }

            // Weight
            var lastWeight = target.itemProperties.weight;
            target.itemProperties.weight = 1f + ((originalItemProperties.weight - 1f) * RelativeScale);
            var diff = target.itemProperties.weight - lastWeight;

            if (target.playerHeldBy != null)
                target.playerHeldBy.carryWeight += diff;

            // Scrap value
            target.SetScrapValue((int)(originalScrapValue * RelativeScale));
        }

        public override void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            base.ScaleOverTimeTo(scale, scaledBy, onComplete, duration, mode, DesiredScale);
        }

        private IEnumerator HologramScaleCoroutine()
        {
            Plugin.Log("Starting hologram scale routine for " + target.name);
            hologram.SetActive(true);

            while(RelativeScale < DesiredScale)
            {
                //Plugin.Log("Going from scale " + RelativeScale + " to desired scale " + DesiredScale);
                float previousScale = RelativeScale;
#if DEBUG
                RelativeScale += Time.deltaTime / 20;
#else
                RelativeScale += Time.deltaTime / 20 * RelativeScale;
#endif

                gameObject.transform.localScale = OriginalScale * RelativeScale;

                UpdatePropertiesBasedOnScale();

                CallListenersAfterEachScale(previousScale, RelativeScale, playerLastScaledBy);

                yield return null;
            }

            Plugin.Log("Finished hologram scale routine for " + target.name);

            target.originalScale = gameObject.transform.localScale;

            hologram.SetActive(false);

            hologramCoroutine = null;

            yield return null;
        }

        private void ResetItemProperties()
        {
            if (target.itemProperties == originalItemProperties) return;

            Destroy(target.itemProperties);
            target.itemProperties = originalItemProperties;
        }

        private void OverrideItemProperties()
        {
            if (target.itemProperties == originalItemProperties)
            {
                // itemProperties is not overriden, overrides it
                target.itemProperties = Instantiate(originalItemProperties);
            }
        }

        private void RecalculateOffset(float scale)
        {
            target.itemProperties.positionOffset = originalItemProperties.positionOffset * scale;
        }

        public void ScaleTemporarlyTo(float scale)
        {
            gameObject.transform.localScale = OriginalScale * scale;
        }

        public override void Reset()
        {
            gameObject.transform.localScale = target.originalScale;
        }
#endregion
    }

    internal class EnemyScaling : TargetScaling<EnemyAI>
    {
    }
}
