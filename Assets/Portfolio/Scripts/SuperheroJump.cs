using System.Collections;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CharacterController))]
public class SuperheroJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 15f;

    [SerializeField] private float jumpUpDuration = 1f;
    [SerializeField] private float jumpDownDuration = 1.5f;
    [SerializeField] private float maxJumpDistance = 20f;

    [Header("Landing Settings")]
    [SerializeField] private float landingImpactRadius = 5f;

    [SerializeField] private float landingAnimationDuration = 2f;
    [SerializeField] private LayerMask groundMask = 1;
    [SerializeField] private float groundCheckDistance = 50f;

    [Header("Movement Control")]
    [SerializeField] private float rotationSpeed = 5f;

    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve landingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation Parameters")]
    [SerializeField] private string jumpTrigger = "Jump";

    [SerializeField] private string landingTrigger = "Landing";
    [SerializeField] private string isInAirBool = "IsInAir";

    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect;

    [SerializeField] private ParticleSystem landingEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [SerializeField] private float cameraShakeDuration = 0.3f;

    [Header("Wind Cutting Effect")]
    [SerializeField] private WindCuttingEffect windCuttingEffect;

    [SerializeField] private bool enableWindEffect = true;

    private CharacterController controller;
    private MovementInput movementInput;
    private Animator animator;
    private Camera playerCamera;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private bool isJumping = false;
    private bool isLanding = false;
    private Sequence jumpSequence;

    // Input - FIXED: Set requireDoubleClick to false for single press
    [Header("Input")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [SerializeField] private bool requireDoubleClick = false; // Changed from true to false
    [SerializeField] private float doubleClickTime = 0.3f;
    private float lastClickTime = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        movementInput = GetComponent<MovementInput>();
        animator = GetComponent<Animator>();
        playerCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // FIXED: Ensure wind effects are stopped at start
        if (windCuttingEffect != null)
        {
            windCuttingEffect.StopWindCuttingEffect();
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (isJumping || isLanding) return;

        if (Input.GetKeyDown(jumpKey))
        {
            if (requireDoubleClick)
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                if (timeSinceLastClick <= doubleClickTime)
                {
                    InitiateSuperheroJump();
                }
                lastClickTime = Time.time;
            }
            else
            {
                // FIXED: Direct jump for single press
                InitiateSuperheroJump();
            }
        }
    }

    private void InitiateSuperheroJump()
    {
        if (!CanJump()) return;

        Vector3 targetPosition = CalculateJumpTarget();
        if (targetPosition != Vector3.zero)
        {
            StartSuperheroJump(targetPosition);
        }
    }

    private bool CanJump()
    {
        return controller.isGrounded && !isJumping && !isLanding;
    }

    private Vector3 CalculateJumpTarget()
    {
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 potentialTarget = transform.position + cameraForward * maxJumpDistance;

        // Raycast to find ground
        RaycastHit hit;
        Vector3 rayStart = potentialTarget + Vector3.up * groundCheckDistance;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance * 2f, groundMask))
        {
            return hit.point;
        }

        // If no ground found, use default position
        return potentialTarget;
    }

    private void StartSuperheroJump(Vector3 targetPosition)
    {
        isJumping = true;
        jumpStartPosition = transform.position;
        jumpTargetPosition = targetPosition;

        // Disable movement input during jump
        if (movementInput != null)
            movementInput.enabled = false;

        // Rotate towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.DORotateQuaternion(Quaternion.LookRotation(direction), 0.5f);
        }

        // Play jump animation and sound
        if (animator != null)
        {
            animator.SetTrigger(jumpTrigger);
            animator.SetBool(isInAirBool, true);
        }

        PlaySound(jumpSound);
        PlayEffect(jumpEffect);

        // Start jump sequence
        ExecuteJumpSequence();
    }

    private void ExecuteJumpSequence()
    {
        jumpSequence = DOTween.Sequence();

        // Calculate arc positions
        Vector3 midPoint = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, 0.5f);
        midPoint.y = Mathf.Max(jumpStartPosition.y, jumpTargetPosition.y) + jumpHeight;

        // FIXED: Start wind effect only when jumping starts
        if (enableWindEffect && windCuttingEffect != null)
        {
            windCuttingEffect.StartWindCuttingEffect();
        }

        // Create jump arc using DOTween
        jumpSequence.Append(
            transform.DOMove(midPoint, jumpUpDuration)
                .SetEase(jumpCurve)
                .OnUpdate(() =>
                {
                    // Update wind effect intensity based on upward progress
                    if (windCuttingEffect != null)
                    {
                        float progress = jumpSequence.ElapsedPercentage();
                        if (progress <= 0.5f) // Only during upward phase
                        {
                            windCuttingEffect.UpdateEffectIntensity(progress * 2f);
                        }
                    }
                })
        );

        jumpSequence.Append(
            transform.DOMove(jumpTargetPosition, jumpDownDuration)
                .SetEase(landingCurve)
                .OnStart(() =>
                {
                    // Stop wind effect when starting downward phase
                    if (windCuttingEffect != null)
                    {
                        windCuttingEffect.StopWindCuttingEffect();
                    }
                })
        );

        jumpSequence.OnComplete(() =>
        {
            ExecuteLanding();
        });
    }

    private void ExecuteLanding()
    {
        isJumping = false;
        isLanding = true;

        // Play landing animation and effects
        if (animator != null)
        {
            animator.SetTrigger(landingTrigger);
            animator.SetBool(isInAirBool, false);
        }

        PlaySound(landingSound);
        PlayEffect(landingEffect);

        // Camera shake
        if (playerCamera != null)
        {
            playerCamera.transform.DOShakePosition(cameraShakeDuration, cameraShakeIntensity);
        }

        // Landing pose duration
        DOVirtual.DelayedCall(landingAnimationDuration, () =>
        {
            CompleteLanding();
        });
    }

    private void CompleteLanding()
    {
        isLanding = false;

        // Re-enable movement
        if (movementInput != null)
            movementInput.enabled = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect != null)
        {
            effect.Play();
        }
    }

    // Public methods for external control
    public void TriggerSuperheroJump(Vector3 targetPosition)
    {
        if (CanJump())
        {
            jumpTargetPosition = targetPosition;
            StartSuperheroJump(targetPosition);
        }
    }

    public void TriggerSuperheroJump()
    {
        InitiateSuperheroJump();
    }

    public bool IsPerformingJump()
    {
        return isJumping || isLanding;
    }

    public void CancelJump()
    {
        if (jumpSequence != null)
        {
            jumpSequence.Kill();
        }

        // Stop wind effect if active
        if (windCuttingEffect != null)
        {
            windCuttingEffect.StopWindCuttingEffect();
        }

        isJumping = false;
        isLanding = false;

        if (movementInput != null)
            movementInput.enabled = true;

        if (animator != null)
        {
            animator.SetBool(isInAirBool, false);
        }
    }

    // Gizmos for visualization
    private void OnDrawGizmosSelected()
    {
        // Draw jump target
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(jumpTargetPosition, 1f);

        // Draw max jump distance
        Gizmos.color = Color.yellow;
        if (playerCamera != null)
        {
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 maxTarget = transform.position + forward * maxJumpDistance;
            Gizmos.DrawWireSphere(maxTarget, 0.5f);
            Gizmos.DrawLine(transform.position, maxTarget);
        }

        // Draw landing impact radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, landingImpactRadius);

        // Draw ground check
        Gizmos.color = Color.green;
        Vector3 checkStart = transform.position + Vector3.up * groundCheckDistance;
        Gizmos.DrawLine(checkStart, checkStart + Vector3.down * groundCheckDistance * 2f);
    }
}