// file: Assets/_Project/Scripts/Voxels/BlockDatabase.cs

using UnityEngine;

public static class BlockDatabase
{
    public static readonly BlockData[] Blocks = new BlockData[]
    {
        new() {
            name = "Air",
            isSolid = false,
            isBreakable = false,
            topTexture = new Vector2Int(0, 0),
            bottomTexture = new Vector2Int(0, 0),
            sideTexture = new Vector2Int(0, 0),
        },
        new() {
            name = "Bedrock",
            isSolid = true,
            isBreakable = false,
            topTexture = new Vector2Int(1, 0),
            bottomTexture = new Vector2Int(1, 0),
            sideTexture = new Vector2Int(1, 0),
        },
        new() {
            name = "Stone",
            isSolid = true,
            isBreakable = true,
            topTexture = new Vector2Int(2, 0),
            bottomTexture = new Vector2Int(2, 0),
            sideTexture = new Vector2Int(2, 0),
        },
        new() {
            name = "Dirt",
            isSolid = true,
            isBreakable = true,
            topTexture = new Vector2Int(3, 0),
            bottomTexture = new Vector2Int(3, 0),
            sideTexture = new Vector2Int(3, 0),
        },
        new() {
            name = "Grass",
            isSolid = true,
            isBreakable = true,
            topTexture = new Vector2Int(4, 0),
            bottomTexture = new Vector2Int(3, 0),
            sideTexture = new Vector2Int(5, 0),
        },
        new() {
            name = "Wood",
            isSolid = true,
            isBreakable = true,
            topTexture = new Vector2Int(6, 0),
            bottomTexture = new Vector2Int(6, 0),
            sideTexture = new Vector2Int(7, 0),
        },
        new() {
            name = "Leaves",
            isSolid = true,
            isBreakable = true,
            topTexture = new Vector2Int(8, 0),
            bottomTexture = new Vector2Int(8, 0),
            sideTexture = new Vector2Int(8, 0),
        }
    };

    public static BlockData GetBlockData(BlockType blockType)
    {
        return Blocks[(int)blockType];
    }
}