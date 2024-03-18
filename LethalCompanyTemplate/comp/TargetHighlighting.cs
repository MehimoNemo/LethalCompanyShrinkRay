using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace LCShrinkRay.comp
{
    internal class TargetCircle : TargetHighlighting
    {
        void Awake()
        {
            HighlightUsing(HighlightMethod.Circle);
        }
    }

    internal class TargetGlassification : TargetHighlighting
    {
        internal override Material ChangedMaterial(Material origin)
        {
            return Materials.Glass;
        }

        void Awake()
        {
            HighlightUsing(HighlightMethod.Material);
        }
    }

    internal class TargetHighlighting : MonoBehaviour
    {
        public enum HighlightMethod
        {
            None,
            Material,
            Circle
        }
        internal HighlightMethod method;
        internal List<Material> originalMaterials = null;
        internal GameObject highlightingGameObject = null;

        public void HighlightUsing(HighlightMethod method = HighlightMethod.Material)
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            if (this.method != HighlightMethod.None)
                Revert();

            var renderer = gameObject.GetComponent<Renderer>();

            this.method = method;
            switch(method)
            {
                case HighlightMethod.Material:
                    if (renderer == null) return;
                    originalMaterials = renderer.materials.ToList();
                    ChangeMaterials();
                    break;
                case HighlightMethod.Circle:
                    highlightingGameObject = Materials.CircleHighlight;
                    if (highlightingGameObject == null)
                    {
                        Plugin.Log("Unable to load highlight.", Plugin.LogType.Error);
                        return;
                    }

                    if (renderer != null)
                    {
                        highlightingGameObject.transform.localScale = new Vector3(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, 1f), renderer.bounds.size.z);
                        highlightingGameObject.transform.position = new Vector3(renderer.bounds.center.x, 0f, renderer.bounds.center.z);
                    }
                    else if(gameObject.TryGetComponent(out Collider collider))
                    {
                        highlightingGameObject.transform.localScale = new Vector3(collider.bounds.size.x, Mathf.Max(collider.bounds.size.y, 1f), collider.bounds.size.z);
                        highlightingGameObject.transform.position = new Vector3(collider.bounds.center.x, 0f, collider.bounds.center.z);
                    }

                    highlightingGameObject.transform.SetParent(gameObject.transform, true);
                    break;
                default: break;
            }
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

        internal void Revert()
        {
            if (highlightingGameObject != null)
                Destroy(highlightingGameObject);

            switch (method)
            {
                case HighlightMethod.Material:
                    if (gameObject.TryGetComponent(out MeshRenderer renderer))
                        renderer.materials = originalMaterials.ToArray();
                    break;
                default: break;
            }
        }

        void OnDestroy()
        {
            Revert();
        }
    }
}
