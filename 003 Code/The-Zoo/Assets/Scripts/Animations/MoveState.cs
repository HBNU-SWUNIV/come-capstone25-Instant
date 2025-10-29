using Players;
using UnityEngine;

namespace Animations
{
    public class MoveState : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (mover != null) mover.CanMove = false;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var mover = animator.GetComponentInParent<IMovable>();
            if (mover != null) mover.CanMove = true;
        }
    }
}