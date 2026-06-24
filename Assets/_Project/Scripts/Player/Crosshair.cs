using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private int size = 12;
    [SerializeField] private int thickness = 2;

    private void OnGUI()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        Rect horizontal = new Rect(centerX - size / 2f, centerY - thickness / 2f, size, thickness);
        Rect vertical = new Rect(centerX - thickness / 2f, centerY - size / 2f, thickness, size);

        GUI.Box(horizontal, GUIContent.none);
        GUI.Box(vertical, GUIContent.none);
    }
}