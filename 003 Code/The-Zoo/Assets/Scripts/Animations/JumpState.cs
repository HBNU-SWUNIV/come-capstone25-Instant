using Players;
using UnityEngine;

namespace Animations
{
    public class JumpState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator,
            AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (mover != null) mover.CanJump = false;
        }

        public override void OnStateExit(Animator animator,
            AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (mover != null) mover.CanJump = true;
        }
    }
}