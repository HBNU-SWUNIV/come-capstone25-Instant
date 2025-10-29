using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.InGame
{
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text tmp;

        private const float PopScale = 1.25f;
        private const float PopDuration = 0.25f;
        private const float Stagger = 0.06f;
        private const float IdleBetweenPops = 0.05f;
        private const bool Loop = true;

        private Tween driver;

        private void OnEnable()
        {
            if (!tmp) tmp = GetComponent<TMP_Text>();
            StartPop();
        }

        private void OnDisable()
        {
            driver?.Kill();
        }

        private void StartPop()
        {
            driver?.Kill();

            // 전체 시퀀스 길이 = (마지막 글자 시작 시점) + (팝 길이 + 대기)
            tmp.ForceMeshUpdate();
            var count = tmp.textInfo.characterCount;
            if (count == 0) return;

            var total = Mathf.Max(0f, (count - 1) * Stagger) + PopDuration + IdleBetweenPops;

            driver = DOVirtual.Float(0f, total, total, t =>
                {
                    // 1) 현재 텍스트 메쉬 싱크
                    tmp.ForceMeshUpdate();
                    var ti = tmp.textInfo;

                    // 2) 문자별 스케일 적용
                    for (var i = 0; i < ti.characterCount; i++)
                    {
                        var c = ti.characterInfo[i];
                        if (!c.isVisible) continue;

                        var matIndex = c.materialReferenceIndex;
                        var vi = c.vertexIndex;
                        var verts = ti.meshInfo[matIndex].vertices;

                        // 글자 i가 시작되는 시간
                        var startT = i * Stagger;
                        var local = t - startT;

                        // 0~popDuration 동안만 팝 곡선, 그 외엔 1.0
                        var s = 1f;
                        if (local >= 0f && local <= PopDuration)
                        {
                            var p = Mathf.Clamp01(local / PopDuration);

                            // 팝 곡선: 빠르게 커졌다가 부드럽게 복귀 (ease-out punch 느낌)
                            // (원하면 AnimationCurve로 바꿔도 OK)
                            var up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 2f)); // 앞 절반 급상승
                            var down = 1f -
                                       Mathf.SmoothStep(0f, 1f,
                                           Mathf.Clamp01((p - 0.5f) * 2f)); // 뒷 절반 완만 하강
                            var pulse = Mathf.Min(up, down); // 봉우리 1개

                            s = Mathf.Lerp(1f, PopScale, pulse);
                        }

                        // 문자 중심 구해서 스케일
                        var c0 = verts[vi + 0];
                        var c1 = verts[vi + 1];
                        var c2 = verts[vi + 2];
                        var c3 = verts[vi + 3];
                        var center = (c0 + c1 + c2 + c3) * 0.25f;

                        ApplyScaleAround(ref verts[vi + 0], center, s);
                        ApplyScaleAround(ref verts[vi + 1], center, s);
                        ApplyScaleAround(ref verts[vi + 2], center, s);
                        ApplyScaleAround(ref verts[vi + 3], center, s);
                    }

                    // 3) 적용
                    for (var m = 0; m < ti.meshInfo.Length; m++)
                    {
                        var mi = ti.meshInfo[m];
                        mi.mesh.vertices = mi.vertices;
                        tmp.UpdateGeometry(mi.mesh, m);
                    }
                })
                .SetEase(Ease.Linear)
                .SetLoops(Loop ? -1 : 0, LoopType.Restart);
        }

        private void ApplyScaleAround(ref Vector3 v, Vector3 center, float scale)
        {
            v = center + (v - center) * scale;
        }
    }
}