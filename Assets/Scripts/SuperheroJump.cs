using System.Collections;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CharacterController))]
public class SuperheroJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float maxJumpHeight = 15f;

    [SerializeField] private float jumpUpDuration = 1f;
    [SerializeField] private float jumpDownDuration = 1.5f;
    [SerializeField] private Transform jumpTarget; // Target to fly to

    [Header("Input")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [SerializeField] private bool disableMovementDuringJump = true;

    [Header("Landing Settings")]
    [SerializeField] private float landingImpactRadius = 5f;

    [SerializeField] private float landingAnimationDuration = 2f;
    [SerializeField] private LayerMask groundMask = 1;
    [SerializeField] private float groundCheckDistance = 50f;

    [Header("Movement Control")]
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private AnimationCurve landingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation Parameters")]
    [SerializeField] private string jumpTrigger = "Jump";

    [SerializeField] private string landingTrigger = "Landing";
    [SerializeField] private string isInAirBool = "IsInAir";

    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect;

    [SerializeField] private ParticleSystem landingEffect;
    [SerializeField] private ParticleSystem windTrail; // Wind trail particle system
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [SerializeField] private float cameraShakeDuration = 0.3f;

    private CharacterController controller;
    private MovementInput movementInput;
    private Animator animator;
    private Camera playerCamera;
    private Vector3 jumpStartPosition;
    private Vector3 jumpTargetPosition;
    private bool isJumping = false;
    private bool isLanding = false;
    private Sequence jumpSequence;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        movementInput = GetComponent<MovementInput>();
        animator = GetComponent<Animator>();
        playerCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Ensure wind trail is stopped at start
        if (windTrail != null)
        {
            windTrail.Stop();
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
            Debug.Log("Jump key pressed, initiating superhero jump.");
            InitiateSuperheroJump();
        }
    }

    private void InitiateSuperheroJump()
    {
        Debug.Log("Can Jump - " + CanJump());
        if (!CanJump()) return;
        Vector3 targetPosition = CalculateJumpTarget();
        StartSuperheroJump(targetPosition);
    }

    private bool CanJump()
    {
        return controller.isGrounded && !isJumping && !isLanding;
    }

    private Vector3 CalculateJumpTarget()
    {
        // If jump target is assigned, use it
        if (jumpTarget != null)
        {
            return jumpTarget.position;
        }

        // No target assigned, crash land at starting position
        return transform.position;
    }

    private void StartSuperheroJump(Vector3 targetPosition)
    {
        isJumping = true;
        jumpStartPosition = transform.position;
        jumpTargetPosition = targetPosition;

        // Disable movement input during jump if enabled
        if (disableMovementDuringJump && movementInput != null)
            movementInput.enabled = false;

        // Rotate towards target if not crash landing
        if (jumpTarget != null)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                transform.DORotateQuaternion(Quaternion.LookRotation(direction), 0.5f);
            }
        }

        // Play jump animation and sound
        if (animator != null)
        {
            animator.SetTrigger(jumpTrigger);
            animator.SetBool(isInAirBool, true);
        }

        PlaySound(jumpSound);
        PlayEffect(jumpEffect);

        // Start wind trail particle system
        if (windTrail != null)
        {
            windTrail.Play();
        }

        // Start jump sequence
        ExecuteJumpSequence();
    }

    private void ExecuteJumpSequence()
    {
        jumpSequence = DOTween.Sequence();

        // Calculate arc positions
        Vector3 midPoint;

        if (jumpTarget != null)
        {
            // Flying to target - create arc
            midPoint = Vector3.Lerp(jumpStartPosition, jumpTargetPosition, 0.5f);
            midPoint.y = Mathf.Max(jumpStartPosition.y, jumpTargetPosition.y) + maxJumpHeight;
        }
        else
        {
            // Crash landing - go straight up then down
            midPoint = jumpStartPosition + Vector3.up * maxJumpHeight;
        }

        // Jump up phase
        jumpSequence.Append(
            transform.DOMove(midPoint, jumpUpDuration)
                .SetEase(jumpCurve)
        );

        // Jump down phase
        jumpSequence.Append(
            transform.DOMove(jumpTargetPosition, jumpDownDuration)
                .SetEase(landingCurve)
                .OnStart(() =>
                {
                    // Stop wind trail when starting descent
                    if (windTrail != null)
                    {
                        windTrail.Stop();
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

        // Re-enable movement if it was disabled
        if (disableMovementDuringJump && movementInput != null)
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

        // Stop wind trail if active
        if (windTrail != null)
        {
            windTrail.Stop();
        }

        isJumping = false;
        isLanding = false;

        if (disableMovementDuringJump && movementInput != null)
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
        if (jumpTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(jumpTarget.position, 1f);
            Gizmos.DrawLine(transform.position, jumpTarget.position);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * maxJumpHeight, 1f);
        }

        // Draw landing impact radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, landingImpactRadius);

        // Draw ground check
        Gizmos.color = Color.yellow;
        Vector3 checkStart = transform.position + Vector3.up * groundCheckDistance;
        Gizmos.DrawLine(checkStart, checkStart + Vector3.down * groundCheckDistance * 2f);
    }
}