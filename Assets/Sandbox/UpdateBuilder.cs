using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor.AI;
#endif

public class UpdateBuilder : MonoBehaviour
{
    NavMeshData m_NavMeshData;
    NavMeshDataInstance m_Handle;
    NavMeshCollectGeometry m_UseGeometry = NavMeshCollectGeometry.PhysicsColliders;
    Bounds rasterizationBounds = new Bounds(Vector3.zero, Vector3.zero);
    Bounds collectionBounds = new Bounds(Vector3.zero, Vector3.zero);
    UnityEngine.AsyncOperation asyncHandle;

#if UNITY_EDITOR
    NavMeshBuildDebugSettings m_Debug;
#endif

    public float volumeSize = 10.0f;
    [Range(-1, 7)]
    public int debugGroup = -1;
    public bool showInputGeometry = false;
    public bool showVoxels = true;
    public bool showRegions = false;
    public bool showRawContours = false;
    public bool showContours = false;
    public bool showPolyMesh = false;
    public bool showPolyMeshDetail = false;

    void OnEnable()
    {
        m_NavMeshData = new NavMeshData();

#if UNITY_EDITOR
        NavMeshBuildDebugFlags initGroups = NavMeshBuildDebugFlags.None;
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.InputGeometry, showInputGeometry);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.Voxels, showVoxels);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.Regions, showRegions);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.RawContours, showRawContours);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.SimplifiedContours, showContours);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.PolygonMeshes, showPolyMesh);
        ApplyDebugFlags(ref initGroups, NavMeshBuildDebugFlags.PolygonMeshesDetail, showPolyMeshDetail);
        m_Debug = new NavMeshBuildDebugSettings();
        m_Debug.flags = initGroups;
#endif

        m_Handle = NavMesh.AddNavMeshData(m_NavMeshData);
    }

    void OnDisable()
    {
        m_Handle.Remove();
    }

    void Update()
    {
        if (!m_NavMeshData)
        {
            return;
        }

        rasterizationBounds = new Bounds(transform.position, volumeSize * Vector3.one);
        var buildSettings = NavMesh.GetSettingsByID(0);
        var borderX = 2 * (rasterizationBounds.extents.x + buildSettings.agentRadius);
        var borderY = (2 * rasterizationBounds.extents.y) + buildSettings.agentHeight;
        var borderZ = 2 * (rasterizationBounds.extents.z + buildSettings.agentRadius);
        collectionBounds = new Bounds(transform.position + new Vector3(0, 0.5f * buildSettings.agentHeight, 0), new Vector3(borderX, borderY, borderZ));
        var markups = new List<NavMeshBuildMarkup>();
        var results = new List<NavMeshBuildSource>();
        UnityEngine.AI.NavMeshBuilder.CollectSources(collectionBounds, ~0, m_UseGeometry, 0, markups, results);
        results.RemoveAll((x) => (x.component != null && x.component.gameObject.GetComponent<NavMeshAgent>() != null));

#if UNITY_EDITOR
        buildSettings.debug = m_Debug;
#endif
        if (asyncHandle == null || asyncHandle.isDone)
        {
            asyncHandle = UnityEngine.AI.NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMeshData, buildSettings, results, rasterizationBounds);
        }
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!m_NavMeshData)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(rasterizationBounds.center, rasterizationBounds.size);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(collectionBounds.center, collectionBounds.size);

        NavMeshBuildDebugFlags visibleGroups = NavMeshBuildDebugFlags.None;
        if (debugGroup == -1)
        {
            visibleGroups = m_Debug.flags;
        }
        else if (debugGroup == 0)
        {
            visibleGroups = NavMeshBuildDebugFlags.None;
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.InputGeometry, showInputGeometry);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.Voxels, showVoxels);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.Regions, showRegions);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.RawContours, showRawContours);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.SimplifiedContours, showContours);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.PolygonMeshes, showPolyMesh);
            ApplyDebugFlags(ref visibleGroups, NavMeshBuildDebugFlags.PolygonMeshesDetail, showPolyMeshDetail);
        }
        else
        {
            switch (debugGroup)
            {
                case 1:
                    visibleGroups = NavMeshBuildDebugFlags.InputGeometry;
                    break;
                case 2:
                    visibleGroups = NavMeshBuildDebugFlags.Voxels;
                    break;
                case 3:
                    visibleGroups = NavMeshBuildDebugFlags.Regions;
                    break;
                case 4:
                    visibleGroups = NavMeshBuildDebugFlags.RawContours;
                    break;
                case 5:
                    visibleGroups = NavMeshBuildDebugFlags.SimplifiedContours;
                    break;
                case 6:
                    visibleGroups = NavMeshBuildDebugFlags.PolygonMeshes;
                    break;
                case 7:
                    visibleGroups = NavMeshBuildDebugFlags.PolygonMeshesDetail;
                    break;
            }
        }

        NavMeshEditorHelpers.DrawBuildDebug(m_NavMeshData, visibleGroups);
#endif
    }

#if UNITY_EDITOR
    void ApplyDebugFlags(ref NavMeshBuildDebugFlags flags, NavMeshBuildDebugFlags toSet, bool active)
    {
        if (active)
        {
            flags |= toSet;
        }
        else
        {
            flags &= ~toSet;
        }
    }
#endif
}
