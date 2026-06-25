using UnityEngine;

public class SelectedBlockDisplay : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction;

    private void Awake()
    {
        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
        }
    }

    private void OnGUI()
    {
        if (playerInteraction == null)
        {
            return;
        }

        GUI.Label(
            new Rect(20f, 20f, 250f, 30f),
            $"Selected: {playerInteraction.SelectedBlock}"
        );
    }
}