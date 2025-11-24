using Players.Common;
using Unity.Netcode;
using UnityEngine;

namespace Animations.Vfx
{
    public class MovementVfx : NetworkBehaviour
    {
        [SerializeField] private float backOffset = 0.10f;

        [SerializeField] private float stepInterval = 0.25f;
        [SerializeField] private float moveThreshold = 0.0025f;

        private CharacterBase character;
        private Vector3 lastPos;
        private float timer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            character = GetComponent<CharacterBase>();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }
            lastPos = transform.position;
        }

        private void FixedUpdate()
        {
            if (character.isDead.Value) return;

            var sqrMove = (transform.position - lastPos).sqrMagnitude;

            if (sqrMove > moveThreshold)
            {
                var speed = Mathf.Sqrt(sqrMove) / Time.fixedDeltaTime;
                var speedFactor = Mathf.Clamp(speed * 0.2f, 0.6f, 2.0f);

                var interval = stepInterval / speedFactor;

                timer += Time.fixedDeltaTime;

                if (timer >= interval)
                {
                    SpawnVfx();
                    timer = 0f;
                }

            }
            else
            {
                timer = 0f;
            }

            lastPos = transform.position;
        }

        private void SpawnVfx()
        {
            var pos = transform.position
                      + transform.forward * backOffset;

            VfxManager.Instance.PlayVfx(pos);
        }
    }
}