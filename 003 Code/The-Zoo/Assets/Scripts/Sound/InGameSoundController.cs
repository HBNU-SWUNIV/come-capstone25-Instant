using UnityEngine;

public class InGameSoundController : MonoBehaviour
{
    AudioSource InGameBackgroundMusic;

    void Start()
    {
        InGameBackgroundMusic = GetComponent<AudioSource>();

        InGameBackgroundMusic.Play();
    }
}
