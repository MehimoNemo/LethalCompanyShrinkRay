using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.events.item;
using LittleCompany.helper;
using System;
using System.Collections.Generic;
using UnityEngine;

using static LittleCompany.components.TargetScaling<GrabbableObject>;

namespace LittleCompany.modifications
{
    public class ObjectModification : Modification
    {
        public static List<string> UnscalableObjects = ["RagdollGrabbableObject"];

        #region Methods
        internal static ItemScaling ScalingOf(GrabbableObject target)
        {
            if (!target.TryGetComponent(out ItemScaling scaling))
                scaling = target.gameObject.AddComponent<ItemScaling>();
            return scaling;
        }

        public static float NextShrunkenSizeOf(GrabbableObject targetObject)
        {
            return Mathf.Max(Rounded(ScalingOf(targetObject).DesiredScale - ModConfig.Instance.values.itemSizeChangeStep), 0f);
        }

        public static float NextIncreasedSizeOf(GrabbableObject targetObject)
        {
            return Rounded(ScalingOf(targetObject).DesiredScale + ModConfig.Instance.values.itemSizeChangeStep);
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
                    if (scaling.DesiredScale == 1f)
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                    if (nextShrunkenSize == scaling.DesiredScale)
                        return false;

                    if (UnscalableObjects.Contains(targetObject.itemProperties.itemName))
                        return false;

                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject);
                    if (nextIncreasedSize == scaling.DesiredScale)
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
                        var previousSize = ScalingOf(targetObject).RelativeScale;
                        var nextShrunkenSize = NextShrunkenSizeOf(targetObject);
                        Plugin.Log("Shrinking object [" + targetObject.name + "] to size: " + nextShrunkenSize);
                        if (Mathf.Approximately(nextShrunkenSize, 0f))
                            ItemEventManager.EventHandlerOf(targetObject).AboutToDeathShrink(previousSize, playerModifiedBy);

                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (Mathf.Approximately(nextShrunkenSize, 0f))
                                ItemEventManager.EventHandlerOf(targetObject).OnDeathShrinking(previousSize, playerModifiedBy);

                            if (onComplete != null)
                                onComplete();
                        }, default, Mode.Linear);

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
                        }, default, Mode.Linear);

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
