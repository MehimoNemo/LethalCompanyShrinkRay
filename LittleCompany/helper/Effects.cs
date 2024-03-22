using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LittleCompany.helper
{
    internal class Effects
    {
        private static GameObject _circleHighlightPrefab = null;
        public static GameObject CircleHighlight
        {
            get
            {
                if (_circleHighlightPrefab == null)
                {
                    _circleHighlightPrefab = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "Shrink/HighlightingCircle.prefab"));
                    if (_circleHighlightPrefab == null) return null;
                }

                var ch = UnityEngine.Object.Instantiate(_circleHighlightPrefab);
                return ch;
            }
        }

        private static GameObject _deathPoof = null;
        public static GameObject DeathPoof
        {
            get
            {
                if (_deathPoof == null)
                {
                    _deathPoof = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "grabbable/Poof.prefab"));
                    if (_deathPoof == null) return null;
                }

                var ch = UnityEngine.Object.Instantiate(_deathPoof);
                return ch;
            }
        }
    }
}
