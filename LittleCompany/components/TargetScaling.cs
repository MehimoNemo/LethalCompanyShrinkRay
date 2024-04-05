using GameNetcodeStuff;
using System;
using System.Collections;
using UnityEngine;

using LittleCompany.helper;
using LittleCompany.modifications;
using LittleCompany.patches;
using LittleCompany.events.enemy;
using LittleCompany.compatibility;

namespace LittleCompany.components
{
    internal abstract class TargetScaling<T> : MonoBehaviour where T : Component
    {
        #region Properties
        internal Vector3 OriginalOffset = Vector3.zero;
        internal Vector3 OriginalScale = Vector3.one;

        public float RelativeScale = 1f;

        public bool GettingScaled => ScaleRoutine != null;
        private Coroutine ScaleRoutine = null;

        internal T target;
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

            OnAwake();
        }

        void OnDestroy() => Reset();
        #endregion

        #region Methods
        internal virtual void OnAwake() { }
        public virtual void ScaleTo(float scale)
        {
            gameObject.transform.localScale = OriginalScale * scale;
            RelativeScale = scale;
        }

        public virtual void ScaleOverTimeTo(float scale, Action onComplete = null)
        {
            ScaleRoutine = StartCoroutine(ScaleOverTimeToCoroutine(scale, () =>
            {
                ScaleRoutine = null;

                // Ensure final scale is set to the desired value
                ScaleTo(scale);
                
                if (onComplete != null)
                    onComplete();
            }));
        }

        private IEnumerator ScaleOverTimeToCoroutine(float scale, Action onComplete = null)
        {
            float elapsedTime = 0f;
            var c = RelativeScale;
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
            return OriginalScale * percentage;
        }

        public bool Unchanged => OriginalScale == gameObject.transform.localScale;

        public virtual void Reset()
        {
            gameObject.transform.localScale = OriginalScale;
            RelativeScale = 1f;
        }
        #endregion
    }

    internal class PlayerScaling : TargetScaling<PlayerControllerB>
    {
        #region Methods
        internal Vector3 armOffset = Vector3.zero;
        internal ModelReplacementApiCompatibility modelReplacementApiCompatibility;

        internal override void OnAwake()
        {
            modelReplacementApiCompatibility = new ModelReplacementApiCompatibility(target);
            OriginalScale = Vector3.one;
            RelativeScale = PlayerInfo.SizeOf(target);
        }

        public override void ScaleOverTimeTo(float scale, Action onComplete = null)
        {
            base.ScaleOverTimeTo(scale, onComplete);
        }

        public override void ScaleTo(float scale)
        {
            if (target == null || target.transform == null)
                return;
            var wasShrunkenBefore = PlayerInfo.IsShrunk(target);
            base.ScaleTo(scale);
            CompatibilityAfterEachScale(scale);
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

                var isShrunk = PlayerInfo.IsShrunk(target);
                if (wasShrunkenBefore != isShrunk)
                {
                    if (isShrunk)
                        PlayerModification.TransitionedToShrunk(target);
                    else
                        PlayerModification.TransitionedFromShrunk(target);
                }
            }
            if (!GettingScaled) // Execute at the very end
            {
                GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(target);
                PlayerInfo.RebuildRig(target);
                CompatibilityAtEndOfScaling();
            }
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
        public override void ScaleTo(float scale)
        {
            base.ScaleTo(scale);

            if (!GettingScaled && target != null)
                target.originalScale = gameObject.transform.localScale;
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
        public override void ScaleTo(float scale)
        {
            var previousScale = RelativeScale;
            base.ScaleTo(scale);

            if (!GettingScaled)
                EnemyEventManager.EventHandlerOf(target)?.SizeChanged(previousScale, scale);
        }
    }
}
