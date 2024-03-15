using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class TargetForceField : TargetHighlighting
    {
        void Awake()
        {
            HighlightUsing(HighlightMethod.ForceField);
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
            ForceField
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
                case HighlightMethod.ForceField:
                    highlightingGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //highlightingGameObject.transform.SetParent(gameObject.transform, false);

                    if (highlightingGameObject.TryGetComponent(out SphereCollider sphereCollider))
                        sphereCollider.enabled = false;

                    if (highlightingGameObject.TryGetComponent(out MeshRenderer sphereRenderer))
                    {
                        sphereRenderer.material = Materials.Glass;
                        sphereRenderer.material.color = new UnityEngine.Color(1f, 0f, 0f, 0.3f); // transparent red
                        sphereRenderer.enabled = true;
                    }

                    if (renderer != null)
                    {
                        highlightingGameObject.transform.localScale = renderer.bounds.size;
                        highlightingGameObject.transform.position = renderer.bounds.center;
                    }
                    else if(gameObject.TryGetComponent(out Collider collider))
                    {
                        highlightingGameObject.transform.localScale = collider.bounds.size;
                        highlightingGameObject.transform.position = collider.bounds.center;
                    }
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
