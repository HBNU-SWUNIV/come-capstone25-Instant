using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Animations.Vfx
{
    public class VfxManager : MonoBehaviour
    {
        public static VfxManager Instance { get; private set; }

        [SerializeField] private GameObject walkDustPrefab;

        private IObjectPool<ParticleSystem> vfxPool;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                vfxPool = new ObjectPool<ParticleSystem>(
                    CreatePooledVfx,
                    OnGet,
                    OnRelease,
                    OnDestroyVfx,
                    collectionCheck: false,
                    defaultCapacity: 25,
                    maxSize: 50
                );
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private ParticleSystem CreatePooledVfx()
        {
            var obj = Instantiate(walkDustPrefab, transform);
            var ps = obj.GetComponent<ParticleSystem>();
            obj.SetActive(false);
            return ps;
        }

        private void OnGet(ParticleSystem ps)
        {
            if (!ps) return;
            ps.gameObject.SetActive(true);
        }

        private void OnRelease(ParticleSystem ps)
        {
            if (!ps) return;
            ps.gameObject.SetActive(false);
        }

        private void OnDestroyVfx(ParticleSystem ps)
        {
            Destroy(ps.gameObject);
        }

        public void PlayVfx(Vector3 position)
        {
            var ps = vfxPool.Get();

            ps.transform.position = position;

            ps.Clear();
            ps.Play();

            StartCoroutine(ReleaseWhenDone(ps));
        }

        private IEnumerator ReleaseWhenDone(ParticleSystem ps)
        {
            yield return new WaitUntil(() => !ps.IsAlive(false));
            vfxPool.Release(ps);
        }
    }
}