using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioSource))]
public class AudioSourceContextMenu : Editor
{
    [MenuItem("CONTEXT/AudioSource/Play One Shot")]
    private static void PlayOneShot(MenuCommand command)
    {
        AudioSource audioSource = (AudioSource)command.context;

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource is null");
            return;
        }

        if (audioSource.clip == null)
        {
            Debug.LogWarning("AudioSource has no clip assigned");
            return;
        }

        // Stop any currently playing audio from this source
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Play the clip as a one-shot
        audioSource.PlayOneShot(audioSource.clip);

        Debug.Log($"Playing one-shot audio: {audioSource.clip.name}");
    }

    [MenuItem("CONTEXT/AudioSource/Play One Shot", true)]
    private static bool ValidatePlayOneShot(MenuCommand command)
    {
        AudioSource audioSource = (AudioSource)command.context;

        // Only show the menu item if there's a clip assigned
        return audioSource != null && audioSource.clip != null;
    }

    // Optional: Add a "Stop Audio" context menu as well
    [MenuItem("CONTEXT/AudioSource/Stop Audio")]
    private static void StopAudio(MenuCommand command)
    {
        AudioSource audioSource = (AudioSource)command.context;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Stopped audio playback");
        }
    }

    [MenuItem("CONTEXT/AudioSource/Stop Audio", true)]
    private static bool ValidateStopAudio(MenuCommand command)
    {
        AudioSource audioSource = (AudioSource)command.context;

        // Only show if audio is currently playing
        return audioSource != null && audioSource.isPlaying;
    }
}