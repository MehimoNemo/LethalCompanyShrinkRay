using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LittleCompany.components
{
    internal abstract class TargetScaling<T> : MonoBehaviour where T : Component
    {
        internal Vector3 OriginalOffset = Vector3.zero;
        internal Vector3 OriginalSize = Vector3.one;

        public float CurrentScale = 1f;

        public bool GettingScaled => ScaleRoutine != null;
        private Coroutine ScaleRoutine = null;

        internal T target;

        void Awake()
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            target = gameObject.GetComponent<T>();

            OriginalSize = gameObject.transform.localScale;

            if(gameObject.TryGetComponent(out GrabbableObject item))
                OriginalOffset = item.itemProperties.positionOffset;
        }

        public virtual void ScaleTo(float scale = 1f, bool overrideOriginalSize = false)
        {
            Plugin.Log("ScaleTo -> " + scale + "[" + overrideOriginalSize + "]");
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

        public IEnumerator ScaleOverTimeToCoroutine(float scale, Action onComplete = null, bool overrideOriginalSize = false)
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

        void OnDestroy()
        {
            Reset();
        }
    }

    internal class PlayerScaling : TargetScaling<PlayerControllerB>
    {
        public override void ScaleTo(float scale = 1f, bool saveAsIntendedSize = false)
        {
            base.ScaleTo(scale);
            if (target?.playerClientId == PlayerInfo.CurrentPlayer?.playerClientId)
            {
                // scale arms & visor
                PlayerInfo.ScaleLocalPlayerBodyParts();
                if (PlayerInfo.CurrentPlayerHeldItem != null)
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(PlayerInfo.CurrentPlayerHeldItem, scale);
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
                        ScreenBlockingGrabbablePatch.TransformItemRelativeTo(PlayerInfo.CurrentPlayerHeldItem, scale);
                        ScreenBlockingGrabbablePatch.CheckForGlassify(PlayerInfo.CurrentPlayerHeldItem);
                    }
                    if (scale != 1f)
                        PlayerMultiplierPatch.Modify(scale);
                    else
                        PlayerMultiplierPatch.Reset();
                }

                if (onComplete != null)
                    onComplete();
            }, overrideOriginalSize);
        }
    }

    internal class ItemScaling : TargetScaling<GrabbableObject>
    {
        public void ScaleTo(float scale = 1f, bool saveAsIntendedSize = false, Vector3 additionalOffset = new Vector3())
        {
            base.ScaleTo(scale, saveAsIntendedSize);

            if (target != null)
                target.itemProperties.positionOffset = OriginalOffset * scale + additionalOffset;
        }

        public override void Reset()
        {
            base.Reset();

            if(target != null)
                target.itemProperties.positionOffset = OriginalOffset;
        }
    }
}
