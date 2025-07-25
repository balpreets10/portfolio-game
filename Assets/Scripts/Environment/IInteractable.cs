public interface IInteractable
{
    InteractableState State { get; set; }

    string GetInteractionText();

    void OnInteract();

    void OnInteractionLost();
}

public interface IInteractableWithSection : IInteractable
{
    Section GetResumeSection();
}

public enum InteractableState
{
    Interactable,
    NonInteractable
}