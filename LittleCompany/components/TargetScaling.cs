using UnityEngine;

namespace LittleCompany.components
{
    internal class TargetScaling : MonoBehaviour
    {
        internal Vector3 originalScale = Vector3.one;
        internal Vector3 originalOffset = Vector3.zero;

        public float CurrentScale = 1f;

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

            CurrentScale = relationalScale;
        }

        public Vector3 SizeAt(float percentage)
        {
            return originalScale * percentage;
        }

        public bool Unchanged => SizeAt(1f) == originalScale;

        void OnDestroy()
        {
            gameObject.transform.localScale = originalScale;

            if (gameObject.TryGetComponent(out GrabbableObject item))
                item.itemProperties.positionOffset = originalOffset;
        }
    }
}
