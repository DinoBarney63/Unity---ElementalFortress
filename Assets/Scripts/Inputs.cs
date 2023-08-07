using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool use1;
	public bool use2;
	public bool jump;
	public bool sprint;
	public bool perspective;
	public bool menuOpenClose;

	private InputAction _moveAction;
	private InputAction _lookAction;
	private InputAction _use1Action;
	private InputAction _use2Action;
	private InputAction _jumpAction;
	private InputAction _sprintAction;
	private InputAction _perspectiveAction;
	private InputAction _menuOpenCloseAction;

	[Header("Movement Settings")]
	[SerializeField]
	private bool analogMovement;

	[Header("Mouse Cursor Settings")]
	[SerializeField]
	private bool cursorLocked = true;

	private PlayerInput _playerInput;

    private void Start()
    {
		_playerInput = GetComponent<PlayerInput>();

		_moveAction = _playerInput.actions["Move"];
		_lookAction = _playerInput.actions["Look"];
		_use1Action = _playerInput.actions["Use1"];
		_use2Action = _playerInput.actions["Use2"];
		_jumpAction = _playerInput.actions["Jump"];
		_sprintAction = _playerInput.actions["Sprint"];
		_perspectiveAction = _playerInput.actions["Perspective"];
		_menuOpenCloseAction = _playerInput.actions["MenuOpenClose"];
    }

    private void Update()
    {
		move = _moveAction.ReadValue<Vector2>();
		look = _lookAction.ReadValue<Vector2>();
		use1 = _use1Action.WasPressedThisFrame();
		use2 = _use2Action.WasPressedThisFrame();
		jump = _jumpAction.IsPressed();
		sprint = _sprintAction.IsPressed();
		perspective = _perspectiveAction.WasPressedThisFrame();
		menuOpenClose = _menuOpenCloseAction.WasPressedThisFrame();
    }

	public bool IsAnalog()
    {
		return analogMovement;
    }

	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(cursorLocked);
	}

	public void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}
}
