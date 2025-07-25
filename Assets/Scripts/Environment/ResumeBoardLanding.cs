using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class ResumeBoardLandingDOTween : MonoBehaviour, IInteractable
{
    public InteractableState State { get; set; }

    [Header("Landing Animation")]
    public float fallHeight = 25f;

    public float landingDuration = 2.5f;
    public float anticipationDelay = 1f;
    public GameObject shadowPrefab;

    private GameObject shadowInstance;
    private Sequence masterSequence;

    [Header("Effects")]
    public ParticleSystem dustParticles;

    public ParticleSystem impactParticles;
    public AudioSource landingSound;

    [Header("Interaction")]
    [SerializeField] private string interactionText = "Send board back";

    public GameObject objects;

    private Vector3 startPos;
    private Vector3 endPos;

    private bool isLanded = false;

    public static event Action OnBoardLanded;

    public static event Action OnBoardSentBack;

    public List<TypeWriterEffect> typewriters;
    public UnityEvent OnBoardLand;

    private void Start()
    {
        Init();
        objects.SetActive(false);
    }

    private void OnEnable()
    {
        Gate.OnGateExit += OnGateExit;
        RaycastManager.OnInteractableWithSectionHit += OnInteractableWithSectionHit;
        ActionButton.OnActionPressed += OnAction;
    }

    private void OnDisable()
    {
        Gate.OnGateExit -= OnGateExit;
        RaycastManager.OnInteractableWithSectionHit -= OnInteractableWithSectionHit;
        ActionButton.OnActionPressed -= OnAction;
    }

    private void OnInteractableWithSectionHit(IInteractableWithSection interactableWithSection)
    {
        // If we're not read and landed, and user hits a section house, send board back
        if (isLanded)
        {
            SendBoardBack();
        }
    }

    private void OnGateExit()
    {
        if (isLanded) return;
        CreateFullLandingSequence();
    }

    #region IInteractable Implementation

    public string GetInteractionText()
    {
        return interactionText;
    }

    public void OnInteract()
    {
        // This is called when the raycast hits this object
        State = InteractableState.Interactable;
    }

    public void OnInteractionLost()
    {
        // This is called when the raycast stops hitting this object
        State = InteractableState.NonInteractable;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
            OnAction();
    }

    private void OnAction()
    {
        if (State == InteractableState.Interactable)
        {
            SendBoardBack();
            State = InteractableState.NonInteractable; // Reset state after interaction
        }
    }

    #endregion IInteractable Implementation

    private void SendBoardBack()
    {
        transform.DOMove(startPos, landingDuration * 1.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            foreach (TypeWriterEffect effect in typewriters)
            {
                effect.StopTypewriter();
            }
            isLanded = false;
            OnBoardSentBack?.Invoke();
            objects.SetActive(false);
        });
    }

    private void Init()
    {
        startPos = transform.position + Vector3.up * fallHeight;
        endPos = transform.position;
        transform.position = startPos;
        dustParticles?.Stop();
    }

    private void CreateFullLandingSequence()
    {
        masterSequence = DOTween.Sequence();
        objects.SetActive(true);
        StartCoroutine(ClearTexts());

        // Create shadow
        if (shadowPrefab)
        {
            shadowInstance = Instantiate(shadowPrefab, new Vector3(transform.position.x, 0.1f, transform.position.z), Quaternion.identity);
            shadowInstance.transform.localScale = Vector3.one * 0.2f;
        }

        // Anticipation delay
        masterSequence.AppendInterval(anticipationDelay);

        // Set start position
        masterSequence.AppendCallback(() => transform.position = startPos);

        // Landing animation
        masterSequence.Append(transform.DOMove(endPos, landingDuration)
            .SetEase(Ease.InCubic));

        // Shadow scaling
        if (shadowInstance)
        {
            masterSequence.Join(shadowInstance.transform.DOScale(Vector3.one, landingDuration)
                .SetEase(Ease.OutQuart));
        }

        // Landing effects
        masterSequence.OnComplete(() =>
        {
            isLanded = true;
            OnBoardLanded?.Invoke();
            OnBoardLand?.Invoke();
            if (shadowInstance) Destroy(shadowInstance);
            TriggerLandingEffects();
        });
    }

    private IEnumerator ClearTexts()
    {
        foreach (TypeWriterEffect effect in typewriters)
        {
            effect.ClearText();
            yield return null;
        }
    }

    private void TriggerLandingEffects()
    {
        dustParticles?.Play();
        // Screen shake
        Camera.main.DOShakePosition(0.5f, 0.3f, 20);
        Camera.main.DOShakeRotation(0.3f, 1f, 20);

        // Particles
        dustParticles?.Play();
        impactParticles?.Play();

        // Sound
        landingSound?.Play();

        // Complex bounce sequence
        CreateComplexBounceEffect();
    }

    private void CreateComplexBounceEffect()
    {
        Vector3 originalScale = transform.localScale;

        DG.Tweening.Sequence bounceSequence = DOTween.Sequence();

        // Initial squash
        bounceSequence.Append(transform.DOScale(
            new Vector3(originalScale.x * 1.3f, originalScale.y * 0.6f, originalScale.z * 1.3f),
            0.08f));

        // First bounce
        bounceSequence.Append(transform.DOScale(
            new Vector3(originalScale.x * 0.9f, originalScale.y * 1.1f, originalScale.z * 0.9f),
            0.15f));

        // Second bounce
        bounceSequence.Append(transform.DOScale(
            new Vector3(originalScale.x * 1.05f, originalScale.y * 0.95f, originalScale.z * 1.05f),
            0.1f));

        // Settle to original
        bounceSequence.Append(transform.DOScale(originalScale, 0.2f)
            .SetEase(Ease.OutBounce));
    }

    private void OnDestroy()
    {
        masterSequence?.Kill();
    }

    // Public method to manually trigger the send back action
    public void ManualSendBoardBack()
    {
        SendBoardBack();
    }
}