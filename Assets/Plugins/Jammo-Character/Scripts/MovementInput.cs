using System.Collections;
using System.Collections.Generic;

using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour
{
    public float Velocity;

    [Space]
    public float InputX;

    public float InputZ;
    public Vector3 desiredMoveDirection;
    public bool blockRotationPlayer;
    public float desiredRotationSpeed = 0.1f;
    public Animator anim;
    public float Speed;
    public float allowPlayerRotation = 0.1f;
    public Camera cam;
    public CharacterController controller;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;

    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;

    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;

    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public float verticalVel;
    [SerializeField] private Vector3 moveVector;

    [Header("Audio")]
    public AudioSource footstepSource;

    public AudioClip[] footstepClips;

    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;
    private bool isRunning;

    private bool isMovementEnabled = false;

    // Use this for initialization
    private void Start()
    {
        anim = this.GetComponent<Animator>();
        cam = Camera.main;
        controller = this.GetComponent<CharacterController>();
        isRunning = false;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isMovementEnabled && Input.GetMouseButtonUp(0))
        {
            isMovementEnabled = true;
        }
        if (isMovementEnabled)
        {
            InputMagnitude();
            moveVector = new Vector3(0, verticalVel * .2f * Time.deltaTime, 0);

            controller.Move(moveVector);
            PlayFootstepSounds();
        }
    }

    private void PlayerMoveAndRotation()
    {
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

        var camera = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        desiredMoveDirection = forward * InputZ + right * InputX;
        if (InputZ != 0 || InputX != 0)
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
        if (blockRotationPlayer == false)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
            controller.Move(desiredMoveDirection * Time.deltaTime * Velocity);
        }
    }

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var camera = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        desiredMoveDirection = forward;

        t.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
    }

    private void InputMagnitude()
    {
        //Calculate Input Vectors
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");

        //anim.SetFloat ("InputZ", InputZ, VerticalAnimTime, Time.deltaTime * 2f);
        //anim.SetFloat ("InputX", InputX, HorizontalAnimSmoothTime, Time.deltaTime * 2f);

        //Calculate the Input Magnitude
        Speed = new Vector2(InputX, InputZ).sqrMagnitude;

        //Physically move player

        if (Speed > allowPlayerRotation)
        {
            anim.SetFloat("Blend", Speed, StartAnimTime, Time.deltaTime);
            PlayerMoveAndRotation();
        }
        else if (Speed < allowPlayerRotation)
        {
            anim.SetFloat("Blend", Speed, StopAnimTime, Time.deltaTime);
            // Reset isRunning when player stops moving
            isRunning = false;
        }
    }

    private void PlayFootstepSounds()
    {
        if (!IsMoving())
        {
            // Reset footstep timer when not moving to prevent audio lag
            footstepTimer = 0f;
            return;
        }

        footstepTimer += Time.deltaTime;
        float currentInterval = isRunning ? footstepInterval * 0.7f : footstepInterval;

        if (footstepTimer >= currentInterval)
        {
            footstepTimer = 0f;
            PlayRandomFootstep();
        }
    }

    private bool IsMoving()
    {
        // Check if player is actually moving by using the Speed value
        // This is more reliable than just checking isRunning
        return Speed > allowPlayerRotation;
    }

    private void PlayRandomFootstep()
    {
        if (footstepSource != null && footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
            footstepSource.PlayOneShot(clip, 0.5f);
        }
    }
}