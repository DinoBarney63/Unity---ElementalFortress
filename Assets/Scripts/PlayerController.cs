using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Movement speed of the player. (m/s)")]
    public float moveSpeed = 2.0f;
    [Tooltip("Sprinting speed of the player. (m/s)")]
    public float sprintSpeed = 5.335f;
    [Tooltip("How the sprinting works. 1 - Hold, 2 - Toggle, 3 - Always")]
    [Range(1, 3)] public int sprintType = 2;
    [Tooltip("Whether the player is sprinting")]
    public bool isSprinting;
    [Tooltip("How fast the player turns to face direction.")]
    public float rotationSpeed = 1.0f;
    [Tooltip("Acceleration and deceleration speed.")]
    public float speedChangeRate = 10.0f;

    public AudioClip landingAudioClip;
    public AudioClip[] footstepAudioClips;
    [Range(0, 1)] public float footstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump.")]
    public float jumpHeight = 1.2f;
    [Tooltip("The force of gravity value on the player. Normaly it is -9.81f")]
    public float gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again.")]
    public float jumpTimeout = 0.1f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs.")]
    public float fallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. (Seperate to the CharacterController in grounded check)")]
    public bool grounded = true;
    [Tooltip("Useful for rough ground")]
    public float groundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. (Should match the radius of the CharacterController)")]
    public float groundedRadius = 0.28f;
    [Tooltip("What layers the player uses as ground")]
    public LayerMask groundLayers;

    [Header("Player Perspective")]
    [Tooltip("The visual gameobject for the player")]
    public GameObject playerVisuals;
    [Tooltip("The Cinemachine Virtual Camera for the first person perspective")]
    public GameObject firstPersonCamera;
    [Tooltip("The Cinemachine Virtual Camera for the third person perspective")]
    public GameObject thirdPersonCamera;
    [Tooltip("Whether the player is in first person or third person")]
    public bool perspective;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject cinemachineCameraTarget;
    [Tooltip("How far in degrees can you rotate the camera up")]
    public float topClamp = 70.0f;
    [Tooltip("How far in degrees can you rotate the camera down")]
    public float bottomClamp = -30.0f;

    // cinemachine
    private float _cinemachineTargetPitch;

    // Player
    private float _speed;
    private float _animationBlend;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // Timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // Animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private PlayerInput _playerInput;
    private Animator _animator;
    private CharacterController _controller;
    private Inputs _input;

    private const float _threshold = 0.01f;
    private const float speedOffset = 0.1f;

    private bool _hasAnimator;

    private bool IsCurrentDeviceMouse
    {
        get
        {
            return _playerInput.currentControlScheme == "KeyboardMouse";
        }
    }

    // Colors for OnDrawGizmosSelected
    Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
    Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

    private void Start()
    {
            
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<Inputs>();
        _playerInput = GetComponent<PlayerInput>();

        AssignAnimationIDs();

        // Reset our timeouts on start
        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();

        
    }

    private void LateUpdate()
    {
        CheckPerspective();
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // Set sphere position, with offset
        Vector3 spherePosition = new(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

        // Update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, grounded);
        }
    }

    private void JumpAndGravity()
    {
        if (grounded)
        {
            // Reset the fall timeout timer
            _fallTimeoutDelta = fallTimeout;

            // Update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // Stop velocity from dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // The square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                // Update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // Jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // Reset the jump timeout timer
            _jumpTimeoutDelta = jumpTimeout;

            // Fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // Update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }
        }

        // Apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void Move()
    {
        Vector2 move = _input.move;
        bool sprint = _input.sprint;
        
        // Calculates whether the player is sprinting or not depending on the sprinting type selected
        if(sprintType == 1)
        {
            isSprinting = sprint;
        }
        else if (sprintType == 2)
        {
            if (move != Vector2.zero)
            {
                if (sprint)
                {
                    isSprinting = true;
                }
            }
            else
                isSprinting = false;
            
        }
        else if (sprintType == 3)
        {
            if (move == Vector2.zero)
                isSprinting = false;
            else
                isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }
        
        // Sets target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // If there is no input, set the target speed to 0
        if (move == Vector2.zero) targetSpeed = 0.0f;

        // A reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float inputMagnitude = _input.IsAnalog() ? move.magnitude : 1f;

        // Accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // Creates curved result rather than a linear one giving a more organic speed change
            // Note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * speedChangeRate);

            // Round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // Normalise input direction
        Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

        // If there is a move input rotate player when the player is moving
        if (move != Vector2.zero)
        {
            // Move
            inputDirection = transform.right * move.x + transform.forward * move.y;
        }

        // Move the player
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // Update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void CheckPerspective()
    {
        if (_input.perspective)
        {
            perspective = !perspective;

            if (perspective)
            {
                firstPersonCamera.SetActive(true);
                playerVisuals.SetActive(false);
                thirdPersonCamera.SetActive(false);
            }
            else
            {
                firstPersonCamera.SetActive(false);
                playerVisuals.SetActive(true);
                thirdPersonCamera.SetActive(true);
            }
        }
    }

    private void CameraRotation()
    {
        Vector2 look = _input.look;
        // If there is an input
        if (look.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += look.y * rotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = look.x * rotationSpeed * deltaTimeMultiplier;

            // Clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

            // Update Cinemachine camera target pitch
            cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // Rotate the player left and right
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        if (grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // When selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
            groundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (footstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, footstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(footstepAudioClips[index], transform.TransformPoint(_controller.center), footstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(landingAudioClip, transform.TransformPoint(_controller.center), footstepAudioVolume);
        }
    }
}
