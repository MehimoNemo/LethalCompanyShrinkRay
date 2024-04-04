using LittleCompany.components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleCompany.helper
{
    internal class EnemyInfo
    {
        public static float LargestGrabbingEnemy
        {
            get
            {
                var scale = 1f;
                foreach(var enemyScaling in UnityEngine.Object.FindObjectsOfType<EnemyScaling>())
                {
                    if(enemyScaling.target is HoarderBugAI)
                        scale = Mathf.Max(scale, enemyScaling.RelativeScale);
                }
                return scale;
            }
        }

        public static List<String> AllEnemyNames
        {
            get
            {
                var list = new List<string>();
                foreach (var enemyType in UnityEngine.Object.FindObjectsOfType<EnemyType>())
                    list.Add(enemyType.enemyName);
                return list;
            }
        }

        public static float SizeOf(EnemyAI enemyAI) => enemyAI.TryGetComponent(out EnemyScaling scaling) ? scaling.RelativeScale : 1f;

        internal class HoarderBug
        {
            internal enum BehaviourState
            {
                Searching = 0,
                Nest,
                Chase
            }
        }
    }
}
