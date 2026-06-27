using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 12f;
    [SerializeField] private BlockType blockToPlace = BlockType.Dirt;
    public BlockType SelectedBlock => blockToPlace;
    [SerializeField] private CharacterController characterController;

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

        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }
        
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
        if (!TryGetTargetBlockPosition(out Vector3Int blockPosition))
        {
            return;
        }

        BlockType blockType = world.GetBlock(blockPosition);
        BlockData blockData = BlockDatabase.GetBlockData(blockType);

        if (!blockData.isBreakable)
        {
            return;
        }

        world.SetBlock(blockPosition, BlockType.Air);
    }

    private void PlaceBlock()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Vector3 blockPoint = hit.point + hit.normal * 0.01f;
            Vector3Int blockPosition = Vector3Int.FloorToInt(blockPoint);
            if (IsBlockOverlappingPlayer(blockPosition))
            {
                return;
            }
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

        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (scrollY > 0f)
        {
            SelectNextBlock(1);
        }
        else if (scrollY < 0f)
        {
            SelectNextBlock(-1);
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
    }

    private void SelectNextBlock(int direction)
    {
        if (placeableBlocks.Length == 0)
        {
            return;
        }

        int newIndex = selectedBlockIndex + direction;

        if (newIndex >= placeableBlocks.Length)
        {
            newIndex = 0;
        }
        else if (newIndex < 0)
        {
            newIndex = placeableBlocks.Length - 1;
        }

        SelectBlock(newIndex);
    }

    private bool IsBlockOverlappingPlayer(Vector3Int blockPosition)
    {
        if (characterController == null)
        {
            return false;
        }

        Bounds blockBounds = new Bounds(
            blockPosition + Vector3.one * 0.5f,
            Vector3.one
        );

        return characterController.bounds.Intersects(blockBounds);
    }

    public bool TryGetTargetBlockPosition(out Vector3Int blockPosition)
    {
        blockPosition = default;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return false;
        }

        if (playerCamera == null)
        {
            return false;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            return false;
        }

        Vector3 blockPoint = hit.point - hit.normal * 0.01f;
        blockPosition = Vector3Int.FloorToInt(blockPoint);

        return true;
    }
}
