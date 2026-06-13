using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private byte[] voxelMap = new byte[VoxelData.ChunkVoxelCount];

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private List<Vector2> uvs = new List<Vector2>();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        PopulateVoxelMap();
        GenerateMesh();
    }

    private int GetVoxelIndex(int x, int y, int z)
    {
        return x + VoxelData.ChunkWidth * (y + VoxelData.ChunkHeight * z);
    }

    private void SetVoxel(int x, int y, int z, BlockType blockType)
    {
        int index = GetVoxelIndex(x, y, z);
        voxelMap[index] = (byte)blockType;
    }

    private BlockType GetVoxel(int x, int y, int z)
    {
        int index = GetVoxelIndex(x, y, z);
        return (BlockType)voxelMap[index];
    }

    private void PopulateVoxelMap()
    {
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.ChunkWidth; z++)
            {
                SetVoxel(x, 0, z, BlockType.Bedrock);

                SetVoxel(x, 1, z, BlockType.Dirt);
                SetVoxel(x, 2, z, BlockType.Dirt);
                SetVoxel(x, 3, z, BlockType.Dirt);

                SetVoxel(x, 4, z, BlockType.Grass);
            }
        }
    }

    private void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        int solidBlockCount = 0;
        int visibleFaceCount = 0;

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (GetVoxel(x, y, z) != BlockType.Air)
                    {
                        solidBlockCount++;
                        for (int faceIndex = 0; faceIndex < VoxelData.FaceChecks.Length; faceIndex++)
                        {
                            Vector3Int neighborOffset = VoxelData.FaceChecks[faceIndex];

                            int neighborX = x + neighborOffset.x;
                            int neighborY = y + neighborOffset.y;
                            int neighborZ = z + neighborOffset.z;

                            if (!CheckVoxel(neighborX, neighborY, neighborZ))
                            {
                                visibleFaceCount++;
                                AddFace(x, y, z, faceIndex);
                            }
                        }

                    }
                }
            }
        }

        Debug.Log(solidBlockCount);
        Debug.Log(visibleFaceCount);
        Debug.Log(vertices.Count);
        Debug.Log(triangles.Count);
        Debug.Log(uvs.Count);

        ApplyMesh();
    }

    private bool IsVoxelInsideChunk(int x, int y, int z)
    {
        return x >= 0 && x < VoxelData.ChunkWidth && y >= 0 && y < VoxelData.ChunkHeight && z >= 0 && z < VoxelData.ChunkWidth;
    }

    private bool CheckVoxel(int x, int y, int z)
    {
        if (!IsVoxelInsideChunk(x, y, z))
        {
            return false;
        }
        
        BlockType blockType = GetVoxel(x, y, z);
        BlockData blockData = BlockDatabase.GetBlockData(blockType);
        return blockData.isSolid;
    }

    private void AddFace(int x, int y, int z, int faceIndex)
    {
        int vertexIndex = vertices.Count;
        Vector3 blockPosition = new Vector3(x, y, z);
        for (int i = 0; i < 4; i++)
        {
            int cubeVertexIndex = VoxelData.VoxelTriangles[faceIndex, i];
            Vector3 cubeVertexPosition = VoxelData.VoxelVertices[cubeVertexIndex];
            Vector3 finalVertexPosition = blockPosition + cubeVertexPosition;
            vertices.Add(finalVertexPosition);
        }
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 3);

        BlockType blockType = GetVoxel(x, y, z);
        BlockData blockData = BlockDatabase.GetBlockData(blockType);
        Vector2Int textureTile = blockData.GetTextureForFace(faceIndex);

        float textureSize = VoxelData.NormalizedBlockTextureSize;
        float u = textureTile.x * textureSize;
        float v = textureTile.y * textureSize;

        uvs.Add(new Vector2(u, v));
        uvs.Add(new Vector2(u, v + textureSize));
        uvs.Add(new Vector2(u + textureSize, v));
        uvs.Add(new Vector2(u + textureSize, v + textureSize));
    }
    
    private void ApplyMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
        };

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
}
