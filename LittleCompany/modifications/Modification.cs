using LittleCompany.Config;
using UnityEngine;

namespace LittleCompany.modifications
{
    public abstract class Modification
    {
        #region Properties
        internal static float DeathShrinkMargin => ModConfig.Instance.values.removeMinimumSizeLimit ? ModConfig.SmallestSizeChange : 0.2f;
        public enum ModificationType
        {
            Normalizing,
            Shrinking,
            Enlarging
        }

        internal static AudioClip deathPoofSFX;
        #endregion

        public static float Rounded(float unroundedValue) => Mathf.Round(unroundedValue * 100f) / 100f; // round to 2 digits
    }
}
