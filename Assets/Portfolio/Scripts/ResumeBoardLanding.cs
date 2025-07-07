using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class ResumeBoardLandingDOTween : MonoBehaviour
{
    //[Header("Target")]
    //public Transform targetTransform;

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

    private Vector3 startPos;
    private Vector3 endPos;

    private bool isRead = true;

    public static event Action OnBoardLanded;

    public List<TypeWriterEffect> typewriters;

    public UnityEvent OnBoardLand;

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (!isRead)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                SendBoardBack();
            }
        }
    }

    private void SendBoardBack()
    {
        transform.DOMove(startPos, landingDuration * 1.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            foreach (TypeWriterEffect effect in typewriters)
            {
                effect.StopTypewriter();
            }
            isRead = true;
        });
    }

    private void OnEnable()
    {
        Gate.OnGateExit += OnGateExit;
    }

    private void OnDisable()
    {
        Gate.OnGateExit -= OnGateExit;
    }

    private void OnGateExit()
    {
        if (!isRead) return;
        CreateFullLandingSequence();
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
        isRead = false;
        masterSequence = DOTween.Sequence();
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

        // Rotation during fall
        //masterSequence.Join(transform.DORotate(new Vector3(0, 360, 0), landingDuration, RotateMode.FastBeyond360)
        //    .SetEase(Ease.OutQuart));

        // Shadow scaling
        if (shadowInstance)
        {
            masterSequence.Join(shadowInstance.transform.DOScale(Vector3.one, landingDuration)
                .SetEase(Ease.OutQuart));
        }

        // Landing effects
        masterSequence.OnComplete(() =>
        {
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

        Sequence bounceSequence = DOTween.Sequence();

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
}