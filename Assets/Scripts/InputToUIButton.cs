using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputToUIButton : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Image buttonImage;

    [SerializeField] private Color idleColor;
    [SerializeField] private Color pressedColor;
    [SerializeField] private string inputName = "Button 1";

    void Update()
    {
        if (playerInput.actions[inputName].WasPressedThisFrame())
        {
            buttonImage.color = pressedColor;
        }

        if (playerInput.actions[inputName].WasReleasedThisFrame())
        {
            buttonImage.color = idleColor;
        }
    }
}
