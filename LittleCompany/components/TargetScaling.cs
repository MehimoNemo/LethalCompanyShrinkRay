using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.modifications;
using LittleCompany.patches;
using System;
using System.Collections;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

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
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            target = gameObject.GetComponent<T>();

            OriginalSize = gameObject.transform.localScale;
        }

        void OnDestroy() => Reset();
        #endregion

        #region Methods
        public virtual void ScaleTo(float scale = 1f, bool overrideOriginalSize = false, bool scalingFinished = true)
        {
            gameObject.transform.localScale = OriginalSize * scale;
            CurrentScale = scale;

            if (overrideOriginalSize)
                OriginalSize = gameObject.transform.localScale;
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
                ScaleTo(newScale, false, false);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            ScaleTo(scale, overrideOriginalSize, true);

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
        #region Methods
        public override void ScaleTo(float scale = 1f, bool saveAsIntendedSize = false, bool scalingFinished = true)
        {
            var wasShrunkenBefore = PlayerInfo.IsShrunk(target);

            base.ScaleTo(scale, saveAsIntendedSize, scalingFinished);

            if (PlayerInfo.IsCurrentPlayer(target))
            {
                // scale arms & visor
                PlayerInfo.ScaleLocalPlayerBodyParts();

                var heldItem = PlayerInfo.CurrentPlayerHeldItem;
                if (heldItem != null)
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, scale);

                var isShrunk = PlayerInfo.IsShrunk(target);
                if (wasShrunkenBefore != isShrunk)
                {
                    if (isShrunk)
                        PlayerModification.TransitionedToShrunk(target);
                    else
                        PlayerModification.TransitionedFromShrunk(target);
                }

                if (scalingFinished)
                {
                    if (heldItem != null)
                        ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);

                    GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(target);
                }
            }
        }
        #endregion
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        #region Methods
        void Awake()
        {
            OriginalOffset = target.itemProperties.positionOffset;
        }

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

            if (target != null)
                target.itemProperties.positionOffset = OriginalOffset;
        }
        #endregion
    }

    internal class EnemyScaling : TargetScaling<EnemyAI>
    {
    }
}
