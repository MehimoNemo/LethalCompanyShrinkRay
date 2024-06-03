using GameNetcodeStuff;
using System;
using System.Collections;
using UnityEngine;

using LittleCompany.helper;
using LittleCompany.modifications;
using LittleCompany.patches;
using System.Collections.Generic;
using LittleCompany.Config;

namespace LittleCompany.components
{
    [DisallowMultipleComponent]
    internal abstract class TargetScaling<T> : MonoBehaviour where T : Component
    {
        #region Properties
        internal Vector3 OriginalOffset = Vector3.zero;
        internal abstract Vector3 OriginalScale { get; }
        public float RelativeScale = 1f;

        private T _target;
        internal T Target
        {
            get
            {
                if(_target == null )
                    _target = gameObject.GetComponent<T>();

                return _target;
            }
        }

        internal HashSet<IScalingListener> scalingListeners;

        public bool GettingScaled { get; private set; } = false;

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

            RelativeScale = 1f / OriginalScale.y * gameObject.transform.localScale.y;

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
            float previousScale = RelativeScale;
            GettingScaled = true;
            StartCoroutine(ScaleOverTimeToCoroutine(scale, scaledBy, duration.GetValueOrDefault(ShrinkRayFX.DefaultBeamDuration), mode.GetValueOrDefault(Mode.Wave), startingFromScale, () =>
            {
                GettingScaled = false;

                // Ensure final scale is set to the desired value
                ScaleTo(scale, scaledBy);

                CallListenersAtEndOfScaling(previousScale, scale, scaledBy);

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
                StopAllCoroutines();
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

        public void CallListenersAtEndOfScaling(float from, float to, PlayerControllerB playerBy)
        {
            foreach (IScalingListener listener in scalingListeners)
            {
                listener.AtEndOfScaling(from, to, playerBy);
            }
        }
        #endregion
    }

    internal class PlayerScaling : TargetScaling<PlayerControllerB>
    {
        #region Methods
        internal Vector3 armOffset = Vector3.zero;

		internal override Vector3 OriginalScale => Vector3.one;

        internal override void OnAwake()
        {
            RelativeScale = PlayerInfo.SizeOf(Target);
        }

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            if (Target == null || Target.transform == null)
                return;

            var wasShrunkenBefore = PlayerInfo.IsShrunk(Target);
            base.ScaleTo(scale, scaledBy);
            if (PlayerInfo.IsCurrentPlayer(Target))
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

            var isShrunk = PlayerInfo.IsShrunk(Target);
            if (wasShrunkenBefore != isShrunk)
            {
                if (isShrunk)
                    PlayerModification.TransitionedToShrunk(Target);
                else
                    PlayerModification.TransitionedFromShrunk(Target);
            }

            if (!GettingScaled) // Execute at the very end
            {
                Plugin.Log("Reached end of scaling");
                GrabbablePlayerList.UpdateWhoIsGrabbableFromPerspectiveOf(Target);
                PlayerInfo.RebuildRig(Target);

                if(PlayerInfo.IsCurrentPlayer(Target))
                    AudioPatches.UpdateEnemyPitches();
            }
        }

        public override void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            if (GrabbablePlayerList.TryFindGrabbableObjectForPlayer(Target.playerClientId, out GrabbablePlayerObject gpo))
                gpo.EnableInteractTrigger(false); // UpdateInteractTrigger happening in UpdateWhoIsGrabbableFromPerspectiveOf after scaling already

            base.ScaleOverTimeTo(scale, scaledBy, onComplete, duration, mode, startingFromScale);
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

        private Vector3? initialScale = null;
        internal override Vector3 OriginalScale
        {
            get
            {
                if (initialScale == null)
                    initialScale = Target.originalScale;

                return initialScale.Value;
            }
        }

        internal override void OnAwake()
        {
            DesiredScale = RelativeScale;
            originalItemProperties = Target.itemProperties;
            OverrideItemProperties();
            originalScrapValue = Target.scrapValue;

            // Hologram
            Plugin.Log("Instantiating hologram of " + Target.name);
            hologram = ItemInfo.visualCopyOf(originalItemProperties);
            if (hologram != null)
            {
                //hologram.transform.SetParent(Target.transform, true);

                Materials.ReplaceAllMaterialsWith(hologram, (Material _) => Materials.Wireframe);

                hologram.SetActive(false);
            }
        }

        private void Update()
        {
            if (hologram != null && Target != null)
            {
                hologram.transform.position = Target.transform.position;
                hologram.transform.rotation = Target.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            RemoveHologram();
            ResetItemProperties();
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
        public void ScaleToImmediate(float scale, PlayerControllerB scaledBy)
        {
            scale = Mathf.Max(scale, 0f);
            playerLastScaledBy = scaledBy;
            DesiredScale = RelativeScale = scale;

            base.ScaleTo(scale, scaledBy);
            Target.originalScale = OriginalScale * scale;
            UpdatePropertiesBasedOnScale();
        }

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            scale = Mathf.Max(scale, 0f);
            playerLastScaledBy = scaledBy;

            DesiredScale = scale;
            if (hologram != null)
                hologram.transform.localScale = OriginalScale * DesiredScale;

            if (hologram != null && DesiredScale > RelativeScale)
            {
                if (hologramCoroutine == null)
                    hologramCoroutine = StartCoroutine(HologramScaleCoroutine());
            }
            else
            {
                base.ScaleTo(scale, scaledBy);
                Target.originalScale = OriginalScale * scale;
                UpdatePropertiesBasedOnScale();
            }
        }

        private void UpdatePropertiesBasedOnScale()
        {
            if (ModConfig.Instance.values.itemScalingVisualOnly)
                return;

            // Item properties
            if (!Mathf.Approximately(Modification.Rounded(RelativeScale), 1f))
                RecalculateOffset(RelativeScale);

            // Hands
            /*float holderRequired = (originalItemProperties.twoHanded ? 1f : 0f) + RelativeScale;
            Target.itemProperties.twoHanded = holderRequired >= 2f;*/

            // Handanimation
            /*bool usingTwoHandAnimation = originalItemProperties.twoHandedAnimation && RelativeScale > 0.5f;
            if (usingTwoHandAnimation != Target.itemProperties.twoHandedAnimation && Target.playerHeldBy != null)
            {
                Target.playerHeldBy.playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
                Target.playerHeldBy.playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");

                Target.playerHeldBy.playerBodyAnimator.SetTrigger("SwitchHoldAnimation");

                if(usingTwoHandAnimation)
                    Target.playerHeldBy.playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
            }

            Target.itemProperties.twoHandedAnimation = usingTwoHandAnimation;*/

            // Weight
            var lastWeight = Target.itemProperties.weight;
            Target.itemProperties.weight = 1f + ((originalItemProperties.weight - 1f) * RelativeScale);
            var diff = Target.itemProperties.weight - lastWeight;

            if (Target.playerHeldBy != null)
                Target.playerHeldBy.carryWeight += diff;

            // Scrap value
            if(originalScrapValue > 0)
                Target.SetScrapValue((int)(originalScrapValue * Mathf.Max(1f + (RelativeScale - 1f) * 0.1f, 2f))); // Maximum twice the value at 10x size
        }

        public override void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            base.ScaleOverTimeTo(scale, scaledBy, onComplete, duration, mode, DesiredScale);
        }

        private IEnumerator HologramScaleCoroutine()
        {
            Plugin.Log("Starting hologram scale routine for " + Target.name);
            hologram.SetActive(true);

            while(RelativeScale < DesiredScale)
            {
                //Plugin.Log("Going from scale " + RelativeScale + " to desired scale " + DesiredScale);
                float previousScale = RelativeScale;

#if DEBUG
                RelativeScale += Time.deltaTime / 2.5f;
#else
                RelativeScale += Time.deltaTime / (20 * RelativeScale);
#endif
                var newScale = OriginalScale * RelativeScale;
                gameObject.transform.localScale = newScale;
                Target.originalScale = newScale;

                UpdatePropertiesBasedOnScale();

                CallListenersAfterEachScale(previousScale, RelativeScale, playerLastScaledBy);

                yield return null;
            }

            Plugin.Log("Finished hologram scale routine for " + Target.name);

            hologram.SetActive(false);

            hologramCoroutine = null;

            yield return null;
        }

        private void ResetItemProperties()
        {
            if (Target.itemProperties == originalItemProperties) return;

            Destroy(Target.itemProperties);
            Target.itemProperties = originalItemProperties;
        }

        private void OverrideItemProperties()
        {
            if (Target.itemProperties == originalItemProperties)
            {
                // itemProperties is not overriden, overrides it
                Target.itemProperties = Instantiate(originalItemProperties);
            }
        }

        private void RecalculateOffset(float scale)
        {
            Target.itemProperties.positionOffset = originalItemProperties.positionOffset * scale;
        }

        public void ScaleTemporarlyTo(float scale)
        {
            gameObject.transform.localScale = OriginalScale * scale;
        }

        public override void Reset()
        {
            gameObject.transform.localScale = Target.originalScale;
        }
#endregion
    }

    internal class EnemyScaling : TargetScaling<EnemyAI>
    {
        internal override Vector3 OriginalScale => Target.enemyType.enemyPrefab.transform.localScale;

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            HandleDifferentlyScaledEnemies(scale);

            base.ScaleTo(scale, scaledBy);

            AudioPatches.AdjustPitchIntensityOf(Target);
        }

        private void HandleDifferentlyScaledEnemies(float scale) // todo: move to their event handler
        {
            if (Target is DocileLocustBeesAI)
            {
                var particles = Target.gameObject.transform.Find("BugSwarmParticle");
                if (particles != null)
                    particles.transform.localScale = OriginalScale * scale;
            }
        }
    }
}
