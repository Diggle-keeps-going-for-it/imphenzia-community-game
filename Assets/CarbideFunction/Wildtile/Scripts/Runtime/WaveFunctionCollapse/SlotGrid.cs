using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Contains a map that is processable by the Wildtile wave function collapse algorithm.
///
/// Before collapse, the slots in the map may contain multiple TransformedModules.
/// After collapse, all slots will either contain exactly one TransformedModule or be marked as a wildcard.
/// </summary>
public class SlotGrid
{
    /// <summary>
    /// Create a SlotGrid manually. The caller must set up slot connections separately.
    /// </summary>
    static public SlotGrid CreateGridManually(Slot[] slots)
    {
        var grid = new SlotGrid{
            slotData = (Slot[])slots.Clone(),
        };

        return grid;
    }

    private Slot[] slotData;
    /// <summary>
    /// Readonly access to the Slots in their native form.
    /// </summary>
    public Slot[] SlotData => slotData;

    /// <summary>
    /// Get the <see cref="Slot"/> from a coordinate.
    /// </summary>
    public Slot GetSlot(int x, int y, int z, Vector3Int dimensions)
    {
        return GetSlot(new Vector3Int(x, y, z), dimensions);
    }

    /// <summary>
    /// Get the <see cref="Slot"/> from a coordinate.
    /// </summary>
    public Slot GetSlot(Vector3Int coord, Vector3Int dimensions)
    {
        if (IsWithinBounds(coord, dimensions))
        {
            var slotIndex = coord.ToFlatArrayIndex(dimensions);
            return slotData[slotIndex];
        }

        return null;
    }

    /// <summary>
    /// Test whether a coordinate can access a Slot in this SlotGrid.
    /// </summary>
    public static bool IsWithinBounds(Vector3Int coord, Vector3Int dimensions)
    {
        return coord.x >= 0 && coord.x < dimensions.x
            && coord.y >= 0 && coord.y < dimensions.y
            && coord.z >= 0 && coord.z < dimensions.z;
    }

}

}
