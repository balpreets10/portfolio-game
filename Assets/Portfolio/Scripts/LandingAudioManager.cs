using UnityEngine;

public class LandingAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip whooshSound;

    public AudioClip impactSound;
    public AudioClip reverb;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayLandingSequence()
    {
        StartCoroutine(AudioSequence());
    }

    private System.Collections.IEnumerator AudioSequence()
    {
        // Whoosh during fall
        audioSource.clip = whooshSound;
        audioSource.Play();

        yield return new WaitForSeconds(1.5f);

        // Impact sound
        audioSource.clip = impactSound;
        audioSource.Play();

        yield return new WaitForSeconds(0.1f);

        // Reverb/echo
        audioSource.clip = reverb;
        audioSource.Play();
    }
}