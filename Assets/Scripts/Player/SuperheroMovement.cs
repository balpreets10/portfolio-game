using System.Collections;
using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(CharacterController))]
public class SuperheroMovement : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float maxJumpHeight = 15f;

    [SerializeField] private float jumpUpDuration = 1f;
    [SerializeField] private float jumpDownDuration = 1.5f;
    [SerializeField] private float horizontalMoveDuration = 1f; // NEW: Duration for horizontal movement
    [SerializeField] private float pauseAtTopDuration = 0.5f; // NEW: Pause duration at the top
    [SerializeField] private Transform jumpTarget; // Target to fly to

    [Header("Input")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [SerializeField] private bool disableMovementDuringJump = true;

    [Header("Landing Settings")]
    [SerializeField] private float landingImpactRadius = 5f;

    [SerializeField] private float landingAnimationDuration = 2f;
    [SerializeField] private LayerMask groundMask = 1;
    [SerializeField] private float groundCheckDistance = 50f;
    [SerializeField] private float groundCheckRadius = 0.5f; // For SphereCast
    [SerializeField] private float minGroundHeight = 0f; // Minimum Y position for ground

    [Header("Movement Control")]
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private AnimationCurve landingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation Parameters")]
    [SerializeField] private string jumpTrigger = "Jump";

    [SerializeField] private string landingTrigger = "Landing";
    [SerializeField] private string isInAirBool = "IsInAir";

    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect;

    [SerializeField] private GameObject effectText;

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
        effectText.SetActive(false);
    }

    private void Update()
    {
        // Keep player on ground when not jumping
        if (!isJumping && !isLanding)
        {
            KeepPlayerOnGround();
        }
    }

    private void OnEnable()
    {
        SectionHouse.OnSectionHouseInteracted += TriggerSuperheroJump;
        SectionDetails.OnSectionDetailsInteracted += JumpBackToMain;
    }

    private void OnDisable()
    {
        SectionHouse.OnSectionHouseInteracted -= TriggerSuperheroJump;
        SectionDetails.OnSectionDetailsInteracted -= JumpBackToMain;
    }

    private void JumpBackToMain()
    {
        Vector3 groundPosition = GetGroundPosition(jumpStartPosition);
        InitiateSuperheroJump(groundPosition);
    }

    private void TriggerSuperheroJump(Section section)
    {
        jumpTarget = section.targetSection.landingPosition;
        Vector3 targetGroundPosition = GetGroundPosition(CalculateJumpTarget());
        InitiateSuperheroJump(targetGroundPosition);
    }

    private void InitiateSuperheroJump(Vector3 position)
    {
        StartSuperheroJump(position);
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

    // NEW METHOD: Get ground position using raycast
    private Vector3 GetGroundPosition(Vector3 targetPosition)
    {
        // Start raycast from high above the target position
        Vector3 raycastStart = new Vector3(targetPosition.x, targetPosition.y + groundCheckDistance, targetPosition.z);

        // Try regular raycast first
        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundMask))
        {
            Vector3 groundPos = hit.point;

            // Position player just above ground (not inside)
            groundPos.y += 0.1f; // Small offset to prevent clipping

            //Debug.Log($"Ground found with Raycast at: {groundPos}, hit object: {hit.collider.name}");
            return groundPos;
        }

        // Try SphereCast as backup
        if (Physics.SphereCast(raycastStart, groundCheckRadius, Vector3.down, out hit,
            groundCheckDistance * 2f, groundMask))
        {
            Vector3 groundPos = hit.point;
            groundPos.y += 0.1f;

            //Debug.Log($"Ground found with SphereCast at: {groundPos}, hit object: {hit.collider.name}");
            return groundPos;
        }

        // Last resort: check all colliders in area
        Collider[] colliders = Physics.OverlapSphere(targetPosition, 10f, groundMask);
        if (colliders.Length > 0)
        {
            float highestY = float.MinValue;
            foreach (var col in colliders)
            {
                if (col.bounds.max.y > highestY)
                {
                    highestY = col.bounds.max.y;
                }
            }
            Vector3 groundPos = new Vector3(targetPosition.x, highestY + 0.1f, targetPosition.z);
            Debug.Log($"Ground found with OverlapSphere at: {groundPos}");
            return groundPos;
        }

        // Fallback: use original position or minimum height
        Vector3 fallbackPos = new Vector3(targetPosition.x, Mathf.Max(targetPosition.y, minGroundHeight), targetPosition.z);
        Debug.LogWarning($"No ground found at {targetPosition}, using fallback: {fallbackPos}");
        return fallbackPos;
    }

    // NEW METHOD: Keep player on ground during normal gameplay
    private void KeepPlayerOnGround()
    {
        if (!controller.isGrounded)
        {
            Vector3 currentPos = transform.position;
            Vector3 groundPos = GetGroundPosition(currentPos);

            // Smoothly move to ground position
            if (Vector3.Distance(currentPos, groundPos) > 0.1f)
            {
                transform.position = Vector3.Lerp(currentPos, groundPos, Time.deltaTime * 10f);
            }
        }
    }

    private void StartSuperheroJump(Vector3 targetPosition)
    {
        isJumping = true;
        jumpStartPosition = transform.position;

        // Ensure target position is on ground
        jumpTargetPosition = GetGroundPosition(targetPosition);

        // Disable movement input during jump if enabled
        if (disableMovementDuringJump && movementInput != null)
            movementInput.enabled = false;

        // Rotate towards target if not crash landing
        if (jumpTarget != null)
        {
            Vector3 direction = (jumpTargetPosition - transform.position).normalized;
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
        effectText.SetActive(true);
        // Calculate positions for the new jump pattern
        Vector3 topPosition = new Vector3(jumpStartPosition.x, jumpStartPosition.y + maxJumpHeight, jumpStartPosition.z);
        Vector3 horizontalTargetPosition = new Vector3(jumpTargetPosition.x, jumpStartPosition.y + maxJumpHeight, jumpTargetPosition.z);

        // Phase 1: Jump straight up
        jumpSequence.Append(
            transform.DOMove(topPosition, jumpUpDuration)
                .SetEase(jumpCurve)
        );

        // Phase 2: Pause at the top
        jumpSequence.AppendInterval(pauseAtTopDuration);

        // Phase 3: Move horizontally to target X,Z coordinates (staying at same height)
        if (Vector3.Distance(topPosition, horizontalTargetPosition) > 0.1f)
        {
            jumpSequence.Append(
                transform.DOMove(horizontalTargetPosition, horizontalMoveDuration)
                    .SetEase(Ease.Linear)
            );
        }

        jumpSequence.AppendInterval(pauseAtTopDuration);

        // Phase 4: Drop straight down to ground
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
                .OnUpdate(() =>
                {
                    // Continuously check for ground during descent
                    Vector3 currentPos = transform.position;
                    Vector3 groundPos = GetGroundPosition(currentPos);

                    // If we're close to ground, snap to it
                    if (currentPos.y <= groundPos.y + 0.5f)
                    {
                        jumpTargetPosition = groundPos;
                    }
                })
        );

        jumpSequence.OnComplete(() =>
        {
            // Final ground check before landing
            Vector3 finalGroundPos = GetGroundPosition(transform.position);
            transform.position = finalGroundPos;
            effectText.SetActive(false);
            ExecuteLanding();
        });
    }

    private void ExecuteLanding()
    {
        isJumping = false;
        isLanding = true;

        // Final ground position adjustment
        Vector3 groundPos = GetGroundPosition(transform.position);
        transform.position = groundPos;

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

        // Final ground check
        Vector3 groundPos = GetGroundPosition(transform.position);
        transform.position = groundPos;

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
            Vector3 groundTargetPosition = GetGroundPosition(targetPosition);
            jumpTargetPosition = groundTargetPosition;
            StartSuperheroJump(groundTargetPosition);
        }
    }

    public void TriggerSuperheroJump()
    {
        Vector3 targetPosition = CalculateJumpTarget();
        if (jumpTarget == null)
        {
            Debug.LogWarning("Jump target is not set. Cannot initiate jump.");
            return;
        }
        Vector3 groundTargetPosition = GetGroundPosition(targetPosition);
        InitiateSuperheroJump(groundTargetPosition);
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

        // Ensure player is on ground
        Vector3 groundPos = GetGroundPosition(transform.position);
        transform.position = groundPos;

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

            // Draw ground detection
            Vector3 groundPos = GetGroundPosition(jumpTarget.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundPos, 0.5f);
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

        // Draw ground check radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }
}