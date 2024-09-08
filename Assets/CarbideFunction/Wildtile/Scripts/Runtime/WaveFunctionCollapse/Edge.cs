using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

internal class Edge
{
    public Vector2 start;
    public Vector3 startNormal;
    public Vector2 end;
    public Vector3 endNormal;
    public Material material;

    public Edge RotatedAboutZ(int yawIndexRaw)
    {
        var yawIndex = yawIndexRaw.PositiveModulo(4);
        var rotator3d = rotators3d[yawIndex];

        return new Edge{
            start = Rotate(yawIndex, start),
            startNormal = rotator3d.MultiplyVector(startNormal),

            end = Rotate(yawIndex, end),
            endNormal = rotator3d.MultiplyVector(endNormal),

            material = material,
        };
    }

    public Edge FlippedAcrossX()
    {
        return FlippedBy(new Vector2(-1f, 1f), new Vector3(-1f, 1f, 1f));
    }

    private Edge FlippedBy(Vector2 flipper2d, Vector3 flipper3d)
    {
        return new Edge{
            start = Vector2.Scale(end, flipper2d),
            startNormal = Vector3.Scale(endNormal, flipper3d),

            end = Vector2.Scale(start, flipper2d),
            endNormal = Vector3.Scale(startNormal, flipper3d),

            material = material,
        };
    }

    private static Vector2[] xRotators2d = new[]
    {
        Vector2.right,
        Vector2.up,
        Vector2.left,
        Vector2.down,
    };
    private static Vector2[] yRotators2d = new[]
    {
        Vector2.up,
        Vector2.left,
        Vector2.down,
        Vector2.right,
    };
    private static Matrix4x4[] rotators3d = new[]
    {
        Matrix4x4.identity,
        rotate90AroundZ,
        rotate90AroundZ * rotate90AroundZ,
        rotate90AroundZ * rotate90AroundZ * rotate90AroundZ,
    };
    private static Matrix4x4 rotate90AroundZ
    {
        get {
            var result = Matrix4x4.zero;
            result[1,0] = -1f;
            result[0,1] = 1f;
            result[2,2] = 1f;
            result[3,3] = 1f;
            return result;
        }
    }

    private static Vector2 Rotate(int yawIndex, Vector2 input)
    {
        Assert.IsTrue(yawIndex >= 0);
        Assert.IsTrue(yawIndex < 4);
        Vector2 rotatorX = xRotators2d[yawIndex];
        Vector2 rotatorY = yRotators2d[yawIndex];
        return new Vector2(Vector2.Dot(rotatorX, input), Vector2.Dot(rotatorY, input));
    }

    public override string ToString()
    {
        return $"{start} ({startNormal}) -> {end} ({endNormal}), mat: {material?.name ?? "<none>"}";
    }
}

}
