using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObstacleKind
{
    Crate,
    Pillar
}

[Serializable]
public struct ObstacleSpec
{
    public Vector2 center;
    public Vector2 size;
    public ObstacleKind kind;
}

/// <summary>
/// A hand-authored (or generated) arena: its size and the cover placed inside it.
/// The arena is always centred on the world origin.
/// </summary>
[CreateAssetMenu(fileName = "NewRoomLayout", menuName = "Exterminator/Room Layout")]
public class RoomLayoutData : ScriptableObject
{
    public string displayName = "Warehouse";
    public Vector2 size = new Vector2(24f, 18f);
    public bool bossArena;
    public Vector2 playerStart = new Vector2(0f, -6f);
    public List<ObstacleSpec> obstacles = new List<ObstacleSpec>();

    public Rect Bounds => new Rect(-size * 0.5f, size);
}
