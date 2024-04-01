using LittleCompany.components;
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
                foreach(var enemyScaling in Object.FindObjectsOfType<EnemyScaling>())
                {
                    if(enemyScaling.target is HoarderBugAI)
                        scale = Mathf.Max(scale, enemyScaling.CurrentScale);
                }
                return scale;
            }
        }

        public static float SizeOf(EnemyAI enemyAI) => enemyAI.TryGetComponent(out EnemyScaling scaling) ? scaling.CurrentScale : 1f;

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
