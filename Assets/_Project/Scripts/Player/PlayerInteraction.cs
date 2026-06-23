using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 12f;
    [SerializeField] private BlockType blockToPlace = BlockType.Dirt;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
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
}
