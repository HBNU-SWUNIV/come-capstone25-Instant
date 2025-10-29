using Planet;
using UnityEngine;

namespace Players.Common
{
    public class PlanetBody : MonoBehaviour
    {
        private const float AlignSpeed = 500f;

        public void Initialize(Rigidbody rb)
        {
            if (!PlanetGravity.Instance) return;

            rb.useGravity = false;
            PlanetGravity.Instance.Subscribe(rb);
        }

        private void Update()
        {
            AlignToSurface();
        }

		private void AlignToSurface()
        {
            if (!PlanetGravity.Instance) return;

            var gravityDirection = -PlanetGravity.Instance.GetGravityDirection(transform.position);

            var targetRotation = Quaternion.FromToRotation(
                transform.up, gravityDirection) * transform.rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, AlignSpeed * Time.deltaTime);
        }
    }
}