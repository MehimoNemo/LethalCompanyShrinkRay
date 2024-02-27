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
                    string assetDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    AssetBundle shaderBundle = AssetBundle.LoadFromFile(Path.Combine(assetDir, "littlecompanyasset"));
                    if (shaderBundle != null)
                    {
                        Shader shader = shaderBundle.LoadAsset<Shader>("Assets/shrinkRay/Shader/wireframe.shader");

                        // Create a material from the loaded shader
                        wireframeMaterial = new Material(shader);
                        wireframeMaterial.SetColor("Edge color", new Color(0f, .969f, .969f, .5f));
                        wireframeMaterial.SetColor("Main Color", new Color(.3f, .3f, .3f, .3f));
                        wireframeMaterial.SetFloat("Edge width", 0.005f);
                    }
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
    }
}
