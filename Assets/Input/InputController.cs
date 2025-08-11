using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private StikInput inputActions;

    void Awake()
    {
        inputActions = new StikInput();

        // Binding input dari dua action map
        inputActions.PlayerStik.Accelerate.performed += ctx => Debug.Log("Gamepad Accelerate");
        inputActions.MozaR3.Accelerate.performed += ctx => Debug.Log("Wheel Accelerate");
    }

    void OnEnable()
    {
        inputActions.PlayerStik.Enable();
        inputActions.MozaR3.Enable();
    }

    void OnDisable()
    {
        inputActions.PlayerStik.Disable();
        inputActions.MozaR3.Disable();
    }
}
