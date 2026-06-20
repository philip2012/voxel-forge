using UnityEngine;

public class World: MonoBehaviour
{
    [SerializeField] private Chunk chunkPrefab;

    private void Start()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                SpawnChunk(new Vector2Int(x, z));
            }
        }
    }

    private Chunk SpawnChunk(Vector2Int chunkCoordinate)
    {
        Vector3 worldPosition = new Vector3(
            chunkCoordinate.x * VoxelData.ChunkWidth,
            0,
            chunkCoordinate.y * VoxelData.ChunkWidth
        );

        Chunk chunk = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, transform);
        chunk.name = $"Chunk ({chunkCoordinate.x}, {chunkCoordinate.y})";

        return chunk;
    }
}