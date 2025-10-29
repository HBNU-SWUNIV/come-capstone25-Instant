using UnityEngine;
using UnityEngine.UI;

public class LobbySoundController : MonoBehaviour
{
    AudioSource LobbyBackgroundMusic;

    void Start()
    {
        LobbyBackgroundMusic = GetComponent<AudioSource>();

        LobbyBackgroundMusic.Play();
    }
}
