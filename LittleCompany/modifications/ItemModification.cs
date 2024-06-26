﻿using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

using static LittleCompany.components.TargetScaling<GrabbableObject>;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.modifications
{
    public class ItemModification : Modification
    {
        public static List<string> UnscalableItems = ["RagdollGrabbableObject"];

        #region Methods
        internal static ItemScaling ScalingOf(GrabbableObject target)
        {
            if (!target.TryGetComponent(out ItemScaling scaling))
                scaling = target.gameObject.AddComponent<ItemScaling>();
            return scaling;
        }

        public static float SizeChangeStep(float multiplier = 1f) => Mathf.Max(ModConfig.Instance.values.itemSizeChangeStep * multiplier, ModConfig.SmallestSizeChange);

        public static float NextShrunkenSizeOf(GrabbableObject targetObject, float multiplier = 1f) => Mathf.Max(Rounded(ScalingOf(targetObject).DesiredScale - SizeChangeStep(multiplier)), 0f);

        public static float NextIncreasedSizeOf(GrabbableObject targetObject, float multiplier = 1f) => Rounded(ScalingOf(targetObject).DesiredScale + SizeChangeStep(multiplier));

        public static bool CanApplyModificationTo(GrabbableObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f)
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
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject, multiplier);
                    if (nextShrunkenSize == scaling.DesiredScale)
                        return false;

                    if (UnscalableItems.Contains(targetObject.itemProperties.itemName))
                        return false;

                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject, multiplier);
                    if (nextIncreasedSize == scaling.DesiredScale)
                        return false;

                    if (UnscalableItems.Contains(targetObject.itemProperties.itemName))
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(GrabbableObject targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f, Action onComplete = null)
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
                        var nextShrunkenSize = NextShrunkenSizeOf(targetObject, multiplier);
                        Plugin.Log("Shrinking object [" + targetObject.name + "] to size: " + nextShrunkenSize);
                        if (Mathf.Approximately(nextShrunkenSize, 0f) && TryGetEventHandlerOf(targetObject, out ItemEventHandler handler))
                            handler.AboutToDeathShrink(previousSize, playerModifiedBy);

                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (Mathf.Approximately(nextShrunkenSize, 0f) && TryGetEventHandlerOf(targetObject, out ItemEventHandler handler))
                                handler.OnDeathShrinking(previousSize, playerModifiedBy);

                            if (onComplete != null)
                                onComplete();
                        }, default, Mode.Linear);

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetObject, multiplier);
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
