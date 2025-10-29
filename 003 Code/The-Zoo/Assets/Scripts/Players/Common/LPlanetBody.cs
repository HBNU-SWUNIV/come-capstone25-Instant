using System;
using UnityEngine;

namespace Players.Common
{
    public class LPlanetBody : MonoBehaviour
    {
        private const float AlignSpeed = 5000f;
        public LPlanetGravity planet;

        public void Initialize(Rigidbody rb)
        {
            rb.useGravity = false;
            planet.Subscribe(rb);
        }

        private void Update()
        {
            AlignToSurface();
        }

		private void AlignToSurface()
        {
            if (!planet) return;

            var gravityDirection = -planet.GetGravityDirection(transform.position);

            var targetRotation = Quaternion.FromToRotation(
                transform.up, gravityDirection) * transform.rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, AlignSpeed * Time.deltaTime);
        }
    }
}