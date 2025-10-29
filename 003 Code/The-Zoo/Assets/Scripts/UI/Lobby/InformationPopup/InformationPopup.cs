using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI.Lobby.InformationPopup
{
    public class InformationPopup : MonoBehaviour
    {
        public static InformationPopup instance;
        [SerializeField] private GameObject informationPopupPrefab;

        private IObjectPool<GameObject> pool;

        private void Awake()
        {
            if (!instance) instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            pool = new ObjectPool<GameObject>(
                CreatePoolObj,
                GetPoolObj,
                ReleasePoolObj,
                DestroyPoolObj,
                true, 3, 5
            );
        }

        public void ShowPopup(string massage)
        {
            var item = pool.Get();

            var msg = item.GetComponentInChildren<TextMeshProUGUI>();
            var btn = item.GetComponentInChildren<Button>();

            msg.text = massage;

            btn.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            btn.onClick.AddListener(HidePopup(item));

            item.SetActive(true);
        }

        private UnityAction HidePopup(GameObject item)
        {
            return () => { pool.Release(item); };
        }

        private GameObject CreatePoolObj()
        {
            return Instantiate(informationPopupPrefab, transform);
        }

        private void GetPoolObj(GameObject obj)
        {
            obj.SetActive(true);
        }

        private void ReleasePoolObj(GameObject obj)
        {
            obj.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            obj.SetActive(false);
        }

        private void DestroyPoolObj(GameObject obj)
        {
            Destroy(obj);
        }
    }
}