using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    [SerializeField]
    Tilemap tilemap;
    [SerializeField]
    Vector3Int offset;

    public void SetTile(Vector3Int pos, Module tile)
    {
        Vector3Int drawPos = pos + offset;
        tilemap.SetTile(drawPos,tile.tile);
    }
}
