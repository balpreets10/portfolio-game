// Data structure for resume sections
using System.Collections;

using UnityEngine;

[System.Serializable]
public class Section
{
    public string title;
    public Transform target;

    [TextArea(5, 10)]
    public string content;

    public Sprite icon;
    public Color themeColor = Color.white;
}

// Example interactable object
public class SectionHouse : MonoBehaviour, IInteractable
{
    [Header("House Settings")]
    public string houseName = "Experience";

    public Section resumeSection;

    [Header("Visual Effects")]
    public GameObject highlightEffect;

    public Light houseLight;

    public string GetInteractionText()
    {
        return $"view {houseName}";
    }

    public void OnInteract()
    {
        // Add any house-specific interaction effects
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
            StartCoroutine(DisableHighlightAfterDelay(2f));
        }

        if (houseLight != null)
        {
            houseLight.intensity = Mathf.Min(houseLight.intensity * 1.5f, 3f);
        }
    }

    public Section GetResumeSection()
    {
        return resumeSection;
    }

    private IEnumerator DisableHighlightAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Optional: Add ambient effects when player approaches
            if (houseLight != null)
            {
                houseLight.intensity = 1.2f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (houseLight != null)
            {
                houseLight.intensity = 0.8f;
            }
        }
    }
}