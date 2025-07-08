using UnityEngine;

public class FootstepDustEffect : MonoBehaviour
{
    [Header("Particle System")]
    public ParticleSystem dustParticleSystem;

    [Header("Footstep Settings")]
    public float stepDistance = 2f; // Distance between footsteps

    public LayerMask groundLayer = 1; // Ground layer mask
    public float raycastDistance = 1.5f; // Distance to check for ground

    [Header("Animation Events (Optional)")]
    public bool useAnimationEvents = false;

    private Vector3 lastFootstepPosition;
    private CharacterController characterController;
    private Rigidbody rb;

    private void Start()
    {
        // Get character movement component
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        lastFootstepPosition = transform.position;

        // Create particle system if not assigned
        if (dustParticleSystem == null)
        {
            CreateDustParticleSystem();
        }
    }

    private void Update()
    {
        // Only check for footsteps if not using animation events
        if (!useAnimationEvents)
        {
            CheckForFootstep();
        }
    }

    private void CheckForFootstep()
    {
        // Check if character is moving and on ground
        if (IsMoving() && IsOnGround())
        {
            // Check if enough distance has been covered for a footstep
            float distanceCovered = Vector3.Distance(transform.position, lastFootstepPosition);

            if (distanceCovered >= stepDistance)
            {
                PlayFootstepEffect();
                lastFootstepPosition = transform.position;
            }
        }
    }

    private bool IsMoving()
    {
        if (characterController != null)
        {
            return characterController.velocity.magnitude > 0.1f;
        }
        else if (rb != null)
        {
            return rb.linearVelocity.magnitude > 0.1f;
        }

        return false;
    }

    private bool IsOnGround()
    {
        // Raycast downward to check if on ground
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            return true;
        }

        // Alternative: Use CharacterController's isGrounded if available
        if (characterController != null)
        {
            return characterController.isGrounded;
        }

        return false;
    }

    public void PlayFootstepEffect()
    {
        if (dustParticleSystem != null)
        {
            // Position particle system at ground level
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayer))
            {
                dustParticleSystem.transform.position = hit.point;
                dustParticleSystem.Play();
            }
        }
    }

    // Call this method from animation events for more precise timing
    public void OnFootstepAnimationEvent()
    {
        if (useAnimationEvents && IsOnGround())
        {
            PlayFootstepEffect();
        }
    }

    [ContextMenu("Setup")]
    private void CreateDustParticleSystem()
    {
        if (dustParticleSystem == null)
        {// Create a new GameObject for the particle system
            GameObject dustObject = new GameObject("Dust Particle System");
            dustObject.transform.SetParent(transform);
            dustObject.transform.localPosition = Vector3.zero;

            // Add and configure particle system
            dustParticleSystem = dustObject.AddComponent<ParticleSystem>();
        }

        var main = dustParticleSystem.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = new Color(0.8f, 0.7f, 0.6f, 0.8f); // Dusty brown color
        main.maxParticles = 50;

        var emission = dustParticleSystem.emission;
        emission.rateOverTime = 0; // We'll trigger manually
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 10, 15, 1, 0.2f)
        });

        var shape = dustParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        var velocityOverLifetime = dustParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, 3f);

        var sizeOverLifetime = dustParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.2f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.8f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = dustParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;
    }
}