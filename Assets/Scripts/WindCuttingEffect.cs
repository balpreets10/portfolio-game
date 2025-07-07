using UnityEngine;
using DG.Tweening;

public class WindCuttingEffect : MonoBehaviour
{
    [Header("Wind Trail Settings")]
    [SerializeField] private ParticleSystem windTrailEffect;
    [SerializeField] private ParticleSystem windBurstEffect;
    [SerializeField] private LineRenderer[] windLines;
    [SerializeField] private int windLineCount = 8;
    [SerializeField] private float windLineLength = 3f;
    [SerializeField] private float windLineSpeed = 2f;
    [SerializeField] private Material windLineMaterial;

    [Header("Wind Distortion")]
    [SerializeField] private Transform windDistortionObject;
    [SerializeField] private float distortionRadius = 2f;
    [SerializeField] private float distortionIntensity = 0.5f;
    [SerializeField] private AnimationCurve distortionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource windAudioSource;
    [SerializeField] private AudioClip windCuttingSound;
    [SerializeField] private float windSoundFadeTime = 0.5f;

    [Header("Shader Effects")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private string windSpeedProperty = "_WindSpeed";
    [SerializeField] private string windIntensityProperty = "_WindIntensity";
    [SerializeField] private float maxWindSpeed = 2f;
    [SerializeField] private float maxWindIntensity = 1f;

    [Header("Visual Effects")]
    [SerializeField] private Transform[] windSliceObjects;
    [SerializeField] private float sliceRotationSpeed = 360f;
    [SerializeField] private float sliceScaleMultiplier = 1.5f;

    private Material[] originalMaterials;
    private Material[] windMaterials;
    private bool isEffectActive = false;
    private Sequence windEffectSequence;

    private void Start()
    {
        InitializeWindLines();
        InitializeMaterials();
    }

    private void InitializeWindLines()
    {
        if (windLines == null || windLines.Length == 0)
        {
            windLines = new LineRenderer[windLineCount];
            for (int i = 0; i < windLineCount; i++)
            {
                GameObject lineObj = new GameObject($"WindLine_{i}");
                lineObj.transform.parent = transform;

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.material = windLineMaterial;
                lr.startWidth = 0.1f;
                lr.endWidth = 0.05f;
                lr.positionCount = 2;
                lr.enabled = false;

                windLines[i] = lr;
            }
        }
    }

    private void InitializeMaterials()
    {
        if (playerRenderer != null)
        {
            originalMaterials = playerRenderer.materials;
            windMaterials = new Material[originalMaterials.Length];

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                windMaterials[i] = new Material(originalMaterials[i]);
            }
        }
    }

    public void StartWindCuttingEffect()
    {
        if (isEffectActive) return;

        isEffectActive = true;

        // Start particle effects
        if (windTrailEffect != null)
        {
            windTrailEffect.Play();
        }

        if (windBurstEffect != null)
        {
            windBurstEffect.Play();
        }

        // Start wind lines
        StartWindLines();

        // Start audio
        StartWindAudio();

        // Start shader effects
        StartShaderEffects();

        // Start visual effects
        StartVisualEffects();

        // Start distortion effect
        StartDistortionEffect();
    }

    public void StopWindCuttingEffect()
    {
        if (!isEffectActive) return;

        isEffectActive = false;

        // Stop particle effects
        if (windTrailEffect != null)
        {
            windTrailEffect.Stop();
        }

        if (windBurstEffect != null)
        {
            windBurstEffect.Stop();
        }

        // Stop wind lines
        StopWindLines();

        // Stop audio
        StopWindAudio();

        // Stop shader effects
        StopShaderEffects();

        // Stop visual effects
        StopVisualEffects();

        // Stop distortion effect
        StopDistortionEffect();
    }

    private void StartWindLines()
    {
        foreach (LineRenderer line in windLines)
        {
            line.enabled = true;
            StartCoroutine(AnimateWindLine(line));
        }
    }

    private void StopWindLines()
    {
        foreach (LineRenderer line in windLines)
        {
            line.enabled = false;
        }
    }

    private System.Collections.IEnumerator AnimateWindLine(LineRenderer line)
    {
        while (isEffectActive)
        {
            // Random position around the player
            Vector3 randomOffset = Random.insideUnitSphere * distortionRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // Keep lines above/around player

            Vector3 startPos = transform.position + randomOffset;
            Vector3 endPos = startPos - transform.up * windLineLength;

            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);

            // Animate line movement
            float alpha = 1f;
            float duration = 1f / windLineSpeed;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                alpha = 1f - (t / duration);
                Color color = line.material.color;
                color.a = alpha;
                line.material.color = color;

                // Move line backwards
                Vector3 velocity = -transform.up * windLineSpeed * Time.deltaTime;
                line.SetPosition(0, line.GetPosition(0) + velocity);
                line.SetPosition(1, line.GetPosition(1) + velocity);

                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }

    private void StartWindAudio()
    {
        if (windAudioSource != null && windCuttingSound != null)
        {
            windAudioSource.clip = windCuttingSound;
            windAudioSource.loop = true;
            windAudioSource.volume = 0f;
            windAudioSource.Play();

            windAudioSource.DOFade(1f, windSoundFadeTime);
        }
    }

    private void StopWindAudio()
    {
        if (windAudioSource != null)
        {
            windAudioSource.DOFade(0f, windSoundFadeTime).OnComplete(() =>
            {
                windAudioSource.Stop();
            });
        }
    }

    private void StartShaderEffects()
    {
        if (playerRenderer != null && windMaterials != null)
        {
            playerRenderer.materials = windMaterials;

            foreach (Material mat in windMaterials)
            {
                if (mat.HasProperty(windSpeedProperty))
                {
                    DOTween.To(() => mat.GetFloat(windSpeedProperty),
                              x => mat.SetFloat(windSpeedProperty, x),
                              maxWindSpeed, 0.5f);
                }

                if (mat.HasProperty(windIntensityProperty))
                {
                    DOTween.To(() => mat.GetFloat(windIntensityProperty),
                              x => mat.SetFloat(windIntensityProperty, x),
                              maxWindIntensity, 0.5f);
                }
            }
        }
    }

    private void StopShaderEffects()
    {
        if (playerRenderer != null)
        {
            if (windMaterials != null)
            {
                foreach (Material mat in windMaterials)
                {
                    if (mat.HasProperty(windSpeedProperty))
                    {
                        DOTween.To(() => mat.GetFloat(windSpeedProperty),
                                  x => mat.SetFloat(windSpeedProperty, x),
                                  0f, 0.5f);
                    }

                    if (mat.HasProperty(windIntensityProperty))
                    {
                        DOTween.To(() => mat.GetFloat(windIntensityProperty),
                                  x => mat.SetFloat(windIntensityProperty, x),
                                  0f, 0.5f);
                    }
                }
            }

            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (originalMaterials != null)
                {
                    playerRenderer.materials = originalMaterials;
                }
            });
        }
    }

    private void StartVisualEffects()
    {
        if (windSliceObjects != null)
        {
            foreach (Transform slice in windSliceObjects)
            {
                if (slice != null)
                {
                    slice.gameObject.SetActive(true);

                    // Rotation animation
                    slice.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.LocalAxisAdd)
                         .SetLoops(-1, LoopType.Incremental)
                         .SetEase(Ease.Linear);

                    // Scale animation
                    slice.DOScale(slice.localScale * sliceScaleMultiplier, 0.5f)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetEase(Ease.InOutSine);
                }
            }
        }
    }

    private void StopVisualEffects()
    {
        if (windSliceObjects != null)
        {
            foreach (Transform slice in windSliceObjects)
            {
                if (slice != null)
                {
                    slice.DOKill();
                    slice.gameObject.SetActive(false);
                }
            }
        }
    }

    private void StartDistortionEffect()
    {
        if (windDistortionObject != null)
        {
            windDistortionObject.gameObject.SetActive(true);

            windEffectSequence = DOTween.Sequence();

            windEffectSequence.Append(
                windDistortionObject.DOScale(Vector3.one * distortionRadius, 0.5f)
                    .SetEase(distortionCurve)
            );

            windEffectSequence.Join(
                DOTween.To(() => 0f, x => SetDistortionIntensity(x), distortionIntensity, 0.5f)
            );

            windEffectSequence.SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StopDistortionEffect()
    {
        if (windEffectSequence != null)
        {
            windEffectSequence.Kill();
        }

        if (windDistortionObject != null)
        {
            windDistortionObject.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
            {
                windDistortionObject.gameObject.SetActive(false);
            });
        }
    }

    private void SetDistortionIntensity(float intensity)
    {
        // This would be used with a shader that has distortion properties
        // You can implement this based on your specific distortion shader
        if (windDistortionObject != null)
        {
            Renderer renderer = windDistortionObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material.HasProperty("_DistortionIntensity"))
            {
                renderer.material.SetFloat("_DistortionIntensity", intensity);
            }
        }
    }

    public void UpdateEffectIntensity(float intensity)
    {
        // Update effect intensity based on jump progress
        intensity = Mathf.Clamp01(intensity);

        if (windTrailEffect != null)
        {
            var emission = windTrailEffect.emission;
            emission.rateOverTime = 50f * intensity;
        }

        if (windAudioSource != null)
        {
            windAudioSource.pitch = 0.8f + (0.4f * intensity);
        }
    }

    private void OnDestroy()
    {
        StopWindCuttingEffect();

        if (windMaterials != null)
        {
            foreach (Material mat in windMaterials)
            {
                if (mat != null)
                {
                    DestroyImmediate(mat);
                }
            }
        }
    }
}