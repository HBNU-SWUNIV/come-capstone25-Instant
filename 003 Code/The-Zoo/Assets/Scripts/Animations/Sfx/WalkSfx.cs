using Scriptable;
using UnityEngine;

namespace Animations.Sfx
{
    public class WalkSfx : StateMachineBehaviour
    {
        [SerializeField] internal SfxData sfxData;

        bool playedThisLoop = false;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var nt = stateInfo.normalizedTime;
            if (!playedThisLoop && nt % 0.5f < 0.05f)
            {
                if (!AudioManager.Instance) return;
                AudioManager.Instance.PlaySfx(sfxData.clip, animator.transform.position, sfxData.volume, sfxData.pitch);
                playedThisLoop = true;
            }
            if (nt % 1f > 0.1f)
            {
                playedThisLoop = false;
            }
        }
    }
}