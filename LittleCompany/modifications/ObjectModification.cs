using LittleCompany.components;
using LittleCompany.Config;
using System;
using UnityEngine;

namespace LittleCompany.modifications
{
    public class ObjectModification : Modification
    {
        #region Methods
        internal static ItemScaling ScalingOf(GrabbableObject target)
        {
            if (!target.TryGetComponent(out ItemScaling scaling))
                scaling = target.gameObject.AddComponent<ItemScaling>();
            return scaling;
        }

        public static float NextShrunkenSizeOf(GrabbableObject targetObject)
        {
            return Mathf.Max(ScalingOf(targetObject).CurrentScale - ModConfig.Instance.values.sizeChangeStep, 0f);
        }

        public static float NextIncreasedSizeOf(GrabbableObject targetObject)
        {
            return Mathf.Min(ScalingOf(targetObject).CurrentScale + ModConfig.Instance.values.sizeChangeStep, 4f);
        }

        public static bool CanApplyModificationTo(GrabbableObject targetObject, ModificationType type)
        {
            if (targetObject == null)
                return false;

            var scaling = ScalingOf(targetObject);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (scaling.CurrentScale == 1f)
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                    Plugin.Log("CanApplyModificationTo -> " + nextShrunkenSize + " / " + scaling.CurrentScale);
                    if (nextShrunkenSize == scaling.CurrentScale)
                        return false;
                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject);
                    if (nextIncreasedSize == scaling.CurrentScale)
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(GrabbableObject targetObject, ModificationType type, Action onComplete = null)
        {
            if (targetObject?.gameObject == null) return;

            var scaling = ScalingOf(targetObject);
            if (scaling == null) return;

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        var normalizedSize = 1f;
                        Plugin.Log("Normalizing object [" + targetObject.name + "]");
                        scaling.ScaleOverTimeTo(normalizedSize, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        }, true);
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                        Plugin.Log("Shrinking object [" + targetObject.name + "] to size: " + nextShrunkenSize);
                        scaling.ScaleOverTimeTo(nextShrunkenSize, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                // Poof Target to death because they are too small to exist
                                if (ShrinkRayFX.TryCreateDeathPoofAt(out GameObject deathPoof, targetObject.transform.position) && targetObject.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null)
                                    audioSource.PlayOneShot(deathPoofSFX);

                                targetObject.DestroyObjectInHand(targetObject.playerHeldBy);
                            }

                            if (onComplete != null)
                                onComplete();
                        }, true);

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetObject);
                        Plugin.Log("Enlarging object [" + targetObject.name + "] to size: " + nextIncreasedSize);
                        scaling.ScaleOverTimeTo(nextIncreasedSize, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        }, true);

                        break;
                    }
                default:
                    return;
            }

            return;
        }
        #endregion
    }
}
