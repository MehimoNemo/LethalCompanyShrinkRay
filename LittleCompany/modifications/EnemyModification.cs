using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.events.enemy;
using GameNetcodeStuff;

namespace LittleCompany.modifications
{
    public class EnemyModification : Modification
    {
        #region Methods
        internal static EnemyScaling ScalingOf(EnemyAI target)
        {
            if (!target.TryGetComponent(out EnemyScaling scaling))
                scaling = target.gameObject.AddComponent<EnemyScaling>();
            return scaling;
        }

        public static float NextShrunkenSizeOf(EnemyAI targetEnemy)
        {
            return Mathf.Max(ScalingOf(targetEnemy).RelativeScale - ModConfig.Instance.values.sizeChangeStep, 0f);
        }

        public static float NextIncreasedSizeOf(EnemyAI targetEnemy)
        {
            return Mathf.Min(ScalingOf(targetEnemy).RelativeScale + ModConfig.Instance.values.sizeChangeStep, 4f);
        }

        public static bool CanApplyModificationTo(EnemyAI targetEnemy, ModificationType type, PlayerControllerB playerModifiedBy)
        {
            if (targetEnemy == null)
                return false;

            var scaling = ScalingOf(targetEnemy);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetEnemy);
                    Plugin.Log("CanApplyModificationTo -> " + nextShrunkenSize + " / " + scaling.RelativeScale);
                    if (nextShrunkenSize == scaling.RelativeScale)
                        return false;
                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetEnemy);
                    if (nextIncreasedSize == scaling.RelativeScale)
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(EnemyAI targetEnemy, ModificationType type, PlayerControllerB playerModifiedBy, Action onComplete = null)
        {
            if (targetEnemy?.gameObject == null) return;

            var scaling = ScalingOf(targetEnemy);
            if (scaling == null) return;

            switch (type)
            {
                case ModificationType.Shrinking:
                    {
                        var previousScale = ScalingOf(targetEnemy).RelativeScale;
                        var nextShrunkenSize = NextShrunkenSizeOf(targetEnemy);
                        Plugin.Log("Shrinking enemy [" + targetEnemy.name + "] to size: " + nextShrunkenSize);
                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                                EnemyEventManager.EventHandlerOf(targetEnemy)?.OnDeathShrinking(previousScale, playerModifiedBy);

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetEnemy);
                        Plugin.Log("Enlarging enemy [" + targetEnemy.name + "] to size: " + nextIncreasedSize);
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
