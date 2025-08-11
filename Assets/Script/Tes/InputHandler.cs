using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Collections;

public class InputHandler : MonoBehaviour
{
    public PlayerInput inputActions;     // Drag komponen PlayerInput ke sini lewat Inspector
    public string actionName = "Tes";    // Nama action di Input Actions
    public GameObject targetObject;      // GameObject yang akan diaktifkan

    private InputAction tesAction;
    private bool isTriggered = false;
    private bool hasActivatedObject = false;
    private Coroutine resetCoroutine;

    void OnEnable()
    {
        if (inputActions != null)
        {
            // Ambil action berdasarkan nama dari PlayerInput
            tesAction = inputActions.actions[actionName];
            if (tesAction != null)
            {
                tesAction.performed += OnTesPerformed;
                tesAction.Enable();
            }
            else
            {
                Debug.LogError($"Action '{actionName}' tidak ditemukan!");
            }
        }
    }

    void OnDisable()
    {
        if (tesAction != null)
        {
            tesAction.performed -= OnTesPerformed;
            tesAction.Disable();
        }
    }

    private void OnTesPerformed(InputAction.CallbackContext context)
    {
        if (!isTriggered)
        {
            isTriggered = true;
            Debug.Log("true");

            if (!hasActivatedObject && targetObject != null)
            {
                targetObject.SetActive(true);
                hasActivatedObject = true;
                Debug.Log("GameObject activated!");
            }

            if (resetCoroutine != null)
                StopCoroutine(resetCoroutine);

            resetCoroutine = StartCoroutine(ResetAfterDelay(5f));
        }
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isTriggered = false;
        Debug.Log("Reset (false)");
    }
}
