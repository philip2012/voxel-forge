using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsOverlay : MonoBehaviour
{
    [SerializeField] private bool showControls;
    [SerializeField] private float width = 360f;
    [SerializeField] private float height = 300f;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            showControls = !showControls;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20f, Screen.height - 35f, 220f, 25f), "F1: Controls");

        if (!showControls)
        {
            return;
        }

        Rect panelRect = new Rect(
            20f,
            20f,
            width,
            height
        );

        GUI.Box(panelRect, "Voxel Forge Controls");

        float x = panelRect.x + 20f;
        float y = panelRect.y + 35f;
        float lineHeight = 24f;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "WASD: Move");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Mouse: Look around");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Space: Jump");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Left Shift: Sprint");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Left Click: Break block");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Right Click: Place block");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "1-5 / Scroll: Select block");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "Esc: Unlock cursor");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "F9: Clear saved world");
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - 40f, lineHeight), "F5: Save world");
    }
}