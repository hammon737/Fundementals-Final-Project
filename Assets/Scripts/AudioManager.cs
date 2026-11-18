using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Hover Sounds")]
    [SerializeField] AudioClip hoverSound;
    [SerializeField] [Range(0f, 1f)] float hoverVolume;

    [Header("Select Sounds")]
    [SerializeField] AudioClip selectSound;
    [SerializeField] [Range(0f, 1f)] float selectVolume;
    static AudioManager instance;

    void Awake()
    {
        ManageSingleton();
    }

    void ManageSingleton()
    {
        if (instance != null)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void PlayHoverSound()
    {
        PlaySound(hoverSound, hoverVolume);
    }
    public void PlaySelectSound()
    {
        PlaySound(selectSound, selectVolume);
    }

    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
        }
        else
        {
            Debug.LogWarning("Audio clip is not assigned.");
        }
    }
}
