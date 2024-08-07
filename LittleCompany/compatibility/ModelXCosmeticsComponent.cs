using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using ModelReplacement.Monobehaviors;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LittleCompany.compatibility
{
    [DisallowMultipleComponent]
    internal class ModelXCosmeticsComponent : MonoBehaviour, IScalingListener
    {
        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = ModelReplacementApiCompatibilityComponent.compatEnabled && MoreCompanyAudioCompatibilityPatch.compatEnabled;
                }
                return (bool)_enabled;
            }
        }

        internal PlayerControllerB player;
        internal ModelReplacementApiCompatibilityComponent compatComponent;
        internal List<Transform> cosmeticTransforms;
        internal List<Vector3> cosmeticOriginalScales;

        public void Awake()
        {
            player = GetComponent<PlayerControllerB>();
            GetComponent<PlayerScaling>()?.AddListener(this);
        }

        public void OnDestroy()
        {
            GetComponent<PlayerScaling>()?.RemoveListener(this);
        }

        public void AdjustToSize(float size)
        {
            if (!isModelReplaced()) return;

            AttemptLoadMoreCompanyCosmeticsTransforms();
            for (int i = 0; i < cosmeticTransforms.Count; i++)
            {
                cosmeticTransforms[i].localScale = cosmeticOriginalScales[i] * size;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void AttemptLoadMoreCompanyCosmeticsTransforms()
        {
            if(cosmeticTransforms == null || cosmeticTransforms.Count == 0)
            {
                cosmeticTransforms = [];
                cosmeticOriginalScales = [];
                MoreCompanyCosmeticManager more = player.GetComponent<MoreCompanyCosmeticManager>();
                float currentPlayerSize = player.GetComponent<PlayerScaling>().RelativeScale;
                foreach (MoreCompanyCosmeticManager.CosmeticInstance2 cosmetic in more.cosmeticInstances)
                {
                    cosmeticTransforms.Add(cosmetic.cosmetic.transform);
                    cosmeticOriginalScales.Add(cosmetic.cosmetic.transform.localScale / currentPlayerSize);
                }
            }
        }

        public bool isModelReplaced()
        {
            if (compatComponent == null)
            {
                compatComponent = player.GetComponent<ModelReplacementApiCompatibilityComponent>();
            }
            return compatComponent != null && compatComponent.replacementModel != null;
        }

        public void AfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            AdjustToSize(to);
        }

        public void AtEndOfScaling(float from, float to, PlayerControllerB playerB)
        {
            AdjustToSize(to);
        }
    }
}
