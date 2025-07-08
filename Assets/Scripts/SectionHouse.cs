// Data structure for resume sections
using System.Collections;

using DG.Tweening;

using UnityEngine;

// Example interactable object
public class SectionHouse : MonoBehaviour, IInteractable
{
    [Header("House Settings")]
    [SerializeField] private Section resumeSection;

    [Header("Visual Effects")]
    public Renderer myRenderer;

    public Transform Ground;
    public Transform LightTarget;

    private Tweener outlineTweener;
    private Tweener colorTweener;

    private void Start()
    {
        if (myRenderer == null) myRenderer = GetComponent<Renderer>();
        myRenderer.material.color = resumeSection.defaultColor;
        myRenderer.material.SetFloat("_OutlineWidth", 0f);
    }

    public string GetInteractionText()
    {
        return $"view {resumeSection.title}";
    }

    public void OnInteract()
    {
        // Add any house-specific interaction effects
        KillColorTweener();
        KillOutlineTweener();

        colorTweener = myRenderer.material.DOColor(resumeSection.themeColor, .5f).SetAutoKill(true).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            colorTweener = null;
        });
        outlineTweener = myRenderer.material.DOFloat(resumeSection.outlineWidth, "_OutlineWidth", .5f).SetAutoKill(true).OnComplete(() =>
        {
            outlineTweener = null;
        });
    }

    public void OnInteractionLost()
    {
        KillColorTweener();
        KillOutlineTweener();
        colorTweener = myRenderer.material.DOColor(resumeSection.defaultColor, .2f).SetEase(Ease.InOutQuad).SetAutoKill(true).OnComplete(() => { colorTweener = null; });
        outlineTweener = myRenderer.material.DOFloat(resumeSection.defaultOutlineWidth, "_OutlineWidth", .2f).SetAutoKill(true).OnComplete(() => { outlineTweener = null; });
    }

    public Section GetResumeSection()
    {
        return resumeSection;
    }

    private void KillOutlineTweener()
    {
        if (outlineTweener != null)
            outlineTweener.Kill();
        outlineTweener = null;
    }

    private void KillColorTweener()
    {
        if (colorTweener != null)
            colorTweener.Kill();
        colorTweener = null;
    }
}

[System.Serializable]
public class Section
{
    public string title;
    public Transform target;
    public int index;

    public Color themeColor = Color.white;
    public Color defaultColor = Color.white;
    public float outlineWidth = 0.14f; // Highlight outline width
    public float defaultOutlineWidth = 0f; // Base outline width
}