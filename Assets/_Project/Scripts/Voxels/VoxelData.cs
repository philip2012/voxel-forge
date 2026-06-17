// file: Assets/_Project/Scripts/Voxels/VoxelData.cs
using UnityEngine;

public static class VoxelData
{
    public const int ChunkWidth = 16;
    public const int ChunkHeight = 16;

    // Total number of block slots inside one chunk.
    public const int ChunkVoxelCount = ChunkWidth * ChunkHeight * ChunkWidth;

    // formula to convert 3d block into a index for 1d array
    // index = x + ChunkWidth * (y + ChunkHeight * z)

    public const int TextureAtlasSizeInBlocks = 16;
    public const float NormalizedBlockTextureSize = 1f / TextureAtlasSizeInBlocks;

    public const float TexturePadding = 0.002f;
    
    public static readonly Vector3[] VoxelVertices = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1),
    };

    public static readonly Vector3Int[] FaceChecks = new Vector3Int[]
    {
        new Vector3Int(0, 0, -1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
    };

    public static readonly int[,] VoxelTriangles = new int[6,4]
    {
        {0, 3, 1, 2},
        {5, 6, 4, 7},
        {3, 7, 2, 6},
        {1, 5, 0, 4},
        {4, 7, 0, 3},
        {1, 2, 5, 6},
    };
}