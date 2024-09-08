using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbideFunction.Wildtile;

public static class VoxelFaceFinder
{
    public static VoxelMesher.FaceData? GetFaceForClick(Camera camera, Vector2 clickScreenCoordinates, VoxelMesher voxelMesher)
    {
        var hit = GetHitUnderCursor(camera, clickScreenCoordinates, voxelMesher);
        if (hit.HasValue)
        {
            return voxelMesher.GetFaceDataForTriIndex(hit.Value.triangleIndex);
        }
        else
        {
            return null;
        }
    }

    private static RaycastHit? GetHitUnderCursor(Camera camera, Vector2 cursorPosition, VoxelMesher voxelMesher)
    {
        var ray = camera.ScreenPointToRay(cursorPosition);
        var hits = Physics.RaycastAll(ray);
        var voxelHit = hits.Select((hit, index) => new {hit, index}).FirstOrDefault(hit => hit.hit.collider.transform == voxelMesher.TempMeshInstance.transform);
        return voxelHit?.hit;
    }
}
