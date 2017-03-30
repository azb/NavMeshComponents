using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.AI;
using UnityEngine;

namespace UnityEditor.AI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshSurface))]
    class NavMeshSurfaceEditor : Editor
    {
        SerializedProperty m_AgentTypeID;
        SerializedProperty m_BuildHeightMesh;
        SerializedProperty m_Center;
        SerializedProperty m_CollectObjects;
        SerializedProperty m_DefaultArea;
        SerializedProperty m_LayerMask;
        SerializedProperty m_OverrideTileSize;
        SerializedProperty m_OverrideVoxelSize;
        SerializedProperty m_Size;
        SerializedProperty m_TileSize;
        SerializedProperty m_UseGeometry;
        SerializedProperty m_VoxelSize;

        SerializedProperty m_DebugVisible;
        SerializedProperty m_ShowInputGeometry;
        SerializedProperty m_ShowVoxels;
        SerializedProperty m_ShowRegions;
        SerializedProperty m_ShowRawContours;
        SerializedProperty m_ShowContours;
        SerializedProperty m_ShowPolyMesh;
        SerializedProperty m_ShowPolyMeshDetail;

        class Styles
        {
            public readonly GUIContent m_LayerMask = new GUIContent("Include Layers");

            public readonly GUIContent m_DebugVisible = new GUIContent("Visible Debug");
            public readonly GUIContent m_ShowInputGeometry = new GUIContent("Input Geometry");
            public readonly GUIContent m_ShowVoxels = new GUIContent("Voxels");
            public readonly GUIContent m_ShowRegions = new GUIContent("Regions");
            public readonly GUIContent m_ShowRawContours = new GUIContent("Raw Contours");
            public readonly GUIContent m_ShowContours = new GUIContent("Simplified Contours");
            public readonly GUIContent m_ShowPolyMesh = new GUIContent("Polygon Meshes");
            public readonly GUIContent m_ShowPolyMeshDetail = new GUIContent("Polygon Meshes Detail");

            public readonly GUIContent m_BakeButton = new GUIContent("Bake");
            public readonly GUIContent m_BakeButtonWithDebug = new GUIContent("Bake with Debug");
        }

        static Styles s_Styles;

        static NavMeshBuildDebugSettings s_DebugVisualization = new NavMeshBuildDebugSettings();
        static bool s_ShowDebugOptions;

        static Color s_HandleColor = new Color(127f, 214f, 244f, 100f) / 255;
        static Color s_HandleColorSelected = new Color(127f, 214f, 244f, 210f) / 255;
        static Color s_HandleColorDisabled = new Color(127f * 0.75f, 214f * 0.75f, 244f * 0.75f, 100f) / 255;

        static int s_HandleControlIDHint = typeof(NavMeshSurfaceEditor).Name.GetHashCode();
        BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle(s_HandleControlIDHint);

        bool editingCollider
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this); }
        }

        void OnEnable()
        {
            m_AgentTypeID = serializedObject.FindProperty("m_AgentTypeID");
            m_BuildHeightMesh = serializedObject.FindProperty("m_BuildHeightMesh");
            m_Center = serializedObject.FindProperty("m_Center");
            m_CollectObjects = serializedObject.FindProperty("m_CollectObjects");
            m_DefaultArea = serializedObject.FindProperty("m_DefaultArea");
            m_LayerMask = serializedObject.FindProperty("m_LayerMask");
            m_OverrideTileSize = serializedObject.FindProperty("m_OverrideTileSize");
            m_OverrideVoxelSize = serializedObject.FindProperty("m_OverrideVoxelSize");
            m_Size = serializedObject.FindProperty("m_Size");
            m_TileSize = serializedObject.FindProperty("m_TileSize");
            m_UseGeometry = serializedObject.FindProperty("m_UseGeometry");
            m_VoxelSize = serializedObject.FindProperty("m_VoxelSize");

            m_DebugVisible = serializedObject.FindProperty("m_DebugVisible");
            m_ShowInputGeometry = serializedObject.FindProperty ("m_ShowInputGeometry");
            m_ShowVoxels = serializedObject.FindProperty ("m_ShowVoxels");
            m_ShowRegions = serializedObject.FindProperty ("m_ShowRegions");
            m_ShowRawContours = serializedObject.FindProperty ("m_ShowRawContours");
            m_ShowContours = serializedObject.FindProperty ("m_ShowContours");
            m_ShowPolyMesh = serializedObject.FindProperty ("m_ShowPolyMesh");
            m_ShowPolyMeshDetail = serializedObject.FindProperty ("m_ShowPolyMeshDetail");

            NavMeshVisualizationSettings.showNavigation++;

            Undo.undoRedoPerformed += UndoRedoPerformed;

            s_DebugVisualization.flags = NavMeshBuildDebugFlags.All;
        }

        void OnDisable()
        {
            NavMeshVisualizationSettings.showNavigation--;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public virtual void UndoRedoPerformed()
        {
            foreach (NavMeshSurface navSurface in targets)
            {
                navSurface.UpdateDebugFlags();
            }
        }

        static string GetAndEnsureTargetPath(NavMeshSurface surface)
        {
            // Create directory for the asset if it does not exist yet.
            var activeScenePath = surface.gameObject.scene.path;

            var targetPath = "Assets";
            if (!string.IsNullOrEmpty(activeScenePath))
                targetPath = Path.Combine(Path.GetDirectoryName(activeScenePath), Path.GetFileNameWithoutExtension(activeScenePath));
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            return targetPath;
        }

        void CreateNavMeshAsset(NavMeshSurface surface)
        {
            var targetPath = GetAndEnsureTargetPath(surface);

            var combinedAssetPath = Path.Combine(targetPath, "NavMesh-" + surface.name + ".asset");
            combinedAssetPath = AssetDatabase.GenerateUniqueAssetPath(combinedAssetPath);
            AssetDatabase.CreateAsset(surface.bakedNavMeshData, combinedAssetPath);
        }

        NavMeshData GetNavMeshAssetToDelete(NavMeshSurface navSurface)
        {
            var prefabType = PrefabUtility.GetPrefabType(navSurface);
            if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
            {
                // Don't allow deleting the asset belonging to the prefab parent
                var parentSurface = PrefabUtility.GetPrefabParent(navSurface) as NavMeshSurface;
                if (parentSurface && navSurface.bakedNavMeshData == parentSurface.bakedNavMeshData)
                    return null;
            }
            return navSurface.bakedNavMeshData;
        }

        void BakeSurface(NavMeshSurface navSurface)
        {
            var assetToDelete = GetNavMeshAssetToDelete(navSurface);
            navSurface.Bake(s_DebugVisualization);
            EditorUtility.SetDirty(navSurface);

            if (assetToDelete)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(assetToDelete));
            }
            CreateNavMeshAsset(navSurface);
            EditorSceneManager.MarkSceneDirty(navSurface.gameObject.scene);
        }

        void ClearSurface(NavMeshSurface navSurface)
        {
            var assetToDelete = GetNavMeshAssetToDelete(navSurface);
            navSurface.RemoveData();
            navSurface.bakedNavMeshData = null;
            EditorUtility.SetDirty(navSurface);

            if (assetToDelete)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(assetToDelete));
                EditorSceneManager.MarkSceneDirty(navSurface.gameObject.scene);
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            var bs = NavMesh.GetSettingsByID(m_AgentTypeID.intValue);

            if (bs.agentTypeID != -1)
            {
                // Draw image
                const float diagramHeight = 80.0f;
                Rect agentDiagramRect = EditorGUILayout.GetControlRect(false, diagramHeight);
                NavMeshEditorHelpers.DrawAgentDiagram(agentDiagramRect, bs.agentRadius, bs.agentHeight, bs.agentClimb, bs.agentSlope);
            }
            NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", m_AgentTypeID);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_CollectObjects);
            if ((CollectObjects)m_CollectObjects.enumValueIndex == CollectObjects.Volume)
            {
                EditorGUI.indentLevel++;
                InspectorEditButtonGUI();
                EditorGUILayout.PropertyField(m_Size);
                EditorGUILayout.PropertyField(m_Center);
            }
            else
            {
                if (editingCollider)
                    EditMode.QuitEditMode();
            }

            EditorGUILayout.PropertyField(m_LayerMask, s_Styles.m_LayerMask);
            EditorGUILayout.PropertyField(m_UseGeometry);

            EditorGUILayout.Space();

            EditorGUILayout.Space();

            bool debugVisibilityChanged = false;
            m_OverrideVoxelSize.isExpanded = EditorGUILayout.Foldout(m_OverrideVoxelSize.isExpanded, "Advanced");
            if (m_OverrideVoxelSize.isExpanded)
            {
                EditorGUI.indentLevel++;

                NavMeshComponentsGUIUtility.AreaPopup("Default Area", m_DefaultArea);

                // Override voxel size.
                EditorGUILayout.PropertyField(m_OverrideVoxelSize);

                using (new EditorGUI.DisabledScope(!m_OverrideVoxelSize.boolValue || m_OverrideVoxelSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_VoxelSize);

                    if (!m_OverrideVoxelSize.hasMultipleDifferentValues)
                    {
                        if (!m_AgentTypeID.hasMultipleDifferentValues)
                        {
                            float voxelsPerRadius = m_VoxelSize.floatValue > 0.0f ? (bs.agentRadius / m_VoxelSize.floatValue) : 0.0f;
                            EditorGUILayout.LabelField(" ", voxelsPerRadius.ToString("0.00") + " voxels per agent radius", EditorStyles.miniLabel);
                        }
                        if (m_OverrideVoxelSize.boolValue)
                            EditorGUILayout.HelpBox("Voxel size controls how accurately the navigation mesh is generated from the level geometry. A good voxel size is 2-4 voxels per agent radius. Making voxel size smaller will increase build time.", MessageType.None);
                    }
                    EditorGUI.indentLevel--;
                }

                // Override tile size
                EditorGUILayout.PropertyField(m_OverrideTileSize);

                using (new EditorGUI.DisabledScope(!m_OverrideTileSize.boolValue || m_OverrideTileSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_TileSize);

                    if (!m_TileSize.hasMultipleDifferentValues && !m_VoxelSize.hasMultipleDifferentValues)
                    {
                        float tileWorldSize = m_TileSize.intValue * m_VoxelSize.floatValue;
                        EditorGUILayout.LabelField(" ", tileWorldSize.ToString("0.00") + " world units", EditorStyles.miniLabel);
                    }

                    if (!m_OverrideTileSize.hasMultipleDifferentValues)
                    {
                        if (m_OverrideTileSize.boolValue)
                            EditorGUILayout.HelpBox("Tile size controls how local the changes to the world are (rebuild or carve). Small tile size allows more local changes, while potentially generating more data overall.", MessageType.None);
                    }
                    EditorGUI.indentLevel--;
                }


                // Height mesh
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_BuildHeightMesh);
                }

                // Debug options
                s_ShowDebugOptions = GUILayout.Toggle(s_ShowDebugOptions, "Debug data to collect", EditorStyles.foldout);
                if (s_ShowDebugOptions)
                {
                    var collectedDebug = s_DebugVisualization.flags;
                    var bShowInputGeometry = EditorGUILayout.Toggle(s_Styles.m_ShowInputGeometry, (collectedDebug & NavMeshBuildDebugFlags.InputGeometry) != 0);
                    var bShowVoxels = EditorGUILayout.Toggle(s_Styles.m_ShowVoxels, (collectedDebug & NavMeshBuildDebugFlags.Voxels) != 0);
                    var bShowRegions = EditorGUILayout.Toggle(s_Styles.m_ShowRegions, (collectedDebug & NavMeshBuildDebugFlags.Regions) != 0);
                    var bShowRawContours = EditorGUILayout.Toggle(s_Styles.m_ShowRawContours, (collectedDebug & NavMeshBuildDebugFlags.RawContours) != 0);
                    var bShowContours = EditorGUILayout.Toggle(s_Styles.m_ShowContours, (collectedDebug & NavMeshBuildDebugFlags.SimplifiedContours) != 0);
                    var bShowPolyMesh = EditorGUILayout.Toggle(s_Styles.m_ShowPolyMesh, (collectedDebug & NavMeshBuildDebugFlags.PolygonMeshes) != 0);
                    var bShowPolyMeshDetail = EditorGUILayout.Toggle(s_Styles.m_ShowPolyMeshDetail, (collectedDebug & NavMeshBuildDebugFlags.PolygonMeshesDetail) != 0);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.InputGeometry, bShowInputGeometry);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.Voxels, bShowVoxels);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.Regions, bShowRegions);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.RawContours, bShowRawContours);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.SimplifiedContours, bShowContours);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.PolygonMeshes, bShowPolyMesh);
                    NavMeshSurface.ApplyDebugFlags(ref collectedDebug, NavMeshBuildDebugFlags.PolygonMeshesDetail, bShowPolyMeshDetail);
                    s_DebugVisualization.flags = collectedDebug;


                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Space ();

                    m_DebugVisible.boolValue = EditorGUILayout.BeginToggleGroup(s_Styles.m_DebugVisible, m_DebugVisible.boolValue);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_ShowInputGeometry, s_Styles.m_ShowInputGeometry);
                    EditorGUILayout.PropertyField(m_ShowVoxels, s_Styles.m_ShowVoxels);
                    EditorGUILayout.PropertyField(m_ShowRegions, s_Styles.m_ShowRegions);
                    EditorGUILayout.PropertyField(m_ShowRawContours, s_Styles.m_ShowRawContours);
                    EditorGUILayout.PropertyField(m_ShowContours, s_Styles.m_ShowContours);
                    EditorGUILayout.PropertyField(m_ShowPolyMesh, s_Styles.m_ShowPolyMesh);
                    EditorGUILayout.PropertyField(m_ShowPolyMeshDetail, s_Styles.m_ShowPolyMeshDetail);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.EndToggleGroup();

                    debugVisibilityChanged = EditorGUI.EndChangeCheck();

                    EditorGUILayout.HelpBox("Debug options help visualize various stages of the NavMesh building process. Data is collected when Bake is pressed and it is shown at the location where the NavMesh is built.", MessageType.None);
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            var hadError = false;
            var multipleTargets = targets.Length > 1;
            foreach (NavMeshSurface navSurface in targets)
            {
                var settings = navSurface.GetBuildSettings();
                // Calculating bounds is potentially expensive when unbounded - so here we just use the center/size.
                // It means the validation is not checking vertical voxel limit correctly when the surface is set to something else than "in volume".
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                if (navSurface.collectObjects == CollectObjects.Volume)
                {
                    bounds = new Bounds(navSurface.center, navSurface.size);
                }

                var errors = settings.ValidationReport(bounds);
                if (errors.Length > 0)
                {
                    if (multipleTargets)
                        EditorGUILayout.LabelField(navSurface.name);
                    foreach (var err in errors)
                    {
                        EditorGUILayout.HelpBox(err, MessageType.Warning);
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (GUILayout.Button("Open Agent Settings...", EditorStyles.miniButton))
                        NavMeshEditorHelpers.OpenAgentSettings(navSurface.agentTypeID);
                    GUILayout.EndHorizontal();
                    hadError = true;
                }

                if (debugVisibilityChanged)
                {
                    navSurface.UpdateDebugFlags();
                    SceneView.RepaintAll();
                }
            }

            if (hadError)
                EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(Application.isPlaying || m_AgentTypeID.intValue == -1))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("Clear"))
                {
                    foreach (NavMeshSurface s in targets)
                        ClearSurface(s);
                    SceneView.RepaintAll();
                }

                bool wantDebug = s_DebugVisualization.flags != NavMeshBuildDebugFlags.None;
                GUIContent bakeButtonText = wantDebug ? s_Styles.m_BakeButtonWithDebug : s_Styles.m_BakeButton;
                if (GUILayout.Button(bakeButtonText))
                {
                    foreach (NavMeshSurface navSurface in targets)
                        BakeSurface(navSurface);
                    SceneView.RepaintAll();
                }
                GUILayout.EndHorizontal();
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
        static void RenderBoxGizmoSelected(NavMeshSurface navSurface, GizmoType gizmoType)
        {
            RenderBoxGizmo(navSurface, gizmoType, true);

            RenderBuildDebug(navSurface);
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderBoxGizmoNotSelected(NavMeshSurface navSurface, GizmoType gizmoType)
        {
            if (NavMeshVisualizationSettings.showNavigation > 0)
                RenderBoxGizmo(navSurface, gizmoType, false);
            else
                Gizmos.DrawIcon(navSurface.transform.position, "NavMeshSurface Icon", true);

            RenderBuildDebug(navSurface);
        }

        private static void RenderBuildDebug(NavMeshSurface navSurface)
        {
            if (navSurface.bakedNavMeshData)
            {
                navSurface.UpdateDebugFlags();
                NavMeshEditorHelpers.DrawBuildDebug(navSurface.bakedNavMeshData, navSurface.m_VisibleDebug);
            }
        }

        static void RenderBoxGizmo(NavMeshSurface navSurface, GizmoType gizmoType, bool selected)
        {
            var color = selected ? s_HandleColorSelected : s_HandleColor;
            if (!navSurface.enabled)
                color = s_HandleColorDisabled;

            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            // Use the unscaled matrix for the NavMeshSurface
            var localToWorld = Matrix4x4.TRS(navSurface.transform.position, navSurface.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorld;

            if (navSurface.collectObjects == CollectObjects.Volume)
            {
                Gizmos.color = color;
                Gizmos.DrawWireCube(navSurface.center, navSurface.size);

                if (selected && navSurface.enabled)
                {
                    var colorTrans = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a * 0.15f);
                    Gizmos.color = colorTrans;
                    Gizmos.DrawCube(navSurface.center, navSurface.size);
                }
            }
            else
            {
                if (navSurface.bakedNavMeshData != null)
                {
                    var bounds = navSurface.bakedNavMeshData.sourceBounds;
                    Gizmos.color = Color.grey;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;

            Gizmos.DrawIcon(navSurface.transform.position, "NavMeshSurface Icon", true);
        }

        void InspectorEditButtonGUI()
        {
            var navSurface = (NavMeshSurface)target;
            var bounds = new Bounds(navSurface.transform.position, navSurface.size);

            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Volume",
                EditorGUIUtility.IconContent("EditCollider"),
                bounds,
                this
                );
        }

        void OnSceneGUI()
        {
            if (!editingCollider)
                return;

            var navSurface = (NavMeshSurface)target;
            var color = navSurface.enabled ? s_HandleColor : s_HandleColorDisabled;
            var localToWorld = Matrix4x4.TRS(navSurface.transform.position, navSurface.transform.rotation, Vector3.one);
            using (new Handles.DrawingScope(color, localToWorld))
            {
                m_BoundsHandle.center = navSurface.center;
                m_BoundsHandle.size = navSurface.size;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(navSurface, "Modified NavMesh Surface");
                    Vector3 center = m_BoundsHandle.center;
                    Vector3 size = m_BoundsHandle.size;
                    navSurface.center = center;
                    navSurface.size = size;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        [MenuItem("GameObject/AI/NavMesh Surface", false, 2000)]
        static public void CreateNavMeshSurface(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("NavMesh Surface", parent);
            go.AddComponent<NavMeshSurface>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }
    }
}
