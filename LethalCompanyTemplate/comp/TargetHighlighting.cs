using LCShrinkRay.helper;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class TargetHighlighting : MonoBehaviour
    {
        internal GameObject highlighter = null;
        internal List<Material> originalMaterials = null;
        void Awake()
        {
            if(gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }


            /*highlighter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlighter.transform.localScale = gameObject.transform.localScale + Vector3.one * 0.3f;
            if (highlighter.TryGetComponent(out BoxCollider boxCollider))
                boxCollider.enabled = false;

            if (highlighter.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.material = new Material(Shader.Find("HDRP/Lit"));
                meshRenderer.material.color = new UnityEngine.Color(1f, 0.3f, 0.3f, 0.8f);
                meshRenderer.enabled = true;
            }*/

            if (!gameObject.TryGetComponent(out MeshRenderer renderer))
            {
                Plugin.Log("TargetHighlighting -> no meshrenderer found!", Plugin.LogType.Error);
                return;
            }

            Plugin.Log("Adding highlighter to " + gameObject.name);

            originalMaterials = renderer.materials.ToList();
            List<Material> targetedMaterials = new List<Material>();
            for (int i = 0; i < renderer.materials.Length; i++)
                targetedMaterials.Add(Materials.TargetedMaterial(renderer.materials[i]));
            renderer.materials = targetedMaterials.ToArray();
        }

        void Update()
        {
            if (highlighter == null) return;

            /*highlighter.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            highlighter.transform.SetLocalPositionAndRotation(gameObject.transform.localPosition, gameObject.transform.localRotation);*/
        }

        void OnDestroy()
        {
            if (gameObject.TryGetComponent(out MeshRenderer renderer))
                renderer.materials = originalMaterials.ToArray();

            //Destroy(highlighter);
        }
    }
}
