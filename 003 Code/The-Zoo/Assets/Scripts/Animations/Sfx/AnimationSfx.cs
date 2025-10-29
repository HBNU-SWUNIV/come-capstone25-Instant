using Players;
using Scriptable;
using UnityEngine;

namespace Animations.Sfx
{
    public class AnimationSfx : StateMachineBehaviour
    {
        [SerializeField] internal SfxData sfxData;

        public override void OnStateEnter(Animator animator,
            AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!AudioManager.Instance) return;
            AudioManager.Instance.PlaySfx(sfxData.clip, animator.transform.position, sfxData.volume, sfxData.pitch);
        }
    }
}