using System;
using System.Collections;
using AI.Seeker;
using DG.Tweening;
using Planet;
using Scriptable;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
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
        private SerializedProperty sensitivity;

        public override void OnEnable()
        {
            moveSpeed = serializedObject.FindProperty(nameof(CharacterBase.moveSpeed));
            runMag = serializedObject.FindProperty(nameof(CharacterBase.runMag));
            sensitivity = serializedObject.FindProperty(nameof(CharacterBase.sensitivity));

            base.OnEnable();
        }

        private void DisplayCharacterControllerProperties()
        {
            EditorGUILayout.PropertyField(moveSpeed);
            EditorGUILayout.PropertyField(runMag);
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

    public class CharacterBase : NetworkTransform, IMovable, IAnimalType
    {
#if UNITY_EDITOR
        public bool characterPropertiesVisible;
#endif
        [SerializeField] internal float moveSpeed = 3f;
        [SerializeField] internal float runMag = 1.5f;
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

        private float currentSpeed;
        private float currentSize = 1f;
        private bool canAttack = true;
        private bool isHit;
        private bool isStunned;

        protected readonly WaitForSeconds respawnWait = new(3f);
        private readonly WaitForSeconds invincibleWait = new(0.5f);
        private readonly WaitForSeconds attackWait = new(0.8f);

        private Tween scaleTween;

        public bool CanMove { get; set; } = true;
        public bool CanJump { get; set; } = true;
        public bool SpinHold { get; set; }
        public AnimalType Type { get; set; }

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
            scaleTween?.Kill();
        }

        public void SetSpeed(float v)
        {
            moveSpeed = v;
            currentSpeed = moveSpeed;
        }

        public void UpdateSpeed(float percent)
        {
            currentSpeed = moveSpeed * percent;
        }

        public void UpdateScale(float v)
        {
            scaleTween?.Kill();

            scaleTween = transform.DOScale(Vector3.one * v, 1.0f)
                .SetEase(Ease.OutBack);
        }

        protected void Initialize(int hp)
        {
            if (!IsOwner) return;

            CanMove = true;
            CanJump = true;
            SetSpeed(moveSpeed);
            UpdateScale(1f);
            isDead.Value = false;

            hBody.Initialize(hp);
            pBody.Initialize(rBody);
            if(!rBody.isKinematic) rBody.linearVelocity = Vector3.zero;

            animator.Rebind();
        }

        protected void InitialLobbyPosition(int index)
        {
            var pos = Util.GetCirclePositions(Vector3.zero, index, 2f, 4);
            Teleport(pos, Quaternion.LookRotation((Vector3.zero - pos).normalized), Vector3.one);
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

            rBody.MovePosition(rBody.position + moveDir * (currentSpeed * Time.fixedDeltaTime));
        }

        protected void Run(bool run)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;

            currentSpeed = run ? moveSpeed * runMag : moveSpeed;

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

        internal void Stun(float time)
        {
            if (!IsOwner) return;
            if (isDead.Value) return;

            StartCoroutine(StunCo(time));
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
            if (isHit) return;

            PlayHitFxRpc();

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

        [Rpc(SendTo.Everyone)]
        private void PlayStunFxRpc(float time)
        {
            fxHandler?.PlayStunFx(time);
        }

        private IEnumerator HitCo()
        {
            animator.OnHit();

            yield return invincibleWait;

            isHit = false;
        }

        private IEnumerator StunCo(float time)
        {
            if (isStunned) yield break;

            isStunned = true;

            animator.OnDeath();

            PlayStunFxRpc(time);

            yield return new WaitForSeconds(time);

            animator.OnIdle();

            CanMove = true;

            isStunned = false;
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