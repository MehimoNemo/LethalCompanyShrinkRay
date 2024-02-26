using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.helper
{
    internal class Materials
    {
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
