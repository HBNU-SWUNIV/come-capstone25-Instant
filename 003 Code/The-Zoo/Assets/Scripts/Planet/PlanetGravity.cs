using System.Collections.Generic;
using UnityEngine;

namespace Planet
{
    public class PlanetGravity : MonoBehaviour
    {
        [SerializeField] private LayerMask avoidMask;
        public static PlanetGravity Instance { get; private set; }

        private const float GravityStrength = 9.81f;
        private readonly HashSet<Rigidbody> affectedBodies = new();

        private Renderer rend;
        private Vector3 center;
        private int groundLayerMask;

        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);

            rend = GetComponent<Renderer>();
            center = transform.position;
            groundLayerMask = LayerMask.GetMask("Ground");
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
            return (center - position).normalized;
        }

        private void ApplyGravity()
        {
            foreach (var rb in affectedBodies)
            {
                if (!rb) continue;

                rb.AddForce(GetGravityDirection(rb.position) * GravityStrength, ForceMode.Acceleration);
            }
        }

        public Vector3 GetSurfacePoint(out Vector3 normal)
        {
            const int maxRetry = 10;

            var attempts = 0;
            var radius = GetRadius();

            while (attempts < maxRetry)
            {
                var dir = Random.onUnitSphere;

                var origin = center + dir * (radius + 10f);

                if (Physics.Raycast(origin, -dir, out var hit, 50f))
                {
                    var hitLayer = hit.collider.gameObject.layer;

                    if (((1 << hitLayer) & avoidMask) != 0)
                    {
                        attempts++;
                        continue;
                    }

                    if (((1 << hitLayer) & groundLayerMask) != 0)
                    {
                        normal = hit.normal;
                        return hit.point;
                    }
                }

                attempts++;
            }

            normal = Vector3.up;
            return center + Vector3.up * radius;
        }

        public float GetRadius()
        {
            var size = rend.bounds.size;
            return 0.5f * Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        public Vector3 GetNormal(Vector3 pos)
        {
            return (pos - center).normalized;
        }

        public Vector3 GetSurfacePointByPosition(Vector3 pos)
        {
            var dir = (pos - center).normalized;
            return center + dir * GetRadius();
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