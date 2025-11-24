using System.Collections;
using DG.Tweening;
using Planet;
using Players.Common;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
#endif

namespace GamePlay
{
#if UNITY_EDITOR
        [CustomEditor(typeof(TornadoMover), true)]
        public class TornadoMoverEditor : NetworkTransformEditor
        {
            private SerializedProperty particles;

            private SerializedProperty moveSpeed;
            private SerializedProperty travelDistance;

            private SerializedProperty stopDuration;
            private SerializedProperty fadeDuration;

            public override void OnEnable()
            {
                particles = serializedObject.FindProperty(nameof(TornadoMover.particles));
                moveSpeed = serializedObject.FindProperty(nameof(TornadoMover.moveSpeed));
                travelDistance = serializedObject.FindProperty(nameof(TornadoMover.travelDistance));
                stopDuration = serializedObject.FindProperty(nameof(TornadoMover.stopDuration));
                fadeDuration = serializedObject.FindProperty(nameof(TornadoMover.fadeDuration));

                base.OnEnable();
            }

            private void DisplayTornadoMoverProperties()
            {
                EditorGUILayout.PropertyField(particles);
                EditorGUILayout.PropertyField(moveSpeed);
                EditorGUILayout.PropertyField(travelDistance);
                EditorGUILayout.PropertyField(stopDuration);
                EditorGUILayout.PropertyField(fadeDuration);
            }

            public override void OnInspectorGUI()
            {
                var tornadoMover = target as TornadoMover;

                void SetExpanded(bool expanded)
                {
                    tornadoMover.tornadoPropertiesVisible = expanded;
                }

                if (tornadoMover)
                    DrawFoldOutGroup<TornadoMover>(tornadoMover.GetType(),
                        DisplayTornadoMoverProperties,
                        tornadoMover.tornadoPropertiesVisible, SetExpanded);
                base.OnInspectorGUI();
            }
        }
#endif

    public class TornadoMover : NetworkTransform
    {
#if UNITY_EDITOR
            public bool tornadoPropertiesVisible;
#endif
        [SerializeField] internal ParticleSystem[] particles;

        [Header("Movement")]
        [SerializeField] internal float moveSpeed = 1f;
        [SerializeField] internal float travelDistance = 25f;

        [Header("Lifetime")]
        [SerializeField] internal float stopDuration = 2f;
        [SerializeField] internal float fadeDuration = 1.5f;

        private float movedDistance;
        private Vector3 lastPos;
        private bool isStopped;

        private PlanetGravity gravity;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            lastPos = transform.position;

            gravity = PlanetGravity.Instance;
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            if (isStopped) return;

            movedDistance += (transform.position - lastPos).magnitude;
            lastPos = transform.position;

            if (movedDistance >= travelDistance)
            {
                StopAndDespawnRpc();
                return;
            }

            MoveForward();
            AlignToSurface();
        }

        private IEnumerator OnHitCo(Collider other)
        {
            if (!other.TryGetComponent<CharacterBase>(out var character)) yield break;
            character.Stun(5f);

            if (!other.TryGetComponent<HittableBody>(out var comp)) yield break;
            comp.KnockBackRpc(transform.position, 40f, 0.5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            StartCoroutine(OnHitCo(other));
        }

        private void MoveForward()
        {
            var pos = transform.position;

            pos += transform.forward * (moveSpeed * Time.fixedDeltaTime);

            pos = gravity.GetSurfacePointByPosition(pos);

            transform.position = pos;
        }

        private void AlignToSurface()
        {
            if (!PlanetGravity.Instance) return;

            var normal = gravity.GetNormal(transform.position);

            var targetRot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                10f * Time.deltaTime
            );
        }

        private void StopAllParticles()
        {
            foreach (var p in particles)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void StopAndDespawnRpc()
        {
            if (!IsOwner) return;

            isStopped = true;

            StopAllParticles();

            DOVirtual.DelayedCall(stopDuration, () =>
            {
                transform.DOScale(Vector3.zero, fadeDuration).SetEase(Ease.InQuad);

                DOVirtual.DelayedCall(fadeDuration, () =>
                {
                    if (!TryGetComponent<NetworkObject>(out var no)) return;
                    if (!no.IsSpawned) return;

                    no.DeferDespawn(1);
                });
            });
        }
    }
}