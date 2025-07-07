using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class TypeWriterEffect : MonoBehaviour
{
    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopEffect = false;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip typeSound;

    private string fullText;
    private string currentText = "";
    private Coroutine typewriterCoroutine;

    // Component references
    private TextMeshPro uiText;

    private TextMeshProUGUI tmpText;

    public UnityEvent onComplete;

    private void Start()
    {
        // Get text component (supports both UI Text and TextMeshPro)
        uiText = GetComponent<TextMeshPro>();
        tmpText = GetComponent<TextMeshProUGUI>();

        if (uiText != null)
        {
            fullText = uiText.text;
            uiText.text = "";
        }
        else if (tmpText != null)
        {
            fullText = tmpText.text;
            tmpText.text = "";
        }
        else
        {
            Debug.LogError("TypewriterEffect: No Text or TextMeshProUGUI component found!");
            return;
        }

        if (playOnStart)
        {
            StartTypewriter();
        }
    }

    public void StartTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    public void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    public void SetText(string newText)
    {
        fullText = newText;
        currentText = "";

        if (uiText != null)
            uiText.text = "";
        else if (tmpText != null)
            tmpText.text = "";
    }

    public void ClearText()
    {
        if (uiText != null)
            uiText.text = "";
        else if (tmpText != null)
            tmpText.text = "";
    }

    public void SetSpeed(float newSpeed)
    {
        typewriterSpeed = newSpeed;
    }

    public void CompleteText()
    {
        StopTypewriter();
        onComplete?.Invoke();
        currentText = fullText;

        if (uiText != null)
            uiText.text = fullText;
        else if (tmpText != null)
            tmpText.text = fullText;
    }

    private IEnumerator TypewriterCoroutine()
    {
        currentText = "";

        foreach (char letter in fullText)
        {
            currentText += letter;

            // Update the appropriate text component
            if (uiText != null)
                uiText.text = currentText;
            else if (tmpText != null)
                tmpText.text = currentText;

            // Play typing sound if available
            if (audioSource != null && typeSound != null && letter != ' ')
            {
                audioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }
        onComplete?.Invoke();

        // Loop if enabled
        if (loopEffect)
        {
            yield return new WaitForSeconds(1f); // Wait before restarting
            StartTypewriter();
        }
    }

    // Public methods for external control
    public bool IsTyping()
    {
        return typewriterCoroutine != null;
    }

    public float GetProgress()
    {
        if (string.IsNullOrEmpty(fullText))
            return 0f;

        return (float)currentText.Length / fullText.Length;
    }
}