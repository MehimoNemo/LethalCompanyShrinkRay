using LCShrinkRay.comp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class TranslateRelativeOffset : MonoBehaviour
    {
        public GameObject go { get; private set; }

        public static void StartRoutine(Transform referenceTransform, GrabbableObject grabbableToMove, Vector3 relativeOffset)
        {
            var routine = grabbableToMove.gameObject.AddComponent<TranslateRelativeOffset>();
            routine.go = grabbableToMove.gameObject;
            routine.StartCoroutine(routine.run(referenceTransform, grabbableToMove, relativeOffset));
        }

        private IEnumerator run(Transform referenceTransform, GrabbableObject grabbableToMove, Vector3 relativeOffset)
        {
            float delay = grabbableToMove.itemProperties.grabAnimationTime + 0.2f;
            yield return new WaitForSeconds(delay);

            Debug.Log("TADAAAA IT'S TIME TO GET ANGLE!!!");

            // Get the reference rotation
            Quaternion referenceRotation = referenceTransform.rotation;

            // Calculate the relative rotation
            Quaternion relativeRotation = Quaternion.Inverse(referenceRotation) * go.transform.rotation * Quaternion.Inverse(Quaternion.Euler(grabbableToMove.itemProperties.rotationOffset));

            // Apply the relative rotation to the local offset
            Vector3 offsetWorld = relativeRotation * relativeOffset;

            // Apply the offset to the current position
            //Vector3 newPosition = grabbableToMove.itemProperties.positionOffset + offsetWorld;
            Vector3 newPosition = offsetWorld;

            // Update the object's position offset
            grabbableToMove.itemProperties.positionOffset = newPosition;
            Plugin.log("newPosition: " + newPosition);
        }
    }
}
