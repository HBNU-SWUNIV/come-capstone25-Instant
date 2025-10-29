using Players;
using Players.Common;
using UnityEngine;

namespace Animations
{
    public class SpinState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (mover != null) mover.SpinHold = true;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (animator.GetBool(CharacterAnimator.SpinHash)) return;
            if (mover != null) mover.SpinHold = false;
        }
    }
}