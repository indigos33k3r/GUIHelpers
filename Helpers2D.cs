﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace toxicFork.GUIHelpers {
    public class Helpers2D : Helpers {
        /// <summary>
        /// Transforms a 2D point into the transform's local space.
        /// </summary>
        /// <param name="transform">The transform of the selected object.</param>
        /// <param name="point">The world coordinates of the point.</param>
        /// <returns>The point in the object's local space.</returns>
        public static Vector2 TransformPoint(Transform transform, Vector2 point) {
            Vector2 rotatedScaledPoint = TransformVector(transform, point);
            Vector2 translatedRotatedScaledPoint = (Vector2) transform.position + rotatedScaledPoint;
            return translatedRotatedScaledPoint;
        }

        /// <summary>
        /// Transforms a 2D vector into the transform's local space.
        /// </summary>
        /// <param name="transform">The transform of the selected object.</param>
        /// <param name="vector">The world-space vector to transform.</param>
        /// <returns>The vector in the object's local space.</returns>
        public static Vector2 TransformVector(Transform transform, Vector2 vector) {
            Vector2 scaledPoint = Vector2.Scale(vector, transform.lossyScale);
            float angle = transform.rotation.eulerAngles.z;
            Vector2 rotatedScaledPoint = Quaternion.AngleAxis(angle, Vector3.forward)*scaledPoint;
            return rotatedScaledPoint;
        }

        public static Vector2 InverseTransformPoint(Transform transform, Vector2 point) {
            Vector2 rotatedScaledVector = point - (Vector2) transform.position;
            return InverseTransformVector(transform, rotatedScaledVector);
        }

        public static Vector2 InverseTransformVector(Transform transform, Vector2 vector) {
            float angle = transform.rotation.eulerAngles.z;
            Vector2 scaledVector = Quaternion.AngleAxis(-angle, Vector3.forward)*vector;
            Vector2 inverseTransformVector = Vector2.Scale(scaledVector, new Vector2(1/transform.lossyScale.x, 1/transform.lossyScale.y));
            return inverseTransformVector;
        }

        public static float GetAngle(Vector2 direction) {
            return Mathf.Rad2Deg*Mathf.Atan2(direction.y, direction.x);
        }

        public static Quaternion Rotate(float angle) {
            return Quaternion.AngleAxis(angle, Vector3.forward);
        }

        public static Vector2 GetDirection(float angle) {
            return Rotate(angle)*Vector3.right;
        }

        public static float DistanceToLine(Ray ray, Vector3 point) {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }

        public static Vector2 ClosestPointToRay(Ray ray, Vector2 point) {
            //http://pastie.org/1066490
            float t = Vector2.Dot(point - (Vector2) ray.origin, ray.direction);

            return ray.GetPoint(t);
        }

        public static Vector2 Intersect2DPlane(Ray ray) {
            float d = Vector3.Dot(-ray.origin, Vector3.forward)/Vector3.Dot(ray.direction, Vector3.forward);
            return ray.GetPoint(d);
        }
        
#if UNITY_EDITOR
        public static Vector2 GUIPointTo2DPosition(Vector2 position) {
            return Intersect2DPlane(HandleUtility.GUIPointToWorldRay(position));
        }
#endif

        public static float DistanceAlongLine(Ray ray, Vector2 wantedPosition) {
            Ray normalFromCenter = new Ray(ray.origin, new Vector2(-ray.direction.y, ray.direction.x));
            float distance = DistanceToLine(normalFromCenter, wantedPosition);

            Vector2 wantedDirection = (wantedPosition - (Vector2)ray.origin).normalized;

            float dot = Vector2.Dot(wantedDirection, ray.direction);
            if (dot < 0)
            {
                distance *= -1;
            }

            return distance;
        }

        public static Vector2 GetNormal(Vector2 direction) {
            return new Vector2(-direction.y, direction.x);
        }
    }
}