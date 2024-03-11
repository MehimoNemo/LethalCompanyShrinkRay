using LCShrinkRay.helper;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class TargetScaling : MonoBehaviour
    {
        internal Vector3 originalScale = Vector3.one;
        internal Vector3 originalOffset = Vector3.zero;

        void Awake()
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            originalScale = gameObject.transform.localScale;

            if(gameObject.TryGetComponent(out GrabbableObject item))
                originalOffset = item.itemProperties.positionOffset;
        }

        public void ScaleRelativeTo(float relationalScale = 1f, Vector3 additionalOffset = new Vector3())
        {
            gameObject.transform.localScale = originalScale * relationalScale;

            if (gameObject.TryGetComponent(out GrabbableObject item))
                item.itemProperties.positionOffset = originalOffset * relationalScale + additionalOffset;
        }

        void OnDestroy()
        {
            gameObject.transform.localScale = originalScale;

            if (gameObject.TryGetComponent(out GrabbableObject item))
                item.itemProperties.positionOffset = originalOffset;
        }
    }
}
