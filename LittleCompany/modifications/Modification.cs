using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LittleCompany.modifications
{
    public abstract class Modification
    {
        #region Properties
        internal static readonly float DeathShrinkMargin = 0.2f;
        public enum ModificationType
        {
            Normalizing,
            Shrinking,
            Enlarging
        }

        internal static AudioClip deathPoofSFX;
        #endregion

        public static float Rounded(float unroundedValue) => Mathf.Round(unroundedValue * 100f) / 100f; // round to 2 digits

        /*#region Methods
        public static float NextShrunkenSizeOf(GameObject target) { throw new NotImplementedException(); }

        public static float NextIncreasedSizeOf(GameObject target) { throw new NotImplementedException(); }

        public static bool CanApplyModificationTo(GameObject target, ModificationType type) { throw new NotImplementedException(); }

        public static void ApplyModificationTo(GameObject target, ModificationType type, Action onComplete = null) { throw new NotImplementedException(); }
        #endregion*/
    }
}
