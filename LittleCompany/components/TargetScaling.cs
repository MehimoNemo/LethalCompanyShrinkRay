using UnityEngine;

namespace LittleCompany.components
{
    internal class TargetScaling : MonoBehaviour
    {
        internal Vector3 intendedScale = Vector3.one;    // The scale this object should have by default (Changes e.g. after modification through shrinkRay)
        internal Vector3 originalScale = Vector3.one;
        internal Vector3 originalOffset = Vector3.zero;

        public float CurrentSize = 1f, IntendedSize = 1f;

        void Awake()
        {
            if (gameObject == null)
            {
                Plugin.Log("TargetHighlighting -> no gameObject found!", Plugin.LogType.Error);
                return;
            }

            originalScale = gameObject.transform.localScale;
            intendedScale = originalScale;

            if(gameObject.TryGetComponent(out GrabbableObject item))
                originalOffset = item.itemProperties.positionOffset;
        }

        public void ScaleRelativeTo(float relationalSize = 1f, Vector3 additionalOffset = new Vector3(), bool saveAsIntendedSize = false)
        {
            gameObject.transform.localScale = intendedScale * relationalSize;

            if (gameObject.TryGetComponent(out GrabbableObject item))
                item.itemProperties.positionOffset = originalOffset * relationalSize + additionalOffset;

            CurrentSize = relationalSize;

            if(saveAsIntendedSize)
            {
                IntendedSize = CurrentSize;
                intendedScale = originalScale * IntendedSize;
                //Plugin.Log("New intended scale is: " + intendedScale + " with a relative size of " + IntendedSize);
            }
        }

        public Vector3 SizeAt(float percentage)
        {
            return originalScale * percentage;
        }

        public bool Unchanged => originalScale == intendedScale;

        public void Reset()
        {
            //Plugin.Log("Reset scaled object to size: " + intendedScale);
            gameObject.transform.localScale = intendedScale;
            CurrentSize = IntendedSize;

            if (gameObject.TryGetComponent(out GrabbableObject item))
                item.itemProperties.positionOffset = originalOffset;
        }
    }
}
