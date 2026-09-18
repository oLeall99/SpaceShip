using UnityEngine;

public class InitialSceneController : MonoBehaviour
{
    private void Update()
    {
        if (WasAnyKeyPressed())
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGameFromInitial();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("fase_grass");
            }
        }
    }

    private bool WasAnyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame) return true;
        }
#endif
        try
        {
            if (Input.anyKeyDown) return true;
        }
        catch { }

        return false;
    }
}
