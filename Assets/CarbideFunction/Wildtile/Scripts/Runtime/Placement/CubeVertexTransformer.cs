using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
internal struct CubeVertexTransformer
{
    public CubeVertexTransformer(
        Vector3 v000, Vector3 x000, Vector3 y000, Vector3 z000,
        Vector3 v001, Vector3 x001, Vector3 y001, Vector3 z001,
        Vector3 v010, Vector3 x010, Vector3 y010, Vector3 z010,
        Vector3 v011, Vector3 x011, Vector3 y011, Vector3 z011,
        Vector3 v100, Vector3 x100, Vector3 y100, Vector3 z100,
        Vector3 v101, Vector3 x101, Vector3 y101, Vector3 z101,
        Vector3 v110, Vector3 x110, Vector3 y110, Vector3 z110,
        Vector3 v111, Vector3 x111, Vector3 y111, Vector3 z111
    )
    {
        this.v000 = v000;
        this.x000 = x000;
        this.y000 = y000;
        this.z000 = z000;
        this.v001 = v001;
        this.x001 = x001;
        this.y001 = y001;
        this.z001 = z001;
        this.v010 = v010;
        this.x010 = x010;
        this.y010 = y010;
        this.z010 = z010;
        this.v011 = v011;
        this.x011 = x011;
        this.y011 = y011;
        this.z011 = z011;
        this.v100 = v100;
        this.x100 = x100;
        this.y100 = y100;
        this.z100 = z100;
        this.v101 = v101;
        this.x101 = x101;
        this.y101 = y101;
        this.z101 = z101;
        this.v110 = v110;
        this.x110 = x110;
        this.y110 = y110;
        this.z110 = z110;
        this.v111 = v111;
        this.x111 = x111;
        this.y111 = y111;
        this.z111 = z111;
    }

    public void FlipVertices()
    {
        (v000, v001) = (v001, v000);
        (x000, x001) = (-x001, -x000);
        (y000, y001) = (y001, y000);
        (z000, z001) = (z001, z000);

        (v010, v011) = (v011, v010);
        (x010, x011) = (-x011, -x010);
        (y010, y011) = (y011, y010);
        (z010, z011) = (z011, z010);

        (v100, v101) = (v101, v100);
        (x100, x101) = (-x101, -x100);
        (y100, y101) = (y101, y100);
        (z100, z101) = (z101, z100);

        (v110, v111) = (v111, v110);
        (x110, x111) = (-x111, -x110);
        (y110, y111) = (y111, y110);
        (z110, z111) = (z111, z110);
    }

    public void RotateVertices(int yawIndex)
    {
        var within4YawIndex = yawIndex.PositiveModulo(4);

        Assert.IsTrue(within4YawIndex >= 0);
        Assert.IsTrue(within4YawIndex < 4);

        switch(within4YawIndex)
        {
            case 0:
                {
                    // leave vertices as they are
                    break;
                }
            case 1:
                {
                    (v000, v001, v101, v100) = ( v100, v000, v001, v101);
                    (x000, x001, x101, x100, z000, z001, z101, z100) = (-z100,-z000,-z001,-z101, x100, x000, x001, x101);
                    (y000, y001, y101, y100) = ( y100, y000, y001, y101);

                    (v010, v011, v111, v110) = ( v110, v010, v011, v111);
                    (x010, x011, x111, x110, z010, z011, z111, z110) = (-z110,-z010,-z011,-z111, x110, x010, x011, x111);
                    (y010, y011, y111, y110) = ( y110, y010, y011, y111);
                    break;
                }
            case 2:
                {
                    (v000, v001, v100, v101) = (v101, v100, v001, v000);
                    (x000, x001, x100, x101, z000, z001, z100, z101) = (-x101,-x100,-x001,-x000,-z101,-z100,-z001,-z000);
                    (y000, y001, y100, y101) = (y101, y100, y001, y000);

                    (v010, v011, v110, v111) = (v111, v110, v011, v010);
                    (x010, x011, x110, x111, z010, z011, z110, z111) = (-x111,-x110,-x011,-x010,-z111,-z110,-z011,-z010);
                    (y010, y011, y110, y111) = (y111, y110, y011, y010);
                    break;
                }
            case 3:
                {
                    (v000, v001, v101, v100) = (v001, v101, v100, v000);
                    (x000, x001, x101, x100, z000, z001, z101, z100) = (z001, z101, z100, z000,-x001,-x101,-x100,-x000);
                    (y000, y001, y101, y100) = (y001, y101, y100, y000);

                    (v010, v011, v111, v110) = (v011, v111, v110, v010);
                    (x010, x011, x111, x110, z010, z011, z111, z110) = (z011, z111, z110, z010,-x011,-x111,-x110,-x010);
                    (y010, y011, y111, y110) = (y011, y111, y110, y010);
                    break;
                }
            default:
                {
                    Assert.IsTrue(false, $"Unhandled yaw index {within4YawIndex} (originally {yawIndex})");
                    break;
                }
        }
    }

    Vector3 v000;
    Vector3 x000;
    Vector3 y000;
    Vector3 z000;

    Vector3 v001;
    Vector3 x001;
    Vector3 y001;
    Vector3 z001;

    Vector3 v010;
    Vector3 x010;
    Vector3 y010;
    Vector3 z010;

    Vector3 v011;
    Vector3 x011;
    Vector3 y011;
    Vector3 z011;

    Vector3 v100;
    Vector3 x100;
    Vector3 y100;
    Vector3 z100;

    Vector3 v101;
    Vector3 x101;
    Vector3 y101;
    Vector3 z101;

    Vector3 v110;
    Vector3 x110;
    Vector3 y110;
    Vector3 z110;

    Vector3 v111;
    Vector3 x111;
    Vector3 y111;
    Vector3 z111;

    public Vector3 V000 => v000;
    public Vector3 NormalX000 => x000;
    public Vector3 NormalY000 => y000;
    public Vector3 NormalZ000 => z000;

    public Vector3 V001 => v001;
    public Vector3 NormalX001 => x001;
    public Vector3 NormalY001 => y001;
    public Vector3 NormalZ001 => z001;

    public Vector3 V010 => v010;
    public Vector3 NormalX010 => x010;
    public Vector3 NormalY010 => y010;
    public Vector3 NormalZ010 => z010;

    public Vector3 V011 => v011;
    public Vector3 NormalX011 => x011;
    public Vector3 NormalY011 => y011;
    public Vector3 NormalZ011 => z011;

    public Vector3 V100 => v100;
    public Vector3 NormalX100 => x100;
    public Vector3 NormalY100 => y100;
    public Vector3 NormalZ100 => z100;

    public Vector3 V101 => v101;
    public Vector3 NormalX101 => x101;
    public Vector3 NormalY101 => y101;
    public Vector3 NormalZ101 => z101;

    public Vector3 V110 => v110;
    public Vector3 NormalX110 => x110;
    public Vector3 NormalY110 => y110;
    public Vector3 NormalZ110 => z110;

    public Vector3 V111 => v111;
    public Vector3 NormalX111 => x111;
    public Vector3 NormalY111 => y111;
    public Vector3 NormalZ111 => z111;
}
}
