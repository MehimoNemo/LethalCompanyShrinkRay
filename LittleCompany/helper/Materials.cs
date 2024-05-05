using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleCompany.helper
{
    internal class Materials
    {
        // Wireframe
        private static Material wireframeMaterial = null;
        public static Material Wireframe
        {
            get
            {
                if (wireframeMaterial == null)
                {
                    Shader wireframeShader = AssetLoader.littleCompanyAsset?.LoadAsset<Shader>(Path.Combine(AssetLoader.BaseAssetPath, "Shader/wireframe.shader"));
                    if(wireframeShader == null)
                    {
                        Plugin.Log("Unable to load wireframe shader!", Plugin.LogType.Error);
                        return null;
                    }

                    // Create a material from the loaded shader
                    wireframeMaterial = new Material(wireframeShader);
                    wireframeMaterial.SetColor("Edge color", new Color(0f, .969f, .969f, .5f));
                    wireframeMaterial.SetColor("Main Color", new Color(.3f, .3f, .3f, .3f));
                    wireframeMaterial.SetFloat("Edge width", 0.005f);
                }
                return wireframeMaterial;
            }
        }

        public static Material TargetedWireframe(Material origin)
        {
            var m = Wireframe;
            m.SetColor("Main Color", origin.color);
            m.SetFloat("Edge width", 0.01f);
            return m;
        }

        // Glass
        private static Material glassMaterial = null;
        public static Material Glass
        {
            get
            {
                if (glassMaterial == null)
                {
                    glassMaterial = new Material(Shader.Find("HDRP/Lit"));
                    if (glassMaterial == null) return null;

                    glassMaterial.color = new Color(0.5f, 0.5f, 0.6f, 0.6f);
                    glassMaterial.renderQueue = 3300;
                    glassMaterial.shaderKeywords = [
                        "_SURFACE_TYPE_TRANSPARENT",
                        "_DISABLE_SSR_TRANSPARENT",
                        "_REFRACTION_THIN",
                        "_NORMALMAP_TANGENT_SPACE",
                        "_ENABLE_FOG_ON_TRANSPARENT"
                    ];
                    glassMaterial.name = "LCGlass";
                }
                return glassMaterial;
            }
        }

        private static Material laserMaterial = null;
        public static Material Laser
        {
            get
            {
                if (laserMaterial == null)
                    laserMaterial = AssetLoader.littleCompanyAsset?.LoadAsset<Material>(Path.Combine(AssetLoader.BaseAssetPath, "Shrink/materials/Laser.mat"));

                return laserMaterial;
            }
        }

        public static Material TargetedMaterial(Material origin)
        {
            var m = new Material(origin);
            m.color = Color.red;
            m.SetColor("_EmissionColor", new Color(1f, 0f, 0f, 2f));
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_Glossiness", 1f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }

        private static readonly Color _burntColor = new Color(0.4f, 0.2f, 0.2f);
        public static Material BurntMaterial
        {
            get
            {
                var m = new Material(Shader.Find("HDRP/Lit"));
                m.color = _burntColor;
                return m;
            }
        }

        public static List<MeshRenderer> GetMeshRenderers(GameObject g)
        {
            List<MeshRenderer> listOfMesh = [];
            foreach (MeshRenderer mesh in g.GetComponentsInChildren<MeshRenderer>())
            {
                listOfMesh.Add(mesh);
            }
            return listOfMesh;
        }

        public static void ReplaceAllMaterialsWith(GameObject g, Func<Material, Material> materialReplacer)
        {
            var meshRenderer = GetMeshRenderers(g);
            foreach (var mr in meshRenderer)
                ReplaceAllMaterialsWith(mr, materialReplacer);
        }

        public static void ReplaceAllMaterialsWith(MeshRenderer mr, Func<Material, Material> materialReplacer)
        {
            var materials = new List<Material>();
            foreach (var m in mr.materials)
                materials.Add(materialReplacer(m));
            mr.materials = materials.ToArray();
        }
    }
}
