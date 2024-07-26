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
        internal virtual Vector3 OriginalScale => Target.gameObject.transform.localScale;

        internal HashSet<IScalingListener> scalingListeners;

        public bool GettingScaled { get; private set; } = false;

        public float ScalingProgress { get; private set; } = 0f; // progress from 0 to 1

        public virtual Transform TransformToScale => gameObject.transform;

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

            RelativeScale = 1f / OriginalScale.y * TransformToScale.localScale.y;

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
            TransformToScale.localScale = OriginalScale * scale;
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

        public bool Unchanged => OriginalScale == TransformToScale.localScale;

        public virtual void Reset()
        {
            TransformToScale.localScale = OriginalScale;
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

    internal class VehicleScaling : TargetScaling<VehicleController>
    {
        internal override Vector3 OriginalScale => Vector3.one * 1.18f;
        float OriginalMass = 0f;

        float OriginalFrontRightWheelRadius = 0f;
        float OriginalFrontRightWheelSprungMass = 0f;
        float OriginalFrontRightWheelSuspensionDistance = 0f;

        float OriginalFrontLeftWheelRadius = 0f;
        float OriginalFrontLeftWheelSprungMass = 0f;
        float OriginalFrontLeftWheelSuspensionDistance = 0f;

        float OriginalBackRightWheelRadius = 0f;
        float OriginalBackRightWheelSprungMass = 0f;
        float OriginalBackRightWheelSuspensionDistance = 0f;

        float OriginalBackLeftWheelRadius = 0f;
        float OriginalBackLeftWheelSprungMass = 0f;
        float OriginalBackLeftWheelSuspensionDistance = 0f;

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            base.ScaleTo(scale, scaledBy);
            UpdateWheelScaling(scale);
            Target.mainRigidbody.mass = OriginalMass * Mathf.Clamp(scale, 0.5f, 1f);
            Target.mainRigidbody.ResetCenterOfMass();
        }

        private void UpdateWheelScaling(float scale)
        {
            Target.FrontRightWheel.radius = OriginalFrontRightWheelRadius * scale;
            Target.FrontRightWheel.sprungMass = OriginalFrontRightWheelSprungMass / scale;
            Target.FrontRightWheel.suspensionDistance = OriginalFrontRightWheelSuspensionDistance * scale;

            Target.FrontLeftWheel.radius = OriginalFrontLeftWheelRadius * scale;
            Target.FrontLeftWheel.sprungMass = OriginalFrontLeftWheelSprungMass / scale;
            Target.FrontLeftWheel.suspensionDistance = OriginalFrontLeftWheelSuspensionDistance * scale;

            Target.BackRightWheel.radius = OriginalBackRightWheelRadius * scale;
            Target.BackRightWheel.sprungMass = OriginalBackRightWheelSprungMass / scale;
            Target.BackRightWheel.suspensionDistance = OriginalBackRightWheelSuspensionDistance * scale;

            Target.BackLeftWheel.radius = OriginalBackLeftWheelRadius * scale;
            Target.BackLeftWheel.sprungMass = OriginalBackLeftWheelSprungMass / scale;
            Target.BackLeftWheel.suspensionDistance = OriginalBackLeftWheelSuspensionDistance * scale;
        }


        internal override void OnAwake()
        {
            OriginalMass = Target.mainRigidbody.mass;

            OriginalFrontRightWheelRadius = Target.FrontRightWheel.radius;
            OriginalFrontRightWheelSprungMass = Target.FrontRightWheel.sprungMass;
            OriginalFrontRightWheelSuspensionDistance = Target.FrontRightWheel.suspensionDistance;

            OriginalFrontLeftWheelRadius = Target.FrontLeftWheel.radius;
            OriginalFrontLeftWheelSprungMass = Target.FrontLeftWheel.sprungMass;
            OriginalFrontLeftWheelSuspensionDistance = Target.FrontLeftWheel.suspensionDistance;

            OriginalBackRightWheelRadius = Target.BackRightWheel.radius;
            OriginalBackRightWheelSprungMass = Target.BackRightWheel.sprungMass;
            OriginalBackRightWheelSuspensionDistance = Target.BackRightWheel.suspensionDistance;

            OriginalBackLeftWheelRadius = Target.BackLeftWheel.radius;
            OriginalBackLeftWheelSprungMass = Target.BackLeftWheel.sprungMass;
            OriginalBackLeftWheelSuspensionDistance = Target.BackLeftWheel.suspensionDistance;
        }

        public override void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            if(RelativeScale > scale)
                PrepareForScaling();
            base.ScaleOverTimeTo(scale, scaledBy, onComplete, duration, mode, startingFromScale);
        }

        private static List<Collider> disabledColliders = new List<Collider>();

        public void PrepareForScaling()
        {
            Target.mainRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;

            foreach (Collider collider in Target.GetComponents<Collider>())
            {
                if (collider is not WheelCollider && collider.enabled)
                {
                    disabledColliders.Add(collider);
                    collider.enabled = false;
                }
            }
            foreach (Collider collider in Target.GetComponentsInChildren<Collider>())
            {
                if (collider is not WheelCollider && collider.enabled)
                {
                    disabledColliders.Add(collider);
                    collider.enabled = false;
                }
            }
        }

        public void UnPrepareForScaling()
        {
            Target.mainRigidbody.constraints = 0;
            foreach (Collider collider in disabledColliders)
            {
                collider.enabled = true;
            }
            disabledColliders.Clear();
        }
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

                if (ModConfig.Instance.values.logicalMultiplier)
                {
                    if (PlayerInfo.IsDefaultSize(Target))
                        PlayerMultiplierPatch.Reset();
                    else
                        PlayerMultiplierPatch.Modify();
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

        public void NextFrameScale(float scale, PlayerControllerB player, int numberOfFrame)
        {
            StartCoroutine(NextFrameScaleCall(scale, player, numberOfFrame));
        }

        IEnumerator NextFrameScaleCall(float scale, PlayerControllerB player, int numberOfFrame)
        {
            //returning 0 will make it wait 1 frame
            yield return numberOfFrame;
            ScaleTo(scale, player);
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
        }

        internal bool TryCreateHologram()
        {
            hologram = ItemInfo.visualCopyOf(originalItemProperties);
            if (hologram == null) return false;

            //hologram.transform.position = Target.transform.position;
            //hologram.transform.SetParent(Target.transform, true);
            Materials.ReplaceAllMaterialsWith(hologram, (Material _) => Materials.Wireframe);
            hologram.SetActive(false);

            return true;
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

            if (DesiredScale > RelativeScale)
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

            if (Target.itemProperties.weight < 0f)
            {
                Target.itemProperties.weight = 0;
                diff = -lastWeight;
            }
                

            if (Target.playerHeldBy != null)
                Target.playerHeldBy.carryWeight = Mathf.Clamp(Target.playerHeldBy.carryWeight + diff, 1f, 10f);

            // Scrap value
            if (originalScrapValue > 0)
                Target.SetScrapValue((int)(originalScrapValue * Mathf.Max(1f + (RelativeScale - 1f) * 0.1f, 2f))); // Maximum twice the value at 10x size
        }

        public override void ScaleOverTimeTo(float scale, PlayerControllerB scaledBy, Action onComplete = null, float? duration = null, Mode? mode = null, float? startingFromScale = null)
        {
            base.ScaleOverTimeTo(scale, scaledBy, onComplete, duration, mode, DesiredScale);
        }

        private IEnumerator HologramScaleCoroutine()
        {
            Plugin.Log("Starting hologram scale routine for " + Target.name);
            if (hologram == null && !TryCreateHologram()) yield break;

            hologram.SetActive(true);

            var isPocketed = false;
            while(RelativeScale < DesiredScale)
            {
                //Plugin.Log("Going from scale " + RelativeScale + " to desired scale " + DesiredScale);
                float previousScale = RelativeScale;

#if DEBUG
                RelativeScale += Time.deltaTime / 2.5f * ModConfig.Instance.values.itemSizeChangeSpeed;
#else
                RelativeScale += Time.deltaTime / (20 * RelativeScale * ModConfig.Instance.values.itemSizeChangeSpeed);
#endif
                var newScale = OriginalScale * RelativeScale;
                TransformToScale.localScale = newScale;
                Target.originalScale = newScale;

                UpdatePropertiesBasedOnScale();

                CallListenersAfterEachScale(previousScale, RelativeScale, playerLastScaledBy);

                if(isPocketed != Target.isPocketed)
                {
                    isPocketed = Target.isPocketed;
                    hologram.SetActive(!isPocketed);
                }

                yield return null;
            }

            Plugin.Log("Finished hologram scale routine for " + Target.name);

            hologram.SetActive(false);

            hologramCoroutine = null;

            yield return null;
        }

        internal void ResetItemProperties()
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
            TransformToScale.localScale = OriginalScale * scale;
        }

        public override void Reset()
        {
            TransformToScale.localScale = Target.originalScale;
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
    internal class ShipObjectScaling : TargetScaling<PlaceableShipObject>
    {
        public readonly float CounterPlacementHeightLimit = 1.5f;
        public override Transform TransformToScale => Target.parentObject.transform;
        private bool AllowPlacementOnCountersDefault = false;

        public float offsetPivotToBottom = 0f;

        private Vector3? _originalScale = null;
        internal override Vector3 OriginalScale
        {
            get
            {
                if(_originalScale == null)
                    _originalScale = TransformToScale.localScale;

                return _originalScale.Value;
            }
        }

        internal override void OnAwake()
        {
            base.OnAwake();

            if (Target.placeObjectCollider != null && Target.parentObject != null)
            {
                var bottomY = Target.placeObjectCollider.bounds.center.y - (Target.placeObjectCollider.bounds.size.y / 2);
                offsetPivotToBottom = Target.parentObject.transform.position.y - bottomY;
            }

            AllowPlacementOnCountersDefault = Target.AllowPlacementOnCounters;
            Plugin.Log("Allowed by default: " + AllowPlacementOnCountersDefault);
        }

        public override void ScaleTo(float scale, PlayerControllerB scaledBy)
        {
            if (Target.parentObject != null)
            {
                var diff = scale - RelativeScale;
                Target.parentObject.positionOffset += Vector3.up * offsetPivotToBottom * diff;
            }

            if(AllowPlacementOnCountersDefault) // allowed by default
            {
                Target.AllowPlacementOnCounters = RelativeScale <= 1f || Target.placeObjectCollider.bounds.size.y < CounterPlacementHeightLimit;
            }
            else // not allowed by default
            {
                Target.AllowPlacementOnCounters = !Target.AllowPlacementOnWalls && Target.placeObjectCollider.bounds.size.y < CounterPlacementHeightLimit;
            }

            base.ScaleTo(scale, scaledBy);
        }
    }
}
