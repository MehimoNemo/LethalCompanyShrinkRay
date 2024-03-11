using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class TargetGlassification : TargetHighlighting
    {
        internal override Material ChangedMaterial(Material origin)
        {
            return Materials.Glass;
        }
    }

    internal class TargetHighlighting : MonoBehaviour
    {
        internal List<Material> originalMaterials = null;

        void Awake()
        {
            if(gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            if (!gameObject.TryGetComponent(out MeshRenderer renderer)) return;

            originalMaterials = renderer.materials.ToList();
            ChangeMaterials();
        }

        internal virtual void ChangeMaterials()
        {
            if (!gameObject.TryGetComponent(out MeshRenderer renderer)) return;
            
            List<Material> targetedMaterials = new List<Material>();
            for (int i = 0; i < renderer.materials.Length; i++)
                targetedMaterials.Add(ChangedMaterial(renderer.materials[i]));
            renderer.materials = targetedMaterials.ToArray();
        }

        internal virtual Material ChangedMaterial(Material origin)
        {
            return Materials.TargetedMaterial(origin);
        }

        void OnDestroy()
        {
            if (gameObject.TryGetComponent(out MeshRenderer renderer))
                renderer.materials = originalMaterials.ToArray();
        }
    }
}
