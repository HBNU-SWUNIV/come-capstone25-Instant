using System.Collections;
using Scriptable;
using Unity.Netcode;
using UnityEngine;

namespace Players.Common
{
    public class FxHandler : NetworkBehaviour
    {
        [SerializeField] private ParticleSystem hitVfx;
        [SerializeField] private SfxData hitSfx;

        [SerializeField] private ParticleSystem attackVfx;
        [SerializeField] private SfxData attackSfx;

        [SerializeField] private ParticleSystem stunVfx;

        public void PlayHitFx()
        {
            HitVfx();
            HitSfx();
        }

        public void PlayAttackFx()
        {
            AttackVfx();
            AttackSfx();
        }

        public void PlayStunFx(float time)
        {
            StartCoroutine(StunVfx(time));
            HitSfx();
        }

        private void HitVfx()
        {
            if (!hitVfx) return;

            hitVfx.Play();
        }

        private void HitSfx()
        {
            if (!hitSfx) return;

            AudioManager.Instance.PlaySfx(hitSfx.clip, transform.position, hitSfx.volume, hitSfx.pitch);
        }

        private void AttackVfx()
        {
            if (!attackVfx) return;

            attackVfx.Play();
        }

        private void AttackSfx()
        {
            if (!attackSfx) return;

            AudioManager.Instance.PlaySfx(attackSfx.clip, transform.position, attackSfx.volume, attackSfx.pitch);
        }

        private IEnumerator StunVfx(float time)
        {
            stunVfx.Play();

            yield return new WaitForSeconds(time);

            stunVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}