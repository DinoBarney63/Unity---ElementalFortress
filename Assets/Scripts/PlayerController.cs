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
    public float jumpTimeout = 0.25f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs.")]
    public float fallTimeout = 0.15f;

    [Space(10)]
    [Tooltip("The player's max health")]
    public int health = 100;
    [Tooltip("Time until regeneration")]
    public float regenerationCountdown = 5;
    [Tooltip("Speed of regeneration")]
    public float regenerationTimer = 1;

    [Tooltip("The player's max stamina")]
    public float stamina = 50;
    [Tooltip("Time until replenishment")]
    public float replenishmentCountdown = 2;
    [Tooltip("Speed of replenishment")]
    public float replenishmentTimer = 0.25f;

    [Space(10)]
    [Tooltip("How far the player can interact with objects")]
    public float reach = 5;
    public int swordLevel = 1;
    public int axeLevel = 1;
    public int pickaxeLevel = 1;

    [Space(10)]
    [Tooltip("Player defence")]
    public ElementalInfo[] elementalDefence;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. (Seperate to the CharacterController in grounded check)")]
    public bool grounded = true;
    [Tooltip("Useful for rough ground")]
    public float groundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. (Should match the radius of the CharacterController)")]
    public float groundedRadius = 0.28f;
    [Tooltip("What layers the player uses as ground")]
    public LayerMask groundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject cinemachineCameraTarget;
    [Tooltip("How far in degrees can you rotate the camera up")]
    public float topClamp = 70.0f;
    [Tooltip("How far in degrees can you rotate the camera down")]
    public float bottomClamp = -30.0f;

    // cinemachine
    private float _cinemachineTargetPitch;

    // player
    [Header("Player Info")]
    public int _health;
    public float _stamina;
    public bool _exhausted;
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private int _neutralDefence = 0;
    private int _earthDefence = 0;
    private int _airDefence = 0;
    private int _thunderDefence = 0;
    private int _waterDefence = 0;
    private int _fireDefence = 0;

    // Timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;
    private float _regenerationCountdown;
    private float _regenerationTimer;
    private float _replenishmentCountdown;
    private float _replenishmentTimer;

    private PlayerInput _playerInput;
    private CharacterController _controller;
    private Inputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;
    private const float speedOffset = 0.1f;

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
        swordLevel = 1;
        axeLevel = 1;
        pickaxeLevel = 1;
        
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<Inputs>();
        _playerInput = GetComponent<PlayerInput>();
        _mainCamera = GameObject.Find("MainCamera");

        _health = health;
        _stamina = stamina;

        // Reset our timeouts on start
        _jumpTimeoutDelta = jumpTimeout;
        _fallTimeoutDelta = fallTimeout;
        _regenerationCountdown = regenerationCountdown;
        _regenerationTimer = regenerationTimer;
        _replenishmentCountdown = replenishmentCountdown;
}

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();

        Interaction();

        HealthRegeneration();
        StaminaReplenishment();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        // Set sphere position, with offset
        Vector3 spherePosition = new(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void JumpAndGravity()
    {
        if (grounded)
        {
            // Reset the fall timeout timer
            _fallTimeoutDelta = fallTimeout;

            // Stop velocity from dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f && !_exhausted)
            {
                // The square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                ChangeStamina(-5);

                // Reset the jump timeout timer
                _jumpTimeoutDelta = jumpTimeout;
            }

            // Jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // Fall timeout (Can be used to calculate fall damage)
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
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
        if (_exhausted)
        {
            isSprinting = false;
        }
        else if (sprintType == 1)
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

        if (isSprinting)
            ChangeStamina(-Time.deltaTime);
        
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
    }

    private void Interaction()
    {
        bool use1 = _input.use1;
        bool use2 = _input.use2;

        if (use1 || use2)
        {
            Ray ray = new(_mainCamera.transform.position, _mainCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, reach))
            {
                ChangeStamina(-2.5f);
                GameObject objectHit = hit.collider.gameObject;
                if (objectHit.CompareTag("MaterialObject"))
                {
                    objectHit.GetComponent<MaterialObject>().PlayerInteract();
                }
            }
        }
    }

    private void HealthRegeneration()
    {
        if (_regenerationCountdown > 0)
        {
            _regenerationCountdown -= Time.deltaTime;
        }
        else
        {
            if (_regenerationTimer > 0)
            {
                _regenerationTimer -= Time.deltaTime;
            }
            else
            {
                _regenerationTimer = regenerationTimer;
                if (_health < health)
                    _health += 1;
            }
        }
    }

    public void ChangeHealth(ElementalInfo info)
    {
        CheckDefence();

        int amount = info.value;
        if (amount < 0)
        {
            if (info.type == ElementalInfo.Type.neutral)
            {
                amount += _neutralDefence;
            }
            else if (info.type == ElementalInfo.Type.earth)
            {
                amount += _fireDefence;
                amount -= _airDefence;
            }
            else if (info.type == ElementalInfo.Type.air)
            {
                amount += _earthDefence;
                amount -= _thunderDefence;
            }
            else if (info.type == ElementalInfo.Type.thunder)
            {
                amount += _airDefence;
                amount -= _waterDefence;
            }
            else if (info.type == ElementalInfo.Type.water)
            {
                amount += _thunderDefence;
                amount -= _fireDefence;
            }
            else if (info.type == ElementalInfo.Type.fire)
            {
                amount += _waterDefence;
                amount -= _earthDefence;
            }

            _regenerationCountdown = regenerationCountdown;
            _regenerationTimer = regenerationTimer;
        }

        _health += amount;
    }

    public void CheckDefence()
    {
        _neutralDefence = 0;
        _earthDefence = 0;
        _airDefence = 0;
        _thunderDefence = 0;
        _waterDefence = 0;
        _fireDefence = 0;

        foreach (ElementalInfo elementalInfo in elementalDefence)
        {
            if (elementalInfo.type == ElementalInfo.Type.neutral)
                _neutralDefence += elementalInfo.value;
            else if (elementalInfo.type == ElementalInfo.Type.earth)
                _earthDefence += elementalInfo.value;
            else if (elementalInfo.type == ElementalInfo.Type.air)
                _airDefence += elementalInfo.value;
            else if (elementalInfo.type == ElementalInfo.Type.thunder)
                _thunderDefence += elementalInfo.value;
            else if (elementalInfo.type == ElementalInfo.Type.water)
                _waterDefence += elementalInfo.value;
            else if (elementalInfo.type == ElementalInfo.Type.fire)
                _fireDefence += elementalInfo.value;
        }
    }

    private void StaminaReplenishment()
    {
        if (_replenishmentCountdown > 0)
        {
            _replenishmentCountdown -= Time.deltaTime;
        }
        else
        {
            if (_replenishmentTimer > 0)
            {
                _replenishmentTimer -= Time.deltaTime;
            }
            else
            {
                _replenishmentTimer = replenishmentTimer;
                if (_stamina < stamina)
                    _stamina += 1;
                else if (_exhausted)
                    _exhausted = false;
            }
        }
    }

    public void ChangeStamina(float amount)
    {
        if (amount < 0)
        {
            _replenishmentCountdown = replenishmentCountdown;
        }

        _stamina += amount;
        if (_stamina < 0)
            _exhausted = true;
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
