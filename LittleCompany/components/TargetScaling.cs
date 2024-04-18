using GameNetcodeStuff;
using System;
using System.Collections;
using UnityEngine;

using LittleCompany.helper;
using LittleCompany.modifications;
using LittleCompany.patches;
using LittleCompany.events.enemy;
using System.Collections.Generic;

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
            float previousScale = RelativeScale;
            gameObject.transform.localScale = OriginalScale * scale;
            RelativeScale = scale;
            CallListenersAfterEachScale(previousScale, scale, scaledBy);
        }

        public virtual void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null)
        {
            ScaleRoutine = StartCoroutine(ScaleOverTimeToCoroutine(scale, scaledBy, () =>
            {
                ScaleRoutine = null;

                // Ensure final scale is set to the desired value
                ScaleTo(scale, scaledBy);
                
                if (onComplete != null)
                {
                    CallListenersAtEndOfScaling();
                    onComplete();
                }
            }));
        }

        private IEnumerator ScaleOverTimeToCoroutine(float scale, PlayerControllerB scaledBy, Action onComplete = null)
        {
            float elapsedTime = 0f;
            var c = RelativeScale;
            var direction = scale < c ? -1f : 1f;
            float a = Mathf.Abs(c - scale); // difference
            const float b = -0.5f;

            while (elapsedTime < ShrinkRayFX.DefaultBeamDuration)
            {
                // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                var x = elapsedTime;
                var newScale = direction * (a + 1f) * Mathf.Pow(x / 2f, 2f) + (x * b * direction) + c;
                ScaleTo(newScale, scaledBy);

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
            }
        }
        #endregion
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        public Item originalItemProperties;

        internal override void OnAwake()
        {
            originalItemProperties = target.itemProperties;
        }

        #region Methods
        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            base.ScaleTo(scale, scaledBy);
            if (originalItemProperties != null)
            {
                if (Modification.Rounded(scale) == 1)
                {
                    // If normalized, reset to original item properties
                    ResetItemProperties();
                }
                else
                {
                    OverrideItemProperties();
                    RecalculateOffset(scale);
                }
            }

            if (!GettingScaled && target != null)
                target.originalScale = gameObject.transform.localScale;
        }
        private void ResetItemProperties()
        {
            if (target.itemProperties != originalItemProperties)
            {
                Destroy(target.itemProperties);
            }
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
