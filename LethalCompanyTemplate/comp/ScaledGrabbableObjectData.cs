using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LCShrinkRay.comp.ScaledGrabbableObjectData;

namespace LCShrinkRay.comp
{
    internal class ScaledGrabbableObjectData : MonoBehaviour
    {
        #region Properties
        public struct RendererDefaults
        {
            public Material[] materials { get; set; }
            public int priority { get; set; }
        }

        public struct InitialValues
        {
            public Vector3 scale { get; set; }
            public Vector3 offset { get; set; }
            public Dictionary<int, RendererDefaults> rendererDefaults { get; set; }
        }

        public InitialValues initialValues { get; private set; }
        public bool IsGlassified { get; set; }
        #endregion

        private void Awake()
        {
            var item = GetComponentInParent<GrabbableObject>();
            if (item == null)
            {
                Plugin.log("GrabbableObjectInitialValues not added to GrabbableObject");
                return;
            }

            initialValues = new InitialValues()
            {
                scale = item.transform.localScale,
                offset = item.itemProperties.positionOffset,
                rendererDefaults = new Dictionary<int, RendererDefaults>()
            };

            var meshRenderer = item.gameObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                foreach (var r in meshRenderer)
                    initialValues.rendererDefaults[r.GetInstanceID()] = new RendererDefaults() { priority = r.rendererPriority, materials = r.sharedMaterials };
            }
        }
    }
}
