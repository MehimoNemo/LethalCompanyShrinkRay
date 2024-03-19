using GameNetcodeStuff;
using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.Netcode;
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


            this.method = method;
            switch(method)
            {
                case HighlightMethod.Material:
                    var renderer = gameObject.GetComponent<MeshRenderer>();
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

                    SetScaleAndPosition();

                    highlightingGameObject.transform.SetParent(gameObject.transform, true);
                    break;
                default: break;
            }
        }

        internal bool CircleWouldBeInvisible(Vector3 size) => size.x <= 0f || size.y <= 0f || size.z <= 0f;

        internal void SetScaleAndPosition()
        {
            if (gameObject.TryGetComponent(out MeshRenderer renderer) && !CircleWouldBeInvisible(renderer.bounds.size))
                SetScaleAndPosition(renderer.bounds.size, renderer.bounds.center, renderer.bounds.size.y / 2);
            else if (gameObject.TryGetComponent(out BoxCollider boxCollider) && !CircleWouldBeInvisible(boxCollider.bounds.size))
                SetScaleAndPosition(boxCollider.bounds.size, boxCollider.bounds.center, boxCollider.bounds.size.y / 2);
            else if (gameObject.TryGetComponent(out SphereCollider sphereCollider) && !CircleWouldBeInvisible(sphereCollider.bounds.size))
                SetScaleAndPosition(sphereCollider.bounds.size, sphereCollider.bounds.center, sphereCollider.bounds.size.y / 2);
            else if (gameObject.TryGetComponent(out Collider collider) && !CircleWouldBeInvisible(collider.bounds.size))
                SetScaleAndPosition(collider.bounds.size, collider.bounds.center, collider.bounds.size.y / 2);
            else
                SetScaleAndPosition(gameObject.transform.localScale, gameObject.transform.position);
        }
        internal void SetScaleAndPosition(Vector3 scale, Vector3 position, float groundPositionOffset = 0f)
        {
            position.y -= groundPositionOffset;
            var maxRadius = Mathf.Max(scale.x, scale.z);
            if(maxRadius < 0.8f)
                highlightingGameObject.transform.localScale = new Vector3(maxRadius, Mathf.Clamp(scale.y, 0.2f, 0.8f), maxRadius);
            else
                highlightingGameObject.transform.localScale = new Vector3(scale.x, Mathf.Clamp(scale.y, 0.2f, 0.8f), scale.z);
            highlightingGameObject.transform.position = new Vector3(position.x, position.y, position.z);
            Plugin.Log("Position: " + highlightingGameObject.transform.position + " / Scale: " + highlightingGameObject.transform.localScale);
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
