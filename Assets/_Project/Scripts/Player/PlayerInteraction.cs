using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 12f;
    [SerializeField] private BlockType blockToPlace = BlockType.Dirt;

    [SerializeField] private BlockType[] placeableBlocks =
    {
        BlockType.Dirt,
        BlockType.Stone,
        BlockType.Grass,
        BlockType.Wood,
        BlockType.Leaves
    };

    private int selectedBlockIndex;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleBlockSelection();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BreakBlock();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            PlaceBlock();
        }
    }

    private void BreakBlock()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Vector3 blockPoint = hit.point - hit.normal * 0.01f;
            Vector3Int blockPosition = Vector3Int.FloorToInt(blockPoint);
            world.SetBlock(blockPosition, BlockType.Air);
        }
    }

    private void PlaceBlock()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Vector3 blockPoint = hit.point + hit.normal * 0.01f;
            Vector3Int blockPosition = Vector3Int.FloorToInt(blockPoint);
            world.SetBlock(blockPosition, blockToPlace);
        }
    }

    private void HandleBlockSelection()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SelectBlock(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SelectBlock(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SelectBlock(2);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            SelectBlock(3);
        }
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            SelectBlock(4);
        }
    }

    private void SelectBlock(int index)
    {
        if (index < 0 || index >= placeableBlocks.Length)
        {
            return;
        }

        selectedBlockIndex = index;
        blockToPlace = placeableBlocks[selectedBlockIndex];

        Debug.Log($"Selected block: {blockToPlace}");
    }
}
