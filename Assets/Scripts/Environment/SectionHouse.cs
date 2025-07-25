using System;
using System.Collections;

using DG.Tweening;

using UnityEngine;

// Updated SectionHouse that implements IInteractableWithSection
public class SectionHouse : MonoBehaviour, IInteractableWithSection
{
    public InteractableState State { get; set; }

    [Header("House Settings")]
    [SerializeField] private Section resumeSection;

    [Header("Visual Effects")]
    public Renderer myRenderer;

    public Transform Ground;
    public Transform LightTarget;
    public Transform effectTextTarget;

    private Tweener outlineTweener;
    private Tweener colorTweener;

    public static event Action<Section> OnSectionHouseInteracted;

    public string InteractionText;

    private void Start()
    {
        if (myRenderer == null) myRenderer = GetComponent<Renderer>();
        myRenderer.material.color = resumeSection.defaultColor;
        myRenderer.material.SetFloat("_OutlineWidth", 0f);
        State = InteractableState.NonInteractable;
    }

    private void OnEnable()
    {
        ActionButton.OnActionPressed += OnAction;
    }

    private void OnDisable()
    {
        ActionButton.OnActionPressed -= OnAction;
    }

    #region IInteractableWithSection Implementation

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
            OnAction();
    }

    private void OnAction()
    {
        if (State == InteractableState.Interactable && resumeSection.targetSection != null)
        {
            // Optionally, you can add logic to update the position or state of the house
            // based on the target section's position or other properties.
            resumeSection.targetSection.Activate();
            OnSectionHouseInteracted?.Invoke(resumeSection);
        }
    }

    public string GetInteractionText()
    {
        return InteractionText;
    }

    public void OnInteract()
    {
        State = InteractableState.Interactable;
        // Add any house-specific interaction effects
        KillColorTweener();
        KillOutlineTweener();

        colorTweener = myRenderer.material.DOColor(resumeSection.themeColor, .5f)
            .SetAutoKill(true)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                colorTweener = null;
            });

        outlineTweener = myRenderer.material.DOFloat(resumeSection.outlineWidth, "_OutlineWidth", .5f)
            .SetAutoKill(true)
            .OnComplete(() =>
            {
                outlineTweener = null;
            });
    }

    public void OnInteractionLost()
    {
        State = InteractableState.NonInteractable;
        KillColorTweener();
        KillOutlineTweener();

        colorTweener = myRenderer.material.DOColor(resumeSection.defaultColor, .2f)
            .SetEase(Ease.InOutQuad)
            .SetAutoKill(true)
            .OnComplete(() =>
            {
                colorTweener = null;
            });

        outlineTweener = myRenderer.material.DOFloat(resumeSection.defaultOutlineWidth, "_OutlineWidth", .2f)
            .SetAutoKill(true)
            .OnComplete(() =>
            {
                outlineTweener = null;
            });
    }

    public Section GetResumeSection()
    {
        return resumeSection;
    }

    #endregion IInteractableWithSection Implementation

    #region Private Methods

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

    #endregion Private Methods

    #region Public Methods

    // Public method to manually trigger the interaction (for testing or other systems)
    public void ManualTriggerInteraction()
    {
        OnInteract();
    }

    // Public method to manually trigger interaction lost (for testing or other systems)
    public void ManualTriggerInteractionLost()
    {
        OnInteractionLost();
    }

    // Method to get the current color state
    public Color GetCurrentColor()
    {
        return myRenderer.material.color;
    }

    // Method to get the current outline width
    public float GetCurrentOutlineWidth()
    {
        return myRenderer.material.GetFloat("_OutlineWidth");
    }

    // Method to check if the house is currently in an interacted state
    public bool IsInteracting()
    {
        return colorTweener != null || outlineTweener != null ||
               myRenderer.material.color == resumeSection.themeColor;
    }

    #endregion Public Methods

    private void OnDestroy()
    {
        // Clean up tweeners to prevent memory leaks
        KillColorTweener();
        KillOutlineTweener();
    }
}

[System.Serializable]
public class Section
{
    public string title;
    public SectionDetails targetSection;
    public int index;

    public Color themeColor = Color.white;
    public Color defaultColor = Color.white;
    public float outlineWidth = 0.14f; // Highlight outline width
    public float defaultOutlineWidth = 0f; // Base outline width
}