using UnityEngine;

public class HotbarDisplay : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private float slotSize = 80f;
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private float bottomOffset = 30f;

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

        int blockCount = playerInteraction.PlaceableBlockCount;

        if (blockCount == 0)
        {
            return;
        }

        float totalWidth = blockCount * slotSize + (blockCount - 1) * slotSpacing;
        float startX = Screen.width / 2f - totalWidth / 2f;
        float y = Screen.height - slotSize - bottomOffset;

        for (int i = 0; i < blockCount; i++)
        {
            float x = startX + i * (slotSize + slotSpacing);
            Rect slotRect = new Rect(x, y, slotSize, slotSize);

            bool isSelected = i == playerInteraction.SelectedBlockIndex;

            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            GUI.Box(slotRect, "");

            GUI.backgroundColor = previousBackgroundColor;

            BlockType blockType = playerInteraction.GetPlaceableBlock(i);

            GUI.Label(
                new Rect(x + 8f, y + 6f, slotSize - 16f, 20f),
                $"{i + 1}"
            );

            GUI.Label(
                new Rect(x + 8f, y + 32f, slotSize - 16f, 40f),
                blockType.ToString()
            );
        }
    }
}