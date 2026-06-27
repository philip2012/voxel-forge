using UnityEngine;

public class BlockHighlighter : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float outlinePadding = 0.01f;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private Material lineMaterial;

    private LineRenderer[] lines;
    private Vector3Int currentBlockPosition;
    private bool hasCurrentBlock;

    private static readonly int[,] Edges =
    {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
        { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
        { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
    };

    private void Awake()
    {
        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        CreateLines();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (playerInteraction == null)
        {
            SetVisible(false);
            return;
        }

        if (!playerInteraction.TryGetTargetBlockPosition(out Vector3Int blockPosition))
        {
            hasCurrentBlock = false;
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (!hasCurrentBlock || currentBlockPosition != blockPosition)
        {
            currentBlockPosition = blockPosition;
            hasCurrentBlock = true;
            UpdateOutline(blockPosition);
        }
    }

    private void CreateLines()
    {
        lines = new LineRenderer[12];

        Material materialToUse = lineMaterial;

        if (materialToUse == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            materialToUse = new Material(shader);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            GameObject lineObject = new GameObject($"Block Outline Edge {i}");
            lineObject.transform.SetParent(transform);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = outlineColor;
            line.endColor = outlineColor;
            line.material = materialToUse;

            lines[i] = line;
        }
    }

    private void UpdateOutline(Vector3Int blockPosition)
    {
        Vector3 min = new Vector3(blockPosition.x, blockPosition.y, blockPosition.z);
        Vector3 max = min + Vector3.one;

        min -= Vector3.one * outlinePadding;
        max += Vector3.one * outlinePadding;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, min.z),

            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z)
        };

        for (int i = 0; i < lines.Length; i++)
        {
            int startCorner = Edges[i, 0];
            int endCorner = Edges[i, 1];

            lines[i].SetPosition(0, corners[startCorner]);
            lines[i].SetPosition(1, corners[endCorner]);
        }
    }

    private void SetVisible(bool visible)
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].enabled = visible;
        }
    }
}