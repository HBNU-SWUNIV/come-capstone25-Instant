using System.Collections.Generic;
using AI.Seeker;
using UnityEngine;

namespace AI
{
    [RequireComponent(typeof(SeekerMover))]
    public class SeekerProximityObserver : MonoBehaviour, ISeekerListener
    {
        [Header("Proximity")]
        public float observeEnterRadius = 12f; // 이 거리 이내면 구독
        public float observeExitRadius = 14f; // 이 거리 밖이면 해제(히스테리시스)
        public float scanInterval = 0.2f;
        public LayerMask hiderMask; // Hider 레이어 지정

        [Header("Optional Filters")]
        public bool useFov;

        [Range(1f, 179f)] public float fovAngle = 110f;
        public bool useLineOfSight;
        public LayerMask obstacleMask;

        [Header("Debug / Gizmos")]
        public bool drawGizmos = true;
        [Range(8, 128)] public int fovSegments = 36; // FOV 호(arc) 정밀도
        public Color gizmoEnterColor = new(0.2f, 0.6f, 1f, 0.6f);
        public Color gizmoExitColor = new(1f, 0.6f, 0.1f, 0.4f);
        public Color gizmoFovColor = new(0.2f, 1f, 0.2f, 0.25f);
        public Color gizmoSeenLine = new(0.2f, 1f, 0.2f, 1f);
        public Color gizmoBlockedLine = new(1f, 0.2f, 0.2f, 1f);

        private readonly Collider[] hits = new Collider[32];
        private readonly HashSet<IHiderBroadcaster> observing = new();

        private SeekerMover seeker;

        private void Awake()
        {
            seeker = GetComponent<SeekerMover>();
        }

        private void OnEnable()
        {
            InvokeRepeating(nameof(Scan), Random.Range(0f, scanInterval), scanInterval);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Scan));
            UnsubscribeAll();
        }

#if UNITY_EDITOR
        // ---------- Gizmos ----------
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // 반경(Enter/Exit)
            Gizmos.color = gizmoEnterColor;
            Gizmos.DrawWireSphere(transform.position, observeEnterRadius);

            Gizmos.color = gizmoExitColor;
            Gizmos.DrawWireSphere(transform.position, observeExitRadius);

            // FOV 웻지(선택)
            if (useFov)
                DrawFovGizmo();

            // 현재 구독 중인 Hider와의 라인(LoS에 따라 색 분기)
            if (Application.isPlaying && observing.Count > 0)
                foreach (var h in observing)
                {
                    var los = !useLineOfSight || HasLineOfSight(h.HiderTransform.position);
                    Gizmos.color = los ? gizmoSeenLine : gizmoBlockedLine;
                    Gizmos.DrawLine(transform.position, h.HiderTransform.position);
                }
        }
#endif

        // ISeekerListener 구현: Hider 행동 수신
        public void OnHiderAction(Transform hider, HiderActionType action)
        {
            seeker.OnHiderAction(hider, action);
        }

        private void Scan()
        {
            var n = Physics.OverlapSphereNonAlloc(
                transform.position, observeEnterRadius, hits, hiderMask,
                QueryTriggerInteraction.Ignore);

            // 1) 현재 반경 내 Hider 집합
            for (var i = 0; i < n; i++)
            {
                var h = hits[i] ? hits[i].GetComponentInParent<IHiderBroadcaster>() : null;
                if (h == null) continue;

                // 옵션: FOV/LoS 필터
                if (useFov && !InFov(h.HiderTransform.position)) continue;
                if (useLineOfSight && !HasLineOfSight(h.HiderTransform.position)) continue;

                if (!observing.Contains(h))
                {
                    h.RegisterListener(this);
                    observing.Add(h);
                }
            }

            // 2) 반경 밖(or 필터 불만족) → 구독 해제
            var toRemove = new List<IHiderBroadcaster>();
            foreach (var h in observing)
            {
                var d = Vector3.Distance(transform.position, h.HiderTransform.position);
                if (d > observeExitRadius ||
                    (useFov && !InFov(h.HiderTransform.position)) ||
                    (useLineOfSight && !HasLineOfSight(h.HiderTransform.position)))
                {
                    h.UnregisterListener(this);
                    toRemove.Add(h);
                }
            }

            foreach (var h in toRemove) observing.Remove(h);
        }

        private bool InFov(Vector3 targetPos)
        {
            var to = (targetPos - transform.position).normalized;
            var ang = Vector3.Angle(transform.forward, to);
            return ang <= fovAngle * 0.5f;
        }

        private bool HasLineOfSight(Vector3 targetPos)
        {
            var dir = (targetPos - transform.position).normalized;
            var dist = Vector3.Distance(transform.position, targetPos);
            return !Physics.Raycast(transform.position, dir, dist, obstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void UnsubscribeAll()
        {
            foreach (var h in observing) h.UnregisterListener(this);
            observing.Clear();
        }

        private void DrawFovGizmo()
        {
            // 전방 평면에서 FOV 호(arc)와 양 옆 경계선
            var origin = transform.position;
            var forward = transform.forward;
            var up = transform.up;
            var right = Vector3.Cross(up, forward).normalized;

            var half = fovAngle * 0.5f;
            var seg = Mathf.Max(8, fovSegments);

            // 경계 방향
            var leftRot = Quaternion.AngleAxis(-half, up);
            var rightRot = Quaternion.AngleAxis(half, up);
            var leftDir = leftRot * forward;
            var rightDir = rightRot * forward;

            // 경계선
            Gizmos.color = gizmoFovColor;
            Gizmos.DrawLine(origin, origin + leftDir * observeEnterRadius);
            Gizmos.DrawLine(origin, origin + rightDir * observeEnterRadius);

            // 호(arc)
            var prev = origin + leftDir * observeEnterRadius;
            for (var i = 1; i <= seg; i++)
            {
                var t = (float)i / seg; // 0..1
                var ang = Mathf.Lerp(-half, half, t);
                var dir = Quaternion.AngleAxis(ang, up) * forward;
                var next = origin + dir * observeEnterRadius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}