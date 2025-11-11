using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("UI AUDIO CLIPS")]
    public AudioSource buttonAudioSource;
    public AudioClip positiveButtonSound;
    public AudioClip negativeButtonSound;


    void Start()
    {
        
    }

    void Update()
    {
        
    }


    public void positiveButtonSoundPlay()
    {
        buttonAudioSource.PlayOneShot(positiveButtonSound);
    }
    public void negativeButtonSoundPlay()
    {
        buttonAudioSource.PlayOneShot(negativeButtonSound);
    }
}
