using System.IO;
using System.Reflection;
using UnityEngine;

namespace LCShrinkRay.helper
{
    internal class Materials
    {
        // Wireframe
        /*private static Material wireframeMaterial = null;
        public static Material Wireframe
        {
            get
            {
                if (wireframeMaterial == null)
                {
                    Shader wireframeShader = AssetLoader.littleCompanyAsset?.LoadAsset<Shader>(Path.Combine(AssetLoader.BaseAssetPath, "Shader/wireframe.shader");
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
        }*/

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
    }
}
