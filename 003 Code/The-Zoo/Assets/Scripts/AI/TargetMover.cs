using UnityEngine;
using Utils;

namespace AI
{
    public class TargetMover : MonoBehaviour
    {
        [SerializeField] private LPlanetGravity planet;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Hider"))
                MoveRandomPosition();
        }

        public void MoveRandomPosition()
        {
            transform.position = planet.transform.position + Util.GetRandomPositionInSphere(8f);
        }
    }
}