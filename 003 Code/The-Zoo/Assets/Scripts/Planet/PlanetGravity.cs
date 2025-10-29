using System.Collections.Generic;
using UnityEngine;

namespace Planet
{
    public class PlanetGravity : MonoBehaviour
    {
        public static PlanetGravity Instance { get; private set; }

        private const float GravityStrength = 98.1f;
        private readonly HashSet<Rigidbody> affectedBodies = new();

        private Renderer rend;

        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);

            rend = GetComponent<Renderer>();
        }

        private void OnDestroy()
        {
            affectedBodies.Clear();
        }

        private void FixedUpdate()
        {
            ApplyGravity();
        }

        public Vector3 GetGravityDirection(Vector3 position)
        {
            return (transform.position - position).normalized;
        }

        private void ApplyGravity()
        {
            foreach (var rb in affectedBodies)
            {
                if (!rb) continue;

                rb.AddForce(GetGravityDirection(rb.position) * GravityStrength, ForceMode.Acceleration);
            }
        }

        public Vector3 GetSurfacePoint(Vector3 direction, out Vector3 normal)
        {
            normal = direction.normalized;
            direction.Normalize();

            var radius = GetRadius();
            var center = transform.position;

            // 구의 바깥쪽에서 안쪽으로 레이 쏘기
            var origin = center + direction * (radius + 10f);
            var layerMask = LayerMask.GetMask("Ground");

            if (!Physics.Raycast(origin, -direction, out var hit, 50f, layerMask))
                return center + direction * radius;

            normal = hit.normal;
            return hit.point;
        }


        public float GetRadius()
        {
            var size = rend.bounds.size;
            return 0.5f * Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        public void Subscribe(Rigidbody rb)
        {
            if (!rb) return;

            affectedBodies.Add(rb);
        }

        public void Unsubscribe(Rigidbody rb)
        {
            if (!rb) return;

            affectedBodies.Remove(rb);
        }
    }
}