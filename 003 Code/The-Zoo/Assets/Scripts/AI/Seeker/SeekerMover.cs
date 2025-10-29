using System.Collections;
using Players.Common;
using UnityEngine;
using Utils;

namespace AI.Seeker
{
    public class SeekerMover : MonoBehaviour, ISeekerListener
    {
        [SerializeField] private LPlanetGravity planet;

        [Header("Suspicion")]
        [SerializeField] private float suspicion;
        [SerializeField] private float suspicionThreshold = 100f;
        [SerializeField] private float investigateEnterThreshold = 80f;
        [SerializeField] private float patrolReturnThreshold = 30f;
        [SerializeField] private float quietDelay = 2.0f;
        [SerializeField] private float decayPerSecIdle = 8f;
        [SerializeField] private float decayPerSecChase = 3f;
        [SerializeField] private float chaseRefreshCooldown = 0.25f;

        [Header("Suspicion Gains")]
        [SerializeField] private float gainJump = 10f;
        [SerializeField] private float gainSpin = 5f;
        [SerializeField] private float gainAttack = 30f;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float minArrivalDistance = 5f;
        [SerializeField] private float stayTime = 2f;

        [SerializeField] private AIState state = AIState.Patrol;

        public float detectDistance = 3.5f;
        private Vector3? avoidDirection;
        public float avoidEndTime;

        private bool isWaiting;
        private float lastActionAt;
        private float lastChaseRefreshAt;

        private Transform lastHider;

        private Vector3 moveDirection;
        private LPlanetBody pBody;

        private Rigidbody rBody;
        private Vector3 targetPosition;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var up = -planet.GetGravityDirection(transform.position);
            var tangentDir = ProjectToTangent(moveDirection, up);
            if (tangentDir.sqrMagnitude < 1e-6f) return;

            // Ray 시작점과 방향
            var rayOrigin = transform.position + up * 0.5f;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(rayOrigin, tangentDir * detectDistance);
        }
#endif

        private void Awake()
        {
            rBody = GetComponent<Rigidbody>();
            pBody = GetComponentInParent<LPlanetBody>();
        }

        private void Start()
        {
            pBody.Initialize(rBody);
            EnterPatrol();
        }

        private void FixedUpdate()
        {
            if (!isWaiting && state != AIState.Investigate)
            {
                if (state == AIState.Chase && lastHider)
                {
                    var dir = (lastHider.position - planet.transform.position).normalized;
                    targetPosition = planet.transform.position + dir * planet.GetRadius();
                    SetMoveDirToward(targetPosition);
                }
                else if (state == AIState.Patrol)
                {
                    // 매 프레임 방향 갱신
                    SetMoveDirToward(targetPosition);
                }

                MoveWithObstacleAvoidance();

                if (state == AIState.Patrol)
                {
                    var distSqr = (transform.position - targetPosition).sqrMagnitude;
                    var thresholdSqr = minArrivalDistance * minArrivalDistance;

#if UNITY_EDITOR
                    // print($"[Seeker] distSqr: {distSqr}, thresholdSqr: {thresholdSqr}");
#endif

                    if (distSqr < thresholdSqr)
                        StartCoroutine(WaitAndPickNewTarget());
                }
            }

            DecaySuspicionIfQuiet();
        }


        private void LateUpdate()
        {
            var up = -planet.GetGravityDirection(transform.position);
            var forward = state == AIState.Investigate && lastHider
                ? ProjectToTangent(lastHider.position - transform.position, up)
                : ProjectToTangent(moveDirection, up);

            if (forward.sqrMagnitude < 1e-6f)
                forward = ProjectToTangent(moveDirection, up);

            var targetRot = Quaternion.LookRotation(forward, up);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }

        public void OnHiderAction(Transform hider, HiderActionType action)
        {
            lastActionAt = Time.time;
            lastHider = hider;

            var gain = action switch
            {
                HiderActionType.Jump => gainJump,
                HiderActionType.Spin => gainSpin,
                HiderActionType.Attack => gainAttack,
                _ => 10f
            };

            suspicion = Mathf.Min(suspicion + gain, 120f);

            if (suspicion >= suspicionThreshold &&
                (state != AIState.Chase || Time.time - lastChaseRefreshAt >= chaseRefreshCooldown))
                EnterChase();
            else if (suspicion >= investigateEnterThreshold && state != AIState.Investigate)
                EnterInvestigate();
        }

        public void Initialize()
        {
            StopAllCoroutines();

            isWaiting = false;
            moveDirection = Vector3.zero;
            targetPosition = transform.position;
            suspicion = 0f;
            lastHider = null;
            lastActionAt = Time.time;
            lastChaseRefreshAt = Time.time;

            if (rBody)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }

            EnterPatrol();
        }

        private void EnterPatrol()
        {
            state = AIState.Patrol;
            PickNewTarget();
        }

        private void EnterInvestigate()
        {
            state = AIState.Investigate;
            StopAllCoroutines();
            isWaiting = false;

            if (!rBody) return;

            rBody.linearVelocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;
        }

        private void EnterChase()
        {
            state = AIState.Chase;
            lastChaseRefreshAt = Time.time;
        }

        private void PickNewTarget()
        {
            var retry = 0;

            do
            {
                targetPosition = planet.transform.position + Util.GetRandomPositionInSphere(planet.GetRadius());

                var dir = (targetPosition - transform.position).normalized;
                var ray = new Ray(transform.position + transform.position.normalized * 0.5f, dir);

                if (!Physics.Raycast(ray, detectDistance, LayerMask.GetMask("Interactable"))) break;

                retry++;
            } while (retry < 10);

            SetMoveDirToward(targetPosition);
        }

        private void SetMoveDirToward(Vector3 targetPos)
        {
            moveDirection = (targetPos - transform.position).normalized;

            var up = -planet.GetGravityDirection(transform.position);
            if (Mathf.Abs(Vector3.Dot(up, moveDirection)) > 0.999f)
                moveDirection = Vector3.Cross(up, Vector3.right).normalized;
        }

        private void MoveTowardsTarget()
        {
            var up = -planet.GetGravityDirection(transform.position);
            var tangentDir = ProjectToTangent(moveDirection, up);
            if (tangentDir.sqrMagnitude < 1e-6f) return;

            rBody.MovePosition(rBody.position + tangentDir * (moveSpeed * Time.fixedDeltaTime));
        }

        private void MoveWithObstacleAvoidance()
        {
            var up = -planet.GetGravityDirection(transform.position);
            var tangentDir = ProjectToTangent(moveDirection, up);
            if (tangentDir.sqrMagnitude < 1e-6f) return;

            var ray = new Ray(transform.position + up * 0.5f, tangentDir);

            if (avoidDirection.HasValue && Time.time < avoidEndTime)
            {
                var avoidMove = ProjectToTangent(avoidDirection.Value, up);
                rBody.MovePosition(rBody.position + avoidMove * (moveSpeed * Time.fixedDeltaTime));
                return;
            }

            if (Physics.Raycast(ray, out var hit, detectDistance))
            {
                if (hit.collider.CompareTag("Interactable"))
                {
                    var right = Vector3.Cross(tangentDir, up);
                    var avoid = Random.value < 0.5f ? right : -right;

                    avoidDirection = avoid;
                    avoidEndTime = Time.time + 0.5f; // 0.5초 회피

                    var avoidMove = ProjectToTangent(avoid, up);
                    rBody.MovePosition(rBody.position + avoidMove * (moveSpeed * Time.fixedDeltaTime));
                    return;
                }
            }

            // 장애물이 없을 경우
            avoidDirection = null;
            avoidEndTime = 0f;
            rBody.MovePosition(rBody.position + tangentDir * (moveSpeed * Time.fixedDeltaTime));
        }

        private static Vector3 ProjectToTangent(Vector3 dir, Vector3 up)
        {
            var tangent = Vector3.Cross(Vector3.Cross(up, dir), up);
            return tangent.sqrMagnitude > 1e-6f ? tangent.normalized : Vector3.zero;
        }

        private IEnumerator WaitAndPickNewTarget()
        {
            isWaiting = true;
            yield return new WaitForSeconds(stayTime);
            PickNewTarget();
            isWaiting = false;
        }

        private void DecaySuspicionIfQuiet()
        {
            if (Time.time - lastActionAt < quietDelay) return;

            var decay = (state == AIState.Chase ? decayPerSecChase : decayPerSecIdle) * Time.fixedDeltaTime;
            suspicion = Mathf.Max(0f, suspicion - decay);

            if (suspicion < patrolReturnThreshold && state != AIState.Patrol)
            {
                lastHider = null;
                EnterPatrol();
            }
        }

        private enum AIState
        {
            Patrol,
            Investigate,
            Chase
        }
    }
}