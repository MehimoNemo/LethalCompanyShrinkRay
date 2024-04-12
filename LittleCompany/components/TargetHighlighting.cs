using LethalLib.Modules;
using LittleCompany.helper;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleCompany.components
{
    internal class TargetCircle : TargetHighlighting
    {
        internal GameObject highlightingGameObject = null;

        void Awake()
        {
            if(HighLightingIsValid())
                HighlightCircle();
        }

        void HighlightCircle()
        {
            highlightingGameObject = Effects.CircleHighlight;
            if (highlightingGameObject == null)
            {
                Plugin.Log("Unable to load highlight.", Plugin.LogType.Error);
                return;
            }

            SetScaleAndPosition();

            highlightingGameObject.transform.SetParent(gameObject.transform, true);
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
            if (maxRadius < 0.8f)
                highlightingGameObject.transform.localScale = new Vector3(maxRadius, Mathf.Clamp(scale.y, 0.2f, 0.8f), maxRadius);
            else
                highlightingGameObject.transform.localScale = new Vector3(scale.x, Mathf.Clamp(scale.y, 0.2f, 0.8f), scale.z);
            highlightingGameObject.transform.position = new Vector3(position.x, position.y, position.z);
        }

        public override void Revert()
        {
            if (highlightingGameObject != null)
                Destroy(highlightingGameObject);
        }
    }

    internal class TargetGlassification : TargetMaterialHighlighting
    {
        internal override Material ChangedMaterial(Material origin)
        {
            return Materials.Glass;
        }
    }

    internal class TargetMaterialHighlighting : TargetHighlighting
    {
        internal Dictionary<MeshRenderer, Material[]> meshesAndOriginalMaterials = null;

        void Awake()
        {
            if (HighLightingIsValid())
                HighlightMaterial();
        }

        void HighlightMaterial()
        {
            LoadMeshRenderersAndOriginalMaterials();
            if (meshesAndOriginalMaterials.Count == 0) return;
            ChangeMaterials();
        }

        internal void LoadMeshRenderersAndOriginalMaterials()
        {
            meshesAndOriginalMaterials = [];
            foreach (MeshRenderer mesh in Materials.GetMeshRenderers(gameObject))
            {
                meshesAndOriginalMaterials.Add(mesh, mesh.materials);
            }
        }

        internal void ChangeMaterials()
        {
            foreach (MeshRenderer mesh in meshesAndOriginalMaterials.Keys)
            {
                ChangeMaterialForMeshRenderer(mesh);
            }
        }

        internal void ChangeMaterialForMeshRenderer(MeshRenderer renderer)
        {
            List<Material> targetedMaterials = [];
            for (int i = 0; i < renderer.materials.Length; i++)
                targetedMaterials.Add(ChangedMaterial(renderer.materials[i]));
            renderer.materials = targetedMaterials.ToArray();
        }

        internal virtual Material ChangedMaterial(Material origin)
        {
            return Materials.TargetedMaterial(origin);
        }

        public override void Revert()
        {
            foreach (KeyValuePair<MeshRenderer, Material[]> meshAndOriginalMaterial in meshesAndOriginalMaterials)
            {
                meshAndOriginalMaterial.Key.materials = meshAndOriginalMaterial.Value;
            }
        }
    }

    abstract internal class TargetHighlighting : MonoBehaviour
    {

        public bool HighLightingIsValid()
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return false;
            }
            return true;
        }

        public abstract void Revert();

        void OnDestroy()
        {
            Revert();
        }
    }
}
