﻿using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.Config;
using System;
using UnityEngine;

using static LittleCompany.components.TargetScaling<VehicleController>;

namespace LittleCompany.modifications
{
    public class VehicleModification : Modification
    {
        #region Methods
        internal static VehicleScaling ScalingOf(VehicleController target)
        {
            if (!target.TryGetComponent(out VehicleScaling scaling))
                scaling = target.gameObject.AddComponent<VehicleScaling>();
            return scaling;
        }

        public static float SizeChangeStep(float multiplier = 1f) => Mathf.Max(ModConfig.Instance.values.vehicleSizeChangeStep * multiplier, ModConfig.SmallestSizeChange);

        public static float NextShrunkenSizeOf(VehicleController targetObject, float multiplier = 1f) => Mathf.Max(Rounded(ScalingOf(targetObject).RelativeScale - SizeChangeStep(multiplier)), 0f);

        public static float NextIncreasedSizeOf(VehicleController targetObject, float multiplier = 1f) => Rounded(ScalingOf(targetObject).RelativeScale + SizeChangeStep(multiplier));

        public static bool CanApplyModificationTo(VehicleController targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f)
        {
            if (targetObject == null)
                return false;

            var scaling = ScalingOf(targetObject);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Normalizing:
                    if (scaling.Unchanged)
                        return false;
                    break;

                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetObject, multiplier);
                    if (nextShrunkenSize == scaling.RelativeScale)
                        return false;

                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetObject, multiplier);
                    if (nextIncreasedSize == scaling.RelativeScale)
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(VehicleController targetObject, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f, Action onComplete = null)
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

                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
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
