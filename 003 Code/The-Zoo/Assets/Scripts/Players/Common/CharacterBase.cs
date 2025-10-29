using System;
using System.Collections;
using AI.Seeker;
using Mission;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
#endif

namespace Players.Common
{
#if UNITY_EDITOR
    [CustomEditor(typeof(CharacterBase), true)]
    public class CharacterBaseEditor : NetworkTransformEditor
    {
        private SerializedProperty moveSpeed;
        private SerializedProperty runMag;
        private SerializedProperty slowdown;
        private SerializedProperty sensitivity;

        public override void OnEnable()
        {
            moveSpeed = serializedObject.FindProperty(nameof(CharacterBase.moveSpeed));
            runMag = serializedObject.FindProperty(nameof(CharacterBase.runMag));
            slowdown = serializedObject.FindProperty(nameof(CharacterBase.slowdown));
            sensitivity = serializedObject.FindProperty(nameof(CharacterBase.sensitivity));

            base.OnEnable();
        }

        private void DisplayCharacterControllerProperties()
        {
            EditorGUILayout.PropertyField(moveSpeed);
            EditorGUILayout.PropertyField(runMag);
            EditorGUILayout.PropertyField(slowdown);
            EditorGUILayout.PropertyField(sensitivity);
        }

        public override void OnInspectorGUI()
        {
            var characterBase = target as CharacterBase;

            void SetExpanded(bool expanded)
            {
                characterBase.characterPropertiesVisible = expanded;
            }

            if (characterBase)
                DrawFoldOutGroup<CharacterBase>(characterBase.GetType(),
                    DisplayCharacterControllerProperties,
                    characterBase.characterPropertiesVisible, SetExpanded);
            base.OnInspectorGUI();
        }
    }
#endif

    public class CharacterBase : NetworkTransform, IMovable
    {
#if UNITY_EDITOR
        public bool characterPropertiesVisible;
#endif
        [SerializeField] internal float moveSpeed = 3f;
        [SerializeField] internal float runMag = 1.5f;
        [SerializeField] internal float slowdown = 0.2f;
        [SerializeField] internal float sensitivity = 1.5f;

        public event Action OnJumpCallback;
        public event Action<bool> OnSpinCallback;
        public event Action OnAttackCallback;

        public NetworkVariable<bool> isDead = new();

        internal CharacterAnimator animator;
        internal FxHandler fxHandler;
        internal HittableBody hBody;
        internal PlanetBody pBody;
        internal Rigidbody rBody;

        private float speed;
        private float slowdownRate = 1f;
        private bool canAttack = true;
        private bool isHit;

        protected readonly WaitForSeconds respawnWait = new(3f);
        private readonly WaitForSeconds invincibleWait = new(0.5f);
        private readonly WaitForSeconds slowdownWait = new(0.5f);
        private readonly WaitForSeconds attackWait = new(0.8f);

        public bool CanMove { get; set; } = true;
        public bool CanJump { get; set; } = true;
        public bool SpinHold { get; set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            animator = GetComponent<CharacterAnimator>();
            fxHandler = GetComponent<FxHandler>();
            hBody = GetComponent<HittableBody>();
            pBody = GetComponent<PlanetBody>();
            rBody = GetComponent<Rigidbody>();

            if (!IsOwner) return;

            hBody.healthPoint.OnValueChanged += OnHpChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!IsOwner) return;

            hBody.healthPoint.OnValueChanged -= OnHpChanged;
        }

        public void SetSpeed(float v)
        {
            moveSpeed = v;
            speed = moveSpeed;
        }

        public void UpdateSpeed(float v)
        {
            moveSpeed *= v;
            speed = moveSpeed;
        }

        public void UpdateScale(float v)
        {
            transform.localScale = Vector3.one * v;
        }

        protected void Initialize(int hp)
        {
            if (!IsOwner) return;

            CanMove = true;
            CanJump = true;
            SetSpeed(3f);
            UpdateScale(1f);
            slowdownRate = 1f;
            isDead.Value = false;

            hBody.Initialize(hp);
            pBody.Initialize(rBody);
            if(!rBody.isKinematic) rBody.linearVelocity = Vector3.zero;

            animator.Rebind();
        }

        protected void Move(Vector2 dir)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;
            if (!CanMove) return;
            if (SpinHold) return;

            animator.OnMove(dir != Vector2.zero);

            if (dir == Vector2.zero) return;

            var moveDir = transform.forward * dir.y + transform.right * dir.x;
            moveDir.Normalize();

            rBody.MovePosition(rBody.position + moveDir * (slowdownRate * (speed * Time.fixedDeltaTime)));
        }

        protected void Run(bool run)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;

            speed = run ? moveSpeed * runMag : moveSpeed;

            animator.OnRun(run);
        }

        protected void Rotate(float yaw)
        {
            if (!IsOwner) return;
            if (!CanMove) return;
            if (yaw == 0f) return;

            transform.Rotate(Vector3.up * (yaw * sensitivity));
        }

        protected void Jump(Action<HiderActionType> func = null)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;
            if (isHit) return;
            if (!CanMove) return;
            if (!CanJump) return;
            if (SpinHold) return;

            animator.OnJump();

            OnJumpCallback?.Invoke();

            func?.Invoke(HiderActionType.Jump);
        }

        protected void Spin(bool spin, Action<HiderActionType> func = null)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;
            if (isHit) return;
            if (!CanMove) return;
            if (!CanJump) return;

            animator.OnSpin(spin);

            OnSpinCallback?.Invoke(spin);

            if (spin)
            {
                func?.Invoke(HiderActionType.Spin);
            }
        }

        protected void Attack(Action<HiderActionType> func = null)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;
            if (isHit) return;
            if (!canAttack) return;
            if (!IsOwner) return;
            if (!CanMove) return;
            if (!CanJump) return;
            if (SpinHold) return;

            StartCoroutine(AttackCooldownCo());

            animator.OnAttack();

            OnAttackCallback?.Invoke();

            func?.Invoke(HiderActionType.Attack);
        }

        private void Hit()
        {
            if (!IsOwner) return;
            if (isDead.Value) return;

            isHit = true;

            StartCoroutine(HitCo());
        }

        private void Death()
        {
            if (!IsOwner) return;
            if (isDead.Value) return;

            StartCoroutine(DeathCo());
        }

        private void OnHpChanged(int previousValue, int newValue)
        {
            if (previousValue < newValue) return;

            if (newValue <= 0)
            {
                Death();
                return;
            }

            Hit();
        }

        [Rpc(SendTo.Everyone)]
        private void PlayHitFxRpc()
        {
            fxHandler?.PlayHitFx();
        }

        private IEnumerator HitCo()
        {
            PlayHitFxRpc();

            yield return null;

            animator.OnHit();

            slowdownRate = 0.5f;

            yield return invincibleWait;

            isHit = false;

            yield return slowdownWait;

            slowdownRate = 1f;
        }

        protected virtual IEnumerator DeathCo()
        {
            yield return null;
        }

        private IEnumerator AttackCooldownCo()
        {
            canAttack = false;

            yield return attackWait;

            canAttack = true;
        }
    }
}