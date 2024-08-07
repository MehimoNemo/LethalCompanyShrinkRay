using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using ModelReplacement.Monobehaviors.Enemies;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LittleCompany.compatibility
{
    [DisallowMultipleComponent]
    internal class ModelReplacementApiMaskedCompatibilityComponent : MonoBehaviour, IScalingListener
    {
        public const string ModelReplacementApiReferenceChain = "meow.ModelReplacementAPI";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModelReplacementApiReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        internal MaskedPlayerEnemy masked;
        internal GameObject replacementModel;
        public Vector3 replacementModelOriginalScale;

        public void Awake()
        {
            masked = GetComponent<MaskedPlayerEnemy>();
            AttachAndRescaleReplacementModel();
            GetComponent<EnemyScaling>()?.AddListener(this);
        }

        public void OnDestroy()
        {
            GetComponent<EnemyScaling>()?.RemoveListener(this);
        }

        public void AttachAndRescaleReplacementModel()
        {
            GameObject foundModel = FindCurrentReplacementModel();
            if (foundModel != null)
            {
                if (replacementModel == null || foundModel.GetInstanceID() != replacementModel.GetInstanceID())
                {
                    Plugin.Log("Replace original scale");
                    replacementModelOriginalScale = foundModel.transform.localScale;
                }
                replacementModel = foundModel;
                Plugin.Log("Replacement: " + replacementModel.name + " : " + replacementModel.GetInstanceID());
                Plugin.Log("Size: " + EnemyInfo.SizeOf(masked));
                AdjustToSize(EnemyInfo.SizeOf(masked));
            }
            else
            {
                Plugin.Log("Replacement null");
                replacementModel = null;
            }
        }

        public void AdjustToSize(float size)
        {
            if (replacementModel != null && replacementModel.transform != null)
            {
                replacementModel.transform.localScale = replacementModelOriginalScale * size;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private GameObject FindCurrentReplacementModel()
        {
            return masked?.GetComponent<MaskedReplacementBase>()?.replacementModel;
        }

        public void AfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            AdjustToSize(to);
        }

        public void AtEndOfScaling(float from, float to, PlayerControllerB playerB)
        {
            AttachAndRescaleReplacementModel();
        }
    }
}
