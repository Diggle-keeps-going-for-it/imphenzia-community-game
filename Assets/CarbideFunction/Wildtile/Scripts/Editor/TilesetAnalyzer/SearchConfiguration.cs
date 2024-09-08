using System;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Class representing a single marching cube to search for, and an example mesh that shows which corners this marching cube would cover.
/// </summary>
[Serializable]
internal class SearchConfiguration
{
    public int marchingCubeConfig;
    public Mesh representativeModel;
}

}
