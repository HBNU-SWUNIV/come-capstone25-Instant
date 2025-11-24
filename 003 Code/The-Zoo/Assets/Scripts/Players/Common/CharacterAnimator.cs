using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Players.Common
{
    public class CharacterAnimator : NetworkAnimator
    {
        public static readonly int IdleHash = Animator.StringToHash("Idle");
        public static readonly int MoveHash = Animator.StringToHash("Move");
        public static readonly int RunHash = Animator.StringToHash("Run");
        public static readonly int JumpHash = Animator.StringToHash("Jump");
        public static readonly int SpinHash = Animator.StringToHash("Spin");
        public static readonly int AttackHash = Animator.StringToHash("Attack");
        public static readonly int HitHash = Animator.StringToHash("Hit");
        public static readonly int DeathHash = Animator.StringToHash("Death");

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

        internal void SetBool(int id, bool value)
        {
            Animator.SetBool(id, value);
        }

        internal void OnSpin(bool spin)
        {
            Animator.SetBool(SpinHash, spin);
        }

        internal void OnAttack()
        {
            Animator.SetTrigger(AttackHash);
        }

        internal void OnMove(bool obj)
        {
            Animator.SetBool(MoveHash, obj);
        }

        internal void OnRun(bool run)
        {
            Animator.SetBool(RunHash, run);
        }

        internal void OnJump()
        {
            Animator.SetTrigger(JumpHash);
        }

        internal void OnHit()
        {
            Animator.SetTrigger(HitHash);
            Animator.CrossFade(HitHash, 0.1f);
        }

        internal void OnDeath()
        {
            Animator.SetTrigger(DeathHash);
            Animator.CrossFade(DeathHash, 0.1f);
        }

        internal void OnIdle()
        {
            Animator.SetTrigger(IdleHash);
            Animator.CrossFade(IdleHash, 0.5f);
        }

        internal void Rebind()
        {
            Animator.Rebind();
        }
    }
}