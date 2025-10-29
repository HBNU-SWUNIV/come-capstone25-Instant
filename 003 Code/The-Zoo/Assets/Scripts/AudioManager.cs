using System.Collections;
using EventHandler;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup efxGroup;

    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip lobbyBGM;
    [SerializeField] private AudioClip inGameBGM;
    [SerializeField] private AudioClip uiSfx;

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource efxSource;

    [SerializeField] private int poolCapacity = 10;
    [SerializeField] private int poolMaxSize = 20;

    private ObjectPool<AudioSource> sfxPool;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSfxPool();

            efxSource.outputAudioMixerGroup = efxGroup;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GamePlayEventHandler.UIChanged += OnUIChanged;

        ApplySavedVolumesOnBoot();
    }

    private void OnDisable()
    {
        GamePlayEventHandler.UIChanged -= OnUIChanged;
    }

    private void OnUIChanged(string obj)
    {
        switch (obj)
        {
            case "Title":
                PlayBGM(titleBGM);
                if (Camera.main != null) Camera.main.GetComponent<AudioListener>().enabled = true;
                break;
            case "Lobby":
                PlayBGM(lobbyBGM);
                if (Camera.main != null) Camera.main.GetComponent<AudioListener>().enabled = false;
                break;
            case "InGame":
                PlayBGM(inGameBGM);
                if (Camera.main != null) Camera.main.GetComponent<AudioListener>().enabled = false;
                break;
        }
    }

    private void ApplySavedVolumesOnBoot()
    {
        var bgm = PlayerPrefs.GetFloat("opt_bgm", 0.75f);
        var sfx = PlayerPrefs.GetFloat("opt_sfx", 0.75f);
        var efx = PlayerPrefs.GetFloat("opt_efx", 0.75f);

        SetBGMVolume(bgm);
        SetSfxVolume(sfx);
        SetEfxVolume(efx);
    }

    public void SetBGMVolume(float linear)
    {
        var db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        mixer.SetFloat("BGM", db);
    }

    public void SetSfxVolume(float linear)
    {
        var db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        mixer.SetFloat("SFX", db);
    }

    public void SetEfxVolume(float linear)
    {
        var db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        mixer.SetFloat("EFX", db);
    }

    #region SFX Pool 초기화

    private void InitSfxPool()
    {
        sfxPool = new ObjectPool<AudioSource>(
            () =>
            {
                var go = new GameObject("PooledSFX");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.outputAudioMixerGroup = sfxGroup;
                src.spatialBlend = 1f;
                src.minDistance = 1f;
                src.maxDistance = 8f;
                src.dopplerLevel = 1f;
                src.spread = 360f;
                src.rolloffMode = AudioRolloffMode.Linear;
                go.SetActive(false);
                return src;
            },
            src => src.gameObject.SetActive(true),
            src =>
            {
                src.Stop();
                src.clip = null;
                src.gameObject.SetActive(false);
            },
            src => Destroy(src.gameObject),
            false,
            poolCapacity,
            poolMaxSize
        );
    }

    #endregion

    #region BGM API

    public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.outputAudioMixerGroup = bgmGroup;
        bgmSource.volume = volume;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    #endregion

    #region SFX API

    /// <summary>
    ///     위치 기반으로 효과음 재생
    /// </summary>
    public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var src = sfxPool.Get();
        src.transform.position = position;
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.Play();
        StartCoroutine(ReleaseWhenDone(src));
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        efxSource.PlayOneShot(clip, volume);
    }

    public void PlayUISfx()
    {
        PlayOneShot(uiSfx);
    }

    private IEnumerator ReleaseWhenDone(AudioSource src)
    {
        yield return new WaitUntil(() => !src.isPlaying);
        sfxPool.Release(src);
    }

    #endregion
}