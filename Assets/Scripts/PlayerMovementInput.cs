using System.Collections;

using Portfolio.InputSystem;

using Reflex.Attributes;

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementInput : MonoBehaviour
{
    #region Enums

    public enum MovementState
    {
        Disabled,
        Idle,
        Moving,
        Stopping,
        Boosting
    }

    public enum InitializationState
    {
        Uninitialized,
        Initializing,
        Ready
    }

    public enum AnimationState
    {
        Idle,
        Starting,
        Running,
        Stopping,
        Boosting
    }

    #endregion Enums

    #region Movement Settings

    [Header("Movement Settings")]
    public float Velocity = 5f;

    public float Speed;
    public float allowPlayerRotation = 0.1f;
    public float desiredRotationSpeed = 0.1f;

    #endregion Movement Settings

    #region Input

    [Header("Input")]
    [SerializeField] private float InputX;

    [SerializeField] private float InputZ;
    [SerializeField] private Vector3 desiredMoveDirection;

    #endregion Input

    #region Animation

    [Header("Animation")]
    public Animator anim;

    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;

    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;

    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;

    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    #endregion Animation

    #region Physics

    [Header("Physics")]
    public float verticalVel = -9.8f;

    [SerializeField] private Vector3 moveVector;

    #endregion Physics

    #region Audio

    [Header("Audio")]
    public AudioSource footstepSource;

    public AudioSource boostAudioSource;

    public AudioClip[] footstepClips;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;

    #endregion Audio

    #region Boost System

    [Header("Boost System")]
    [SerializeField] private bool isBoostActive = false;

    [SerializeField] private float boostTimer = 0f;
    [SerializeField] private float boostCooldownTimer = 0f;
    [SerializeField] private Vector3 boostDirection = Vector3.zero;
    [SerializeField] private float currentBoostSpeed = 0f;
    private BoostData currentBoostData;
    private GameObject activeBoostEffect;

    #endregion Boost System

    [Header("Camera Integration")]
    [SerializeField] private bool allowCameraRotationOverride = true;

    private bool isCameraOverridingRotation = true;

    #region State Management

    [Header("State (Debug)")]
    [SerializeField] private MovementState currentMovementState = MovementState.Disabled;

    [SerializeField] private InitializationState initState = InitializationState.Uninitialized;
    [SerializeField] private AnimationState animState = AnimationState.Idle;

    #endregion State Management

    #region References

    private Camera cam;
    private CharacterController controller;
    private Coroutine stopAnimationCoroutine;
    private Coroutine boostCoroutine;

    [Inject] private IPlatformDetector platformDetector;
    [Inject] private IPlatformMovementFactory movementFactory;
    [Inject] private SpeedBoostSettings boostSettings;
    private IPlatformMovementInputHandler movementInputHandler;

    #endregion References

    #region Unity Lifecycle

    private void OnEnable()
    {
        SplashScreen.OnLoadingComplete += OnLoadingComplete;
    }

    private void OnDisable()
    {
        SplashScreen.OnLoadingComplete -= OnLoadingComplete;
    }

    private void OnLoadingComplete()
    {
        SetInitializationState(InitializationState.Initializing);
        InitializeComponents();

        // Mobile platforms start with movement enabled
        if (platformDetector.CurrentPlatform == GamePlatform.Mobile)
        {
            SetMovementState(MovementState.Idle);
        }
    }

    private void Update()
    {
        if (initState != InitializationState.Ready) return;

        movementInputHandler?.UpdateInput();
        HandleMovementActivation();
        UpdateBoostCooldown();
        if (Input.GetKeyUp(KeyCode.P))
        {
            Debug.Log("Jumping");
            anim.SetBool("Jumping", true);
        }
    }

    private void OnDestroy()
    {
        CleanupCoroutines();
        CleanupInputHandler();
        CleanupBoostEffects();
    }

    #endregion Unity Lifecycle

    #region State Management Methods

    private void SetMovementState(MovementState newState)
    {
        if (currentMovementState == newState) return;

        MovementState previousState = currentMovementState;
        currentMovementState = newState;

        OnMovementStateChanged(previousState, newState);
    }

    private void SetInitializationState(InitializationState newState)
    {
        initState = newState;
    }

    private void SetAnimationState(AnimationState newState)
    {
        if (animState == newState) return;

        AnimationState previousState = animState;
        animState = newState;

        OnAnimationStateChanged(previousState, newState);
    }

    private void OnMovementStateChanged(MovementState previous, MovementState current)
    {
        switch (current)
        {
            case MovementState.Disabled:
                break;

            case MovementState.Idle:
                if (animState == AnimationState.Running)
                    SetAnimationState(AnimationState.Stopping);
                break;

            case MovementState.Moving:
                SetAnimationState(AnimationState.Running);
                break;

            case MovementState.Stopping:
                SetAnimationState(AnimationState.Stopping);
                break;

            case MovementState.Boosting:
                SetAnimationState(AnimationState.Boosting);
                break;
        }
    }

    private void OnAnimationStateChanged(AnimationState previous, AnimationState current)
    {
        switch (current)
        {
            case AnimationState.Starting:
                CleanupCoroutines();
                break;

            case AnimationState.Running:
                CleanupCoroutines();
                break;

            case AnimationState.Stopping:
                CleanupCoroutines();
                stopAnimationCoroutine = StartCoroutine(SmoothStopAnimation());
                break;

            case AnimationState.Boosting:
                CleanupCoroutines();
                break;
        }
    }

    #endregion State Management Methods

    #region Initialization

    private void InitializeComponents()
    {
        anim = GetComponent<Animator>();
        cam = Camera.main;
        controller = GetComponent<CharacterController>();

        if (cam == null)
        {
            Debug.LogError("Camera.main not found! Please ensure there's a camera tagged as MainCamera.");
        }

        if (controller == null)
        {
            Debug.LogError("CharacterController not found!");
        }

        if (boostSettings == null)
        {
            Debug.LogError("SpeedBoostSettings not injected! Please ensure it's registered in your DI container.");
        }

        movementInputHandler = movementFactory.GetHandler(platformDetector.CurrentPlatform);
        movementInputHandler.Initialize();
        movementInputHandler.SetEnabled(true);
        movementInputHandler.OnMovementInput += UpdateMovement;
        movementInputHandler.OnSpeedBoostRequested += HandleSpeedBoostRequest;

        SetInitializationState(InitializationState.Ready);
    }

    #endregion Initialization

    #region Input Handling

    private void HandleMovementActivation()
    {
        if (currentMovementState != MovementState.Disabled) return;

        switch (platformDetector.CurrentPlatform)
        {
            case GamePlatform.PC:
                if (Input.GetMouseButtonUp(0))
                {
                    Debug.Log("Enabling Movement");
                    SetMovementState(MovementState.Idle);
                }
                break;
        }
    }

    private void UpdateMovement(Vector2 input)
    {
        if (currentMovementState == MovementState.Disabled || currentMovementState == MovementState.Boosting) return;

        InputX = input.x;
        InputZ = input.y;
        ProcessMovementInput();
        ApplyGravity();
        PlayFootstepSounds();
    }

    private void ProcessMovementInput()
    {
        Speed = new Vector2(InputX, InputZ).sqrMagnitude;

        if (Speed > allowPlayerRotation)
        {
            SetMovementState(MovementState.Moving);
            UpdateAnimationBlend();
            CalculateDesiredMoveDirection();
            PerformPlayerRotation();
            PerformPlayerMove();
        }
        else
        {
            SetMovementState(MovementState.Idle);
        }
    }

    private void UpdateAnimationBlend()
    {
        if (anim != null && animState == AnimationState.Running)
        {
            anim.SetFloat("Blend", Speed, StartAnimTime, Time.deltaTime);
        }
    }

    #endregion Input Handling

    #region Boost System

    private void HandleSpeedBoostRequest(BoostData boostData)
    {
        if (!CanActivateBoost())
        {
            if (boostSettings.enableDebugLogging)
            {
                Debug.Log("Boost request denied - cooldown active or boost already running");
            }
            return;
        }

        ActivateBoost(boostData);
    }

    private bool CanActivateBoost()
    {
        return !isBoostActive &&
               boostCooldownTimer <= 0f &&
               currentMovementState != MovementState.Disabled &&
               boostSettings != null;
    }

    private void ActivateBoost(BoostData boostData)
    {
        currentBoostData = boostData;
        isBoostActive = true;
        boostTimer = boostData.duration;
        boostCooldownTimer = boostSettings.cooldownTime;

        // Immediately stop movement and block input
        InputX = 0f;
        InputZ = 0f;

        // Block input for the entire boost duration + pauses
        float totalInputBlockTime = boostData.preBoostPause + boostData.duration + boostData.postBoostPause;
        movementInputHandler.BlockInput(totalInputBlockTime);

        // Transform boost direction to world space
        if (cam != null)
        {
            var forward = cam.transform.forward;
            var right = cam.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            boostDirection = (forward * boostData.direction.z + right * boostData.direction.x).normalized;
        }
        else
        {
            boostDirection = transform.TransformDirection(boostData.direction);
        }

        SetMovementState(MovementState.Boosting);

        // Start boost coroutine with pauses
        boostCoroutine = StartCoroutine(BoostCoroutineWithPauses());

        if (boostSettings.enableDebugLogging)
        {
            Debug.Log($"Boost activated - Direction: {boostDirection}, Duration: {boostData.duration}");
        }
    }

    private IEnumerator BoostCoroutineWithPauses()
    {
        // Pre-boost pause
        if (boostSettings.enableDebugLogging)
        {
            Debug.Log($"Pre-boost pause: {currentBoostData.preBoostPause}s");
        }

        yield return new WaitForSeconds(currentBoostData.preBoostPause);

        // Start boost effects and animation
        PlayBoostEffects();

        // Set boost animation
        if (anim != null)
        {
            anim.SetFloat("Blend", 2f); // Max blend for boost animation
            anim.SetTrigger("Boost"); // Trigger boost animation
        }

        // Main boost phase
        float elapsed = 0f;
        float baseSpeed = Velocity * currentBoostData.speedMultiplier;

        while (elapsed < currentBoostData.duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / currentBoostData.duration;

            // Apply boost curve
            float curveValue = boostSettings.boostCurve.Evaluate(t);
            currentBoostSpeed = baseSpeed * curveValue;

            // Move player
            Vector3 boostMovement = boostDirection * currentBoostSpeed * Time.deltaTime;
            controller.Move(boostMovement);

            // Apply gravity during boost
            ApplyGravity();

            yield return null;
        }

        // Stop boost effects
        StopBoostEffects();

        // Post-boost pause
        if (boostSettings.enableDebugLogging)
        {
            Debug.Log($"Post-boost pause: {currentBoostData.postBoostPause}s");
        }

        yield return new WaitForSeconds(currentBoostData.postBoostPause);

        // End boost
        DeactivateBoost();
    }

    private void DeactivateBoost()
    {
        isBoostActive = false;
        boostTimer = 0f;
        currentBoostSpeed = 0f;
        boostDirection = Vector3.zero;

        // Reset animation blend
        if (anim != null)
        {
            anim.SetFloat("Blend", 0f);
        }

        SetMovementState(MovementState.Idle);

        if (boostSettings.enableDebugLogging)
        {
            Debug.Log("Boost deactivated");
        }
    }

    private void UpdateBoostCooldown()
    {
        if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.deltaTime;
        }
    }

    private void PlayBoostEffects()
    {
        if (boostSettings == null) return;

        // Play activation sound
        if (boostAudioSource != null && boostSettings.boostActivationSound != null)
        {
            boostAudioSource.PlayOneShot(boostSettings.boostActivationSound);
        }

        // Play loop sound
        if (boostAudioSource != null && boostSettings.boostLoopSound != null)
        {
            boostAudioSource.clip = boostSettings.boostLoopSound;
            boostAudioSource.loop = true;
            boostAudioSource.Play();
        }

        // Spawn particle effect
        if (boostSettings.boostParticleEffect != null)
        {
            activeBoostEffect = Instantiate(boostSettings.boostParticleEffect, transform);
        }
    }

    private void StopBoostEffects()
    {
        // Stop loop sound
        if (boostAudioSource != null && boostAudioSource.isPlaying)
        {
            boostAudioSource.Stop();
        }

        // Destroy particle effect
        if (activeBoostEffect != null)
        {
            Destroy(activeBoostEffect);
            activeBoostEffect = null;
        }
    }

    #endregion Boost System

    #region Movement & Rotation

    private void PerformPlayerMove()
    {
        // Move the character
        controller.Move(desiredMoveDirection * Time.deltaTime * Velocity);
    }

    private void PerformPlayerRotation()
    {
        // Skip rotation if camera is overriding it
        if (IsCameraOverridingRotation())
        {
            return;
        }
        // Handle rotation (camera system can override this later)
        if (desiredMoveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(desiredMoveDirection),
                desiredRotationSpeed
            );
        }
    }

    private void CalculateDesiredMoveDirection()
    {
        if (cam == null) return;

        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        desiredMoveDirection = forward * InputZ + right * InputX;
    }

    private void ApplyGravity()
    {
        moveVector = new Vector3(0, verticalVel * 0.2f * Time.deltaTime, 0);
        controller.Move(moveVector);
    }

    #endregion Movement & Rotation

    #region Animation

    private IEnumerator SmoothStopAnimation()
    {
        float currentBlend = anim != null ? anim.GetFloat("Blend") : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < StopAnimTime && anim != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / StopAnimTime;
            float blendValue = Mathf.Lerp(currentBlend, 0f, t);

            anim.SetFloat("Blend", blendValue);
            yield return null;
        }

        // Ensure it's set to 0
        if (anim != null)
            anim.SetFloat("Blend", 0f);

        SetAnimationState(AnimationState.Idle);
        stopAnimationCoroutine = null;
    }

    #endregion Animation

    #region Audio

    private void PlayFootstepSounds()
    {
        if (currentMovementState != MovementState.Moving)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer += Time.deltaTime;
        bool isRunning = currentMovementState == MovementState.Moving;
        float currentInterval = isRunning ? footstepInterval * 0.7f : footstepInterval;

        if (footstepTimer >= currentInterval)
        {
            footstepTimer = 0f;
            PlayRandomFootstep();
        }
    }

    private void PlayRandomFootstep()
    {
        if (footstepSource != null && footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.PlayOneShot(clip, 0.5f);
        }
    }

    #endregion Audio

    #region Camera Integration Methods

    /// <summary>
    /// Called by camera system to override player rotation
    /// </summary>
    /// <param name="overrideRotation">Whether camera should control player rotation</param>
    public void SetCameraRotationOverride(bool overrideRotation)
    {
        if (!allowCameraRotationOverride) return;

        isCameraOverridingRotation = overrideRotation;

        if (overrideRotation)
        {
            Debug.Log("Camera is now controlling player rotation");
        }
        else
        {
            Debug.Log("Player rotation control returned to movement system");
        }
    }

    /// <summary>
    /// Check if camera is currently overriding rotation
    /// </summary>
    /// <returns>True if camera is controlling rotation</returns>
    public bool IsCameraOverridingRotation()
    {
        return isCameraOverridingRotation && allowCameraRotationOverride;
    }

    /// <summary>
    /// Allow or disallow camera rotation override
    /// </summary>
    /// <param name="allow">Whether to allow camera override</param>
    public void SetAllowCameraRotationOverride(bool allow)
    {
        allowCameraRotationOverride = allow;

        if (!allow)
        {
            isCameraOverridingRotation = false;
        }
    }

    #endregion Camera Integration Methods

    #region Cleanup

    private void CleanupCoroutines()
    {
        if (stopAnimationCoroutine != null)
        {
            StopCoroutine(stopAnimationCoroutine);
            stopAnimationCoroutine = null;
        }
    }

    private void CleanupInputHandler()
    {
        if (movementInputHandler != null)
        {
            movementInputHandler.OnMovementInput -= UpdateMovement;
            movementInputHandler.OnSpeedBoostRequested -= HandleSpeedBoostRequest;
        }
    }

    private void CleanupBoostEffects()
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            boostCoroutine = null;
        }
    }

    #endregion Cleanup
}