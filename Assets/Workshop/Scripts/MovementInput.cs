using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementInput : MonoBehaviour
{
    public InputActionAsset InputActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction pausePlayer;
    private InputAction pauseUI;

    private Vector2 moveAmt;
    private Rigidbody rb;

    public float MoveSpeed = 5f;
    public float JumpStrength = 5f;

    public GameObject PauseMenu;

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        pausePlayer = InputSystem.actions.FindAction("Player/Pause");
        pauseUI = InputSystem.actions.FindAction("UI/Pause");

        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveAmt = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame())
        {
            Jump();
        }

        DisplayPause();
    }

    public void Jump()
    {
        rb.AddForceAtPosition(new Vector3(0, JumpStrength, 0), Vector3.up, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        rb.MovePosition(rb.position + transform.forward * moveAmt.y * MoveSpeed * Time.deltaTime);
    }

    private void DisplayPause()
    {
        if (pausePlayer.WasPressedThisFrame())
        {
            PauseMenu.SetActive(true);
            InputActions.FindActionMap("Player").Disable();
            InputActions.FindActionMap("UI").Enable();
        }
        else if (pauseUI.WasPressedThisFrame())
        {
            PauseMenu.SetActive(false);
            InputActions.FindActionMap("UI").Disable();
            InputActions.FindActionMap("Player").Enable();
        }
    }
}
