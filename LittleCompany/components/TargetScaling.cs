using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.modifications;
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
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            target = gameObject.GetComponent<T>();

            OriginalSize = gameObject.transform.localScale;

            OnAwake();
        }

        void OnDestroy() => Reset();
        #endregion

        #region Methods
        internal virtual void OnAwake() { }
        public virtual void ScaleTo(float scale, bool permanently = false)
        {
            gameObject.transform.localScale = OriginalSize * scale;
            CurrentScale = scale;

            if(permanently)
                OriginalSize = gameObject.transform.localScale;
        }

        public virtual void ScaleOverTimeTo(float scale, Action onComplete = null, bool permanently = false)
        {
            ScaleRoutine = StartCoroutine(ScaleOverTimeToCoroutine(scale, () =>
            {
                ScaleRoutine = null;

                // Ensure final scale is set to the desired value
                ScaleTo(scale, permanently);
                
                if (onComplete != null)
                    onComplete();
            }));
        }

        private IEnumerator ScaleOverTimeToCoroutine(float scale, Action onComplete = null)
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
        internal Vector3 armOffset = Vector3.zero;

        public override void ScaleOverTimeTo(float scale, Action onComplete = null, bool permanently = false)
        {
            if(PlayerInfo.IsCurrentPlayer(target))
                armOffset = PlayerInfo.CalcLocalArmScale();
            base.ScaleOverTimeTo(scale, onComplete, permanently);
        }

        public override void ScaleTo(float scale, bool permanently = false)
        {
            var wasShrunkenBefore = PlayerInfo.IsShrunk(target);

            base.ScaleTo(scale, permanently);

            if (PlayerInfo.IsCurrentPlayer(target))
            {
                // scale arms & visor
                PlayerInfo.ScaleLocalPlayerBodyParts();

                var heldItem = PlayerInfo.CurrentPlayerHeldItem;
                if (heldItem != null)
                {
                    var currentArmOffset = PlayerInfo.CalcLocalArmScale();
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, scale, (armOffset - currentArmOffset) / 2);
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

            if(!GettingScaled) // Execute at the very end
                GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(target);
        }
        #endregion
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        #region Methods
        internal override void OnAwake()
        {
            OriginalOffset = target.itemProperties.positionOffset;
        }

        public void ScaleTo(float scale, bool permanently = false, Vector3 additionalOffset = new Vector3())
        {
            base.ScaleTo(scale, permanently);
            if (permanently)
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
