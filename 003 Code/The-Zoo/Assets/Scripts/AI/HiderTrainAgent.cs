using System.Collections;
using System.Collections.Generic;
using AI.Seeker;
using Players;
using Players.Common;
using Unity.Cinemachine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace AI
{
    public class HiderTrainAgent : Agent, IMovable, IHiderBroadcaster
    {
        [SerializeField] private LPlanetGravity planet;
        [SerializeField] private SeekerMover seeker;
        [SerializeField] private TargetMover target;
        [SerializeField] private List<Transform> interactables;

        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float runMag = 1.5f;
        [SerializeField] internal float sensitivity = 0.15f;

        [Header("Suspicion Settings")]
        [SerializeField] private float maxSuspicion = 150f;
        [SerializeField] private float suspicionDecayPerSecond = 3f;
        [SerializeField][MinMaxRangeSlider(0, 100)] private Vector2 suspicionSafeZone;
        [SerializeField] private float suspicionBonus = 0.002f;
        [SerializeField] private float suspicionPenalty = -0.003f;

        [Header("Suspicion Gains")]
        [SerializeField] private float gainJump = 10f;
        [SerializeField] private float gainSpin = 1f;
        [SerializeField] private float gainAttack = 30f;
        public float suspicion;

        public Vector2 moveInput;
        public Vector2 lookInput;

        private readonly List<ISeekerListener> listeners = new();
        private readonly WaitForSeconds attackWait = new(0.8f);
        private Animator animator;
        private BehaviorParameters bp;
        private DecisionRequester dr;
        private PlayerInputHandler input;

        private bool isMove;
        private bool isRun;
        private bool isSpin;
        private LPlanetBody pBody;

        // agents components
        private RayPerceptionSensorComponent3D raySensor;

        private Rigidbody rBody;

        private float speed;
        private bool canAttack = true;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Interactable"))
                AddReward(-0.01f);

            if (collision.collider.CompareTag("PickUp")) AddReward(0.2f);
            if (collision.collider.CompareTag("Seeker"))
            {
                AddReward(-0.2f);
                EndEpisode();
            }
        }

        public void RegisterListener(ISeekerListener l)
        {
            if (l != null && !listeners.Contains(l)) listeners.Add(l);
        }

        public void UnregisterListener(ISeekerListener l)
        {
            if (l != null) listeners.Remove(l);
        }

        public Transform HiderTransform => transform;
        public bool CanMove { get; set; } = true;
        public bool CanJump { get; set; } = true;
        public bool SpinHold { get; set; }

        private void BroadcastAction(HiderActionType action)
        {
            // 행동 발생 시 호출 (점프/스핀/공격 등)
            foreach (var l in listeners)
                l.OnHiderAction(transform, action);
        }

        public override void Initialize()
        {
            base.Initialize();

            raySensor = GetComponent<RayPerceptionSensorComponent3D>();
            dr = GetComponent<DecisionRequester>();
            bp = GetComponent<BehaviorParameters>();

            rBody = GetComponent<Rigidbody>();
            pBody = GetComponent<LPlanetBody>();
            animator = GetComponent<Animator>();
            input = GetComponent<PlayerInputHandler>();

            pBody.Initialize(rBody);

            if (bp.BehaviorType == BehaviorType.HeuristicOnly)
            {
                dr.DecisionPeriod = 1;
                input.enabled = true;
            }
            else
            {
                input.enabled = false;
            }
        }

        public override void OnEpisodeBegin()
        {
            transform.position = planet.transform.position +
                                 Util.GetRandomPositionInSphere(planet.GetRadius());

            rBody.linearVelocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;

            CanMove = true;
            CanJump = true;
            SpinHold = false;

            seeker.Initialize();

            isSpin = false;
            isRun = false;

            animator.Rebind();

            suspicion = 60f;

            if (seeker)
                seeker.transform.position =
                    planet.transform.position + Util.GetRandomPositionInSphere(7.5f);

            if (target) target.MoveRandomPosition();

            foreach (var interactable in interactables)
            {
                interactable.position =
                    planet.transform.position + Util.GetRandomPositionInSphere(8.5f);

                var normal = -planet.GetGravityDirection(interactable.position);
                interactable.rotation =
                    Quaternion.FromToRotation(Vector3.up, normal);
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.up.normalized);
            sensor.AddObservation(transform.forward.normalized);
            sensor.AddObservation(transform.right.normalized);
            sensor.AddObservation(moveInput);
            sensor.AddObservation(lookInput);
            sensor.AddObservation(CanMove);
            sensor.AddObservation(CanJump);
            sensor.AddObservation(SpinHold);
            sensor.AddObservation(isSpin);
            sensor.AddObservation(isRun);

            sensor.AddObservation(GetSeekerViewDot());
            sensor.AddObservation(suspicion / maxSuspicion);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var dActions = actions.DiscreteActions;

            MoveAction(dActions[0], dActions[1]);
            LookAction(dActions[2]);
            JumpAction(dActions[3]);
            SpinAction(dActions[4]);
            RunAction(dActions[5]);
            AttackAction(dActions[6]);
            HandleRewards();
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actionOut = actionsOut.DiscreteActions;

            moveInput = input.MoveInput;

            actionOut[0] = moveInput.y switch
            {
                > 0 => 1,
                < 0 => 2,
                _ => 0
            };
            actionOut[1] = moveInput.x switch
            {
                > 0 => 1,
                < 0 => 2,
                _ => 0
            };

            lookInput = input.LookInput;

            actionOut[2] = lookInput.x switch
            {
                > 0 => 1,
                < 0 => 2,
                _ => 0
            };

            actionOut[3] = input.InputActions.Player.Jump.phase == InputActionPhase.Performed
                ? 1
                : 0;
            actionOut[4] = input.InputActions.Player.Spin.phase == InputActionPhase.Performed
                ? 1
                : 0;
            actionOut[5] = input.InputActions.Player.Run.phase == InputActionPhase.Performed
                ? 1
                : 0;
            actionOut[6] = input.InputActions.Player.Attack.phase == InputActionPhase.Performed
                ? 1
                : 0;
        }

        private void MoveAction(int action1, int action2)
        {
            if (!CanMove) return;
            if (SpinHold) return;

            moveInput.y = action1 switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };
            moveInput.x = action2 switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };

            isMove = moveInput != Vector2.zero;

            animator.SetBool(CharacterAnimator.MoveHash, isMove);

            if (moveInput == Vector2.zero) return;

            var moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
            moveDir.Normalize();

            rBody.MovePosition(rBody.position +
                               moveDir * (speed * Time.fixedDeltaTime));
        }

        private void LookAction(int action)
        {
            lookInput.x = action switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };

            if (!CanMove) return;
            if (lookInput.x == 0f) return;

            transform.Rotate(Vector3.up * (lookInput.x * sensitivity));
        }

        private void JumpAction(int action)
        {
            if (action != 1) return;
            if (!CanMove) return;
            if (!CanJump) return;
            if (SpinHold) return;

            BroadcastAction(HiderActionType.Jump);
            suspicion = Mathf.Min(suspicion + gainJump, maxSuspicion);
            animator.SetTrigger(CharacterAnimator.JumpHash);
        }

        private bool lastSpin;

        private void SpinAction(int action)
        {
            if (!CanMove) return;
            if (!CanJump) return;

            isSpin = action == 1;

            animator.SetBool(CharacterAnimator.SpinHash, isSpin);

            var info = animator.GetCurrentAnimatorStateInfo(0);

            if (!info.IsName("Spin")) return;
            if (info.normalizedTime >= 1.15f && !lastSpin)
            {
                lastSpin = true;
                BroadcastAction(HiderActionType.Spin);
                suspicion = Mathf.Min(suspicion + gainSpin, maxSuspicion);
            }
            else
            {
                lastSpin = false;
            }
        }

        private void RunAction(int action)
        {
            if (!CanMove) return;
            if (SpinHold) return;

            isRun = action == 1;

            speed = isRun ? moveSpeed * runMag : moveSpeed;

            animator.SetBool(CharacterAnimator.RunHash, isRun);
        }

        private void AttackAction(int action)
        {
            if (!canAttack) return;
            if (action != 1) return;
            if (!CanMove) return;
            if (!CanJump) return;
            if (SpinHold) return;

            StartCoroutine(AttackCooldownCo());

            BroadcastAction(HiderActionType.Attack);
            suspicion = Mathf.Min(suspicion + gainAttack, maxSuspicion);
            animator.SetTrigger(CharacterAnimator.AttackHash);
        }

        private void HandleSuspicion()
        {
            if (suspicion > 0f)
            {
                suspicion -= suspicionDecayPerSecond * Time.fixedDeltaTime;
                if (suspicion < 0f) suspicion = 0f;
            }

            if (suspicion < suspicionSafeZone.x || suspicion > suspicionSafeZone.y)
            {
                AddReward(suspicionPenalty);
            }
            else
            {
                AddReward(suspicionBonus);
            }

            // 4. 일정 수치 초과 시 실패 처리
            if (suspicion >= maxSuspicion)
            {
                AddReward(-1f);
                EndEpisode();
            }
        }

        private void RewardIdle()
        {
            var isIdleNow = rBody.linearVelocity.magnitude < 0.01f
                            && !isRun
                            && !isSpin
                            && !isMove;

            var isSeekerVisible = GetSeekerViewDot() >= 0f;

            // 의심도가 낮고(안전) / Seeker가 안 보이고 / idle일 때
            if (isIdleNow && !isSeekerVisible && suspicion < suspicionSafeZone.y)
            {
                AddReward(0.0005f);
            }
        }

        private void HandleRewards()
        {
            if (rBody.linearVelocity.magnitude > 0.005f) // 이동 중일 때만
            {
                var gravityDir = -planet.GetGravityDirection(transform.position).normalized;

                var tangentVelocity = Vector3.ProjectOnPlane(rBody.linearVelocity.normalized, gravityDir).normalized;
                var tangentForward = Vector3.ProjectOnPlane(transform.forward, gravityDir).normalized;

                var alignment = Vector3.Dot(tangentForward, tangentVelocity);

                if (alignment > 0.95f) // 정면에 가까운 방향으로 이동 중
                {
                    AddReward(0.0005f); // 의미 있는 생존 보상
                }
                else if (alignment > 0.5f)
                {
                    AddReward(0.0002f);
                }
            }

            var dot = GetSeekerViewDot();

            if (dot >= 0f)
            {
                if (dot > 0.8f) AddReward(-0.001f);
                else if (dot < 0.5f) AddReward(0.001f);
            }

            HandleSuspicion();

            RewardIdle();
        }

        private void IsSeekerFind(out Transform tr)
        {
            tr = null;

            if (!raySensor) return;

            var observations = raySensor.RaySensor.RayPerceptionOutput;

            if (observations.RayOutputs == null) return;

            foreach (var sub in observations.RayOutputs)
                if (sub.HitTaggedObject && sub.HitGameObject.CompareTag("Seeker"))
                {
                    tr = sub.HitGameObject.transform;
                    return;
                }
        }

        private float GetSeekerViewDot()
        {
            IsSeekerFind(out var seekerTr);
            if (!seekerTr) return -1f;

            var toHider = (transform.position - seekerTr.position).normalized;
            var seekerForward = seekerTr.forward;
            return Vector3.Dot(seekerForward, toHider); // 1에 가까울수록 정면
        }

        protected void Hit()
        {
            animator.SetTrigger(CharacterAnimator.HitHash);
        }

        private IEnumerator AttackCooldownCo()
        {
            canAttack = false;

            yield return attackWait;

            canAttack = true;
        }
    }
}