using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.helper;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleCompany.modifications
{
    public class ObjectModification : Modification
    {
        public static List<string> UnscalableObjects = new List<string>();

        #region Methods
        internal static ItemScaling ScalingOf(GrabbableObject target)
        {
            if (!target.TryGetComponent(out ItemScaling scaling))
                scaling = target.gameObject.AddComponent<ItemScaling>();
            return scaling;
        }

        public static float NextShrunkenSizeOf(GrabbableObject targetObject)
        {
            return Mathf.Max(Rounded(ScalingOf(targetObject).RelativeScale - ModConfig.Instance.values.itemSizeChangeStep), 0f);
        }

        public static float NextIncreasedSizeOf(GrabbableObject targetObject)
        {
            return Rounded(ScalingOf(targetObject).RelativeScale + ModConfig.Instance.values.itemSizeChangeStep);
        }

        public static bool CanApplyModificationTo(GrabbableObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy)
        {
            if (targetObject == null)
                return false;

            var scaling = ScalingOf(targetObject);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (scaling.RelativeScale == 1f)
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                    if (nextShrunkenSize == scaling.RelativeScale)
                        return false;

                    if (UnscalableObjects.Contains(targetObject.itemProperties.itemName))
                        return false;

                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject);
                    if (nextIncreasedSize == scaling.RelativeScale)
                        return false;

                    if (UnscalableObjects.Contains(targetObject.itemProperties.itemName))
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(GrabbableObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy, Action onComplete = null)
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
                        scaling.ScaleOverTimeTo(normalizedSize, playerModifiedBy, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        });
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                        Plugin.Log("Shrinking object [" + targetObject.name + "] to size: " + nextShrunkenSize);
                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                // Poof Target to death because they are too small to exist
                                if (Effects.TryCreateDeathPoofAt(out GameObject deathPoof, targetObject.transform.position) && targetObject.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null)
                                    audioSource.PlayOneShot(deathPoofSFX);

                                targetObject.DestroyObjectInHand(targetObject.playerHeldBy);
                            }

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetObject);
                        Plugin.Log("Enlarging object [" + targetObject.name + "] to size: " + nextIncreasedSize);
                        scaling.ScaleOverTimeTo(nextIncreasedSize, playerModifiedBy, () =>
                        {
                            if (onComplete != null)
                                onComplete();
                        });

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
