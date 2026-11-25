using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disimpan di root desa hasil "Village Builder".
/// Menyimpan TerrainData asli & objek yang dinonaktifkan supaya desa bisa
/// di-clear / di-rebuild tanpa merusak terrain asli.
/// </summary>
[DisallowMultipleComponent]
public class VillageBuildRecord : MonoBehaviour
{
    public List<Terrain> terrains = new List<Terrain>();
    public List<TerrainData> originalTerrainData = new List<TerrainData>();
    public List<GameObject> disabledObjects = new List<GameObject>();

    public Vector3 villageCenter;
    public float villageYaw;
    public float baseHeight;

    public bool playerMoved;
    public Vector3 playerOriginalPosition;
    public Quaternion playerOriginalRotation = Quaternion.identity;

    public List<Transform> boundaryTransforms = new List<Transform>();
    public List<Vector3> originalBoundaryPositions = new List<Vector3>();
    public List<Vector3> originalBoundaryScales = new List<Vector3>();
}
