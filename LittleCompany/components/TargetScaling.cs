using GameNetcodeStuff;
using LittleCompany.compatibility;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using System.Collections;
using UnityEngine;

namespace LittleCompany.components
{
    internal abstract class TargetScaling<T> : MonoBehaviour where T : Component
    {
        #region Properties
        internal Vector3 OriginalOffset = Vector3.zero;
        internal Vector3 OriginalSize = Vector3.one;

        public float CurrentScale = 1f;

        public bool GettingScaled => ScaleRoutine != null;
        private Coroutine ScaleRoutine = null;

        internal T target;
        #endregion

        #region Base Methods
        void Awake()
        {
            if (gameObject == null || !gameObject.TryGetComponent(out target))
            {
                Plugin.Log("TargetScaling -> target not found!", Plugin.LogType.Error);
                return;
            }

            OriginalSize = gameObject.transform.localScale;

            if(gameObject.TryGetComponent(out GrabbableObject item))
                OriginalOffset = item.itemProperties.positionOffset;
            OnAwake();
        }

        internal virtual void OnAwake() { }

        void OnDestroy() => Reset();
        #endregion

        #region Methods
        public virtual void ScaleTo(float scale = 1f, bool overrideOriginalSize = false)
        {
            gameObject.transform.localScale = OriginalSize * scale;
            CurrentScale = scale;

            if (overrideOriginalSize)
            {
                OriginalSize = gameObject.transform.localScale;
                CurrentScale = 1f;
            }
        }

        public virtual void ScaleOverTimeTo(float scale, Action onComplete = null, bool overrideOriginalSize = false)
        {
            ScaleRoutine = StartCoroutine(ScaleOverTimeToCoroutine(scale, onComplete, overrideOriginalSize));
        }

        private IEnumerator ScaleOverTimeToCoroutine(float scale, Action onComplete = null, bool overrideOriginalSize = false)
        {
            float elapsedTime = 0f;
            var c = CurrentScale;
            var direction = scale < c ? -1f : 1f;
            float a = Mathf.Abs(c - scale); // difference
            const float b = -0.5f;

            while (elapsedTime < ShrinkRayFX.beamDuration)
            {
                // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                var x = elapsedTime;
                var newScale = direction * (a + 1f) * Mathf.Pow(x / 2f, 2f) + (x * b * direction) + c;
                ScaleTo(newScale);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            ScaleTo(scale, overrideOriginalSize);

            ScaleRoutine = null;
            if (onComplete != null)
                onComplete();
        }

        public void StopScaling()
        {
            if (GettingScaled)
                StopCoroutine(ScaleRoutine);
        }

        public virtual Vector3 SizeAt(float percentage)
        {
            return OriginalSize * percentage;
        }

        public bool Unchanged => OriginalSize == gameObject.transform.localScale;

        public virtual void Reset()
        {
            gameObject.transform.localScale = OriginalSize;
            CurrentScale = 1f;
        }
        #endregion
    }

    internal class PlayerScaling : TargetScaling<PlayerControllerB>
    {
        internal ModelReplacementApiCompatibility modelReplacementApiCompatibility;

        #region Methods
        internal override void OnAwake()
        {
            modelReplacementApiCompatibility = new ModelReplacementApiCompatibility(target);
            OriginalSize = Vector3.one;
            CurrentScale = PlayerInfo.SizeOf(target);
        }

        public override void ScaleTo(float scale = 1f, bool saveAsIntendedSize = false)
        {
            base.ScaleTo(scale);
            CompatibilityAfterEachScale(scale);
            if (target?.playerClientId == PlayerInfo.CurrentPlayer?.playerClientId)
            {
                // scale arms & visor
                PlayerInfo.ScaleLocalPlayerBodyParts();
            }
        }

        public override void ScaleOverTimeTo(float scale, Action onComplete = null, bool overrideOriginalSize = false)
        {
            base.ScaleOverTimeTo(scale, () =>
            {
                if (target?.playerClientId == PlayerInfo.CurrentPlayer?.playerClientId)
                {
                    PlayerInfo.ScaleLocalPlayerBodyParts();
                    if (PlayerInfo.CurrentPlayerHeldItem != null)
                    {
                        ScreenBlockingGrabbablePatch.CheckForGlassify(PlayerInfo.CurrentPlayerHeldItem);
                    }
                    if (scale != 1f)
                        PlayerMultiplierPatch.Modify(scale);
                    else
                        PlayerMultiplierPatch.Reset();
                }
                PlayerInfo.RebuildRig(target);
                CompatibilityAtEndOfScaling();
                if (onComplete != null)
                    onComplete();
            }, overrideOriginalSize);
        }

        private void CompatibilityAfterEachScale(float scale)
        {
            if (ModelReplacementApiCompatibility.enabled)
            {
                modelReplacementApiCompatibility.AdjustToSize(scale);
            }
        }

        private void CompatibilityAtEndOfScaling()
        {
            if (ModelReplacementApiCompatibility.enabled)
            {
                modelReplacementApiCompatibility.ReloadCurrentReplacementModel();
            }
        }
        #endregion
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        #region Methods
        public void ScaleTo(float scale = 1f, bool saveAsIntendedSize = false, Vector3 additionalOffset = new Vector3())
        {
            base.ScaleTo(scale, saveAsIntendedSize);
            if (saveAsIntendedSize)
                target.originalScale = OriginalSize;

            if (target != null)
                target.itemProperties.positionOffset = OriginalOffset * scale + additionalOffset;
        }

        public override void Reset()
        {
            base.Reset();

            if(target != null)
                target.itemProperties.positionOffset = OriginalOffset;
        }
        #endregion
    }
}
