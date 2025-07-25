using System;

using UnityEngine;

public class SectionDetails : MonoBehaviour, IInteractable
{
    public static event Action OnSectionDetailsInteracted;

    public Transform landingPosition;

    public InteractableState State { get; set; }

    private void Start()
    {
        State = InteractableState.NonInteractable;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ActionButton.OnActionPressed += OnAction;
    }

    private void OnDisable()
    {
        ActionButton.OnActionPressed -= OnAction;
    }

    public string GetInteractionText()
    {
        return "to go Back";
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            OnAction();
        }
    }

    private void OnAction()
    {
        if (State == InteractableState.Interactable)
        {
            OnSectionDetailsInteracted?.Invoke();
            State = InteractableState.NonInteractable;
            gameObject.SetActive(false);
        }
    }

    public void OnInteract()
    {
        State = InteractableState.Interactable;
    }

    public void OnInteractionLost()
    {
        State = InteractableState.NonInteractable;
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }
}