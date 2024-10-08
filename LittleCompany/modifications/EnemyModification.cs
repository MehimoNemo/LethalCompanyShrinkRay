﻿using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.Config;
using LittleCompany.events.enemy;
using GameNetcodeStuff;

namespace LittleCompany.modifications
{
    public class EnemyModification : Modification
    {
        public static bool callDeathShrinkEvent = true;
        #region Methods
        internal static EnemyScaling ScalingOf(EnemyAI target)
        {
            if (!target.TryGetComponent(out EnemyScaling scaling))
                scaling = target.gameObject.AddComponent<EnemyScaling>();
            return scaling;
        }
        public static float SizeChangeStep(float multiplier = 1f) => Mathf.Max(ModConfig.Instance.values.enemySizeChangeStep * multiplier, ModConfig.SmallestSizeChange);

        public static float NextShrunkenSizeOf(EnemyAI targetEnemy, float multiplier = 1f) => Mathf.Max(Rounded(ScalingOf(targetEnemy).RelativeScale - SizeChangeStep(multiplier)), 0f);

        public static float NextIncreasedSizeOf(EnemyAI targetEnemy, float multiplier = 1f) => Rounded(ScalingOf(targetEnemy).RelativeScale + SizeChangeStep(multiplier));

        public static bool CanApplyModificationTo(EnemyAI targetEnemy, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f)
        {
            if (targetEnemy == null)
                return false;

            var scaling = ScalingOf(targetEnemy);
            if (scaling == null)
                return false;

            switch (type)
            {
                case ModificationType.Shrinking:
                    var nextShrunkenSize = NextShrunkenSizeOf(targetEnemy, multiplier);
                    Plugin.Log("CanApplyModificationTo -> " + nextShrunkenSize + " / " + scaling.RelativeScale);
                    if (nextShrunkenSize == scaling.RelativeScale)
                        return false;
                    break;

                case ModificationType.Enlarging:
                    var nextIncreasedSize = NextIncreasedSizeOf(targetEnemy, multiplier);
                    if (nextIncreasedSize == scaling.RelativeScale)
                        return false;
                    break;

                default:
                    return false; // Not supported yet
            }

            return true;
        }

        public static void ApplyModificationTo(EnemyAI targetEnemy, ModificationType type, PlayerControllerB playerModifiedBy, float multiplier = 1f, Action onComplete = null)
        {
            if (targetEnemy?.gameObject == null) return;

            var scaling = ScalingOf(targetEnemy);
            if (scaling == null) return;

            switch (type)
            {
                case ModificationType.Shrinking:
                    {
                        var previousScale = ScalingOf(targetEnemy).RelativeScale;
                        var nextShrunkenSize = NextShrunkenSizeOf(targetEnemy, multiplier);
                        Plugin.Log("Shrinking enemy [" + targetEnemy.name + "] to size: " + nextShrunkenSize);
                        if (nextShrunkenSize < DeathShrinkMargin && callDeathShrinkEvent)
                            EnemyEventManager.EventHandlerOf(targetEnemy)?.AboutToDeathShrink(previousScale, playerModifiedBy);

                        scaling.ScaleOverTimeTo(nextShrunkenSize, playerModifiedBy, () =>
                        {
                            if (nextShrunkenSize < DeathShrinkMargin)
                            {
                                Plugin.Log("Death Shrinking: " + nextShrunkenSize + " : " + DeathShrinkMargin);
                                var enemyEventHandler = EnemyEventManager.EventHandlerOf(targetEnemy);
                                if(enemyEventHandler != null)
                                {
                                    if (callDeathShrinkEvent)
                                        enemyEventHandler.OnDeathShrinking(previousScale, playerModifiedBy);
                                    else
                                        enemyEventHandler.DespawnEnemy();
                                }
                            }

                            if (onComplete != null)
                                onComplete();
                        });

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var nextIncreasedSize = NextIncreasedSizeOf(targetEnemy, multiplier);
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
