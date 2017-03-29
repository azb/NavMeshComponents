using System.Collections.Generic;

namespace UnityEngine.AI
{
    public enum CollectObjects
    {
        All = 0,
        Volume = 1,
        Children = 2,
    }

    [ExecuteInEditMode]
    [DefaultExecutionOrder(-102)]
    [AddComponentMenu("Navigation/NavMeshSurface", 30)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshSurface : MonoBehaviour
    {
        [SerializeField]
        int m_AgentTypeID;
        public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }

        [SerializeField]
        CollectObjects m_CollectObjects = CollectObjects.All;
        public CollectObjects collectObjects { get { return m_CollectObjects; } set { m_CollectObjects = value; } }

        [SerializeField]
        Vector3 m_Size = new Vector3(10.0f, 10.0f, 10.0f);
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }

        [SerializeField]
        Vector3 m_Center = new Vector3(0, 2.0f, 0);
        public Vector3 center { get { return m_Center; } set { m_Center = value; } }

        [SerializeField]
        LayerMask m_LayerMask = ~0;
        public LayerMask layerMask { get { return m_LayerMask; } set { m_LayerMask = value; } }

        [SerializeField]
        NavMeshCollectGeometry m_UseGeometry = NavMeshCollectGeometry.RenderMeshes;
        public NavMeshCollectGeometry useGeometry { get { return m_UseGeometry; } set { m_UseGeometry = value; } }

        [SerializeField]
        int m_DefaultArea;
        public int defaultArea { get { return m_DefaultArea; } set { m_DefaultArea = value; } }

        [SerializeField]
        bool m_IgnoreNavMeshAgent = true;
        public bool ignoreNavMeshAgent { get { return m_IgnoreNavMeshAgent; } set { m_IgnoreNavMeshAgent = value; } }

        [SerializeField]
        bool m_IgnoreNavMeshObstacle = true;
        public bool ignoreNavMeshObstacle { get { return m_IgnoreNavMeshObstacle; } set { m_IgnoreNavMeshObstacle = value; } }

        [SerializeField]
        bool m_OverrideTileSize;
        public bool overrideTileSize { get { return m_OverrideTileSize; } set { m_OverrideTileSize = value; } }
        [SerializeField]
        int m_TileSize = 256;
        public int tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
        [SerializeField]
        bool m_OverrideVoxelSize;
        public bool overrideVoxelSize { get { return m_OverrideVoxelSize; } set { m_OverrideVoxelSize = value; } }
        [SerializeField]
        float m_VoxelSize;
        public float voxelSize { get { return m_VoxelSize; } set { m_VoxelSize = value; } }

#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("Show or hide all the available debug data for this NavMesh surface.")]
        private bool m_DebugVisible = true;
        public bool debugVisible { get { return m_DebugVisible; } set { m_DebugVisible = value; } }
        [SerializeField]
        [Tooltip("The exact source geometry used when baking this NavMesh surface.")]
        private bool m_ShowInputGeometry = true;
        public bool showInputGeometry { get { return m_ShowInputGeometry; } set { m_ShowInputGeometry = value; } }
        [SerializeField]
        [Tooltip("Show the voxels used for sampling the source geometry during baking.")]
        private bool m_ShowVoxels = true;
        public bool showVoxels { get { return m_ShowVoxels; } set { m_ShowVoxels = value; } }
        [SerializeField]
        [Tooltip("Show the different flat regions computed during the NavMesh baking.")]
        private bool m_ShowRegions = true;
        public bool showRegions { get { return m_ShowRegions; } set { m_ShowRegions = value; } }
        [SerializeField]
        [Tooltip("Show the contours of the relevant voxels used during baking.")]
        private bool m_ShowRawContours = true;
        public bool showRawContours { get { return m_ShowRawContours; } set { m_ShowRawContours = value; } }
        [SerializeField]
        [Tooltip("Show the smoothed contours of the flat areas computed during the NavMesh baking.")]
        private bool m_ShowContours = true;
        public bool showContours { get { return m_ShowContours; } set { m_ShowContours = value; } }
        [SerializeField]
        [Tooltip("Show the polygonal mesh constructed within the generated contours during the NavMesh baking.")]
        private bool m_ShowPolyMesh = true;
        public bool showPolyMesh { get { return m_ShowPolyMesh; } set { m_ShowPolyMesh = value; } }
        [SerializeField]
        [Tooltip("Show the detailed polygonal mesh constructed by taking into account the source geometry.")]
        private bool m_ShowPolyMeshDetail = true;
        public bool showPolyMeshDetail { get { return m_ShowPolyMeshDetail; } set { m_ShowPolyMeshDetail = value; } }
#endif

        // Currently not supported advanced options
        [SerializeField]
        bool m_BuildHeightMesh;
        public bool buildHeightMesh { get { return m_BuildHeightMesh; } set { m_OverrideTileSize = value; } }

        // Reference to whole scene navmesh data asset.
        [SerializeField]
        NavMeshData m_BakedNavMeshData;
        public NavMeshData bakedNavMeshData { get { return m_BakedNavMeshData; } set { m_BakedNavMeshData = value; } }

        // Do not serialize - runtime only state.
        NavMeshDataInstance m_NavMeshDataInstance;
        Vector3 m_LastPosition = Vector3.zero;
        Quaternion m_LastRotation = Quaternion.identity;

        static readonly List<NavMeshSurface> s_NavMeshSurfaces = new List<NavMeshSurface>();

        public static List<NavMeshSurface> activeSurfaces
        {
            get { return s_NavMeshSurfaces; }
        }

#if UNITY_EDITOR
        public void UpdateDebugFlags()
        {
            if (m_BakedNavMeshData != null)
            {
                NavMeshBuildDebugSettings debugFlags = new NavMeshBuildDebugSettings();
                debugFlags.showInputGeometry = m_ShowInputGeometry && m_DebugVisible;
                debugFlags.showVoxels = m_ShowVoxels && m_DebugVisible;
                debugFlags.showRegions = m_ShowRegions && m_DebugVisible;
                debugFlags.showRawContours = m_ShowRawContours && m_DebugVisible;
                debugFlags.showContours = m_ShowContours && m_DebugVisible;
                debugFlags.showPolyMesh = m_ShowPolyMesh && m_DebugVisible;
                debugFlags.showPolyMeshDetail = m_ShowPolyMeshDetail && m_DebugVisible;

                m_BakedNavMeshData.debugVisibility = debugFlags;
            }
        }
#endif

        void OnEnable()
        {
            Register(this);
            AddData();

#if UNITY_EDITOR
            UpdateDebugFlags();
#endif
        }

        void OnDisable()
        {
            RemoveData();
            Unregister(this);
        }

        public void AddData()
        {
            if (m_NavMeshDataInstance.valid)
                return;

            if (m_BakedNavMeshData != null)
            {
                m_NavMeshDataInstance = NavMesh.AddNavMeshData(m_BakedNavMeshData, transform.position, transform.rotation);
                m_NavMeshDataInstance.owner = this;
            }

            m_LastPosition = transform.position;
            m_LastRotation = transform.rotation;
        }

        public void RemoveData()
        {
            m_NavMeshDataInstance.Remove();
            m_NavMeshDataInstance = new NavMeshDataInstance();
        }

        public NavMeshBuildSettings GetBuildSettings()
        {
            var buildSettings = NavMesh.GetSettingsByID(m_AgentTypeID);

            if (overrideTileSize)
            {
                buildSettings.overrideTileSize = true;
                buildSettings.tileSize = tileSize;
            }
            if (overrideVoxelSize)
            {
                buildSettings.overrideVoxelSize = true;
                buildSettings.voxelSize = voxelSize;
            }
            return buildSettings;
        }

        public void Bake()
        {
            Bake(new NavMeshBuildDebugSettings());
        }

        public void Bake(NavMeshBuildDebugSettings debug)
        {
            var sources = CollectSources();

            // Use unscaled bounds - this differs in behaviour from e.g. collider components.
            // But is similar to reflection probe - and since navmesh data has no scaling support - it is the right choice here.
            var sourcesBounds = new Bounds(m_Center, Abs(m_Size));
            if (m_CollectObjects == CollectObjects.All || m_CollectObjects == CollectObjects.Children)
            {
                sourcesBounds = CalculateWorldBounds(sources);
            }

            var data = NavMeshBuilder.BuildNavMeshData(GetBuildSettings(),
                sources, sourcesBounds, transform.position, transform.rotation, debug);

            if (data != null)
            {
                data.name = gameObject.name;
                RemoveData();
                m_BakedNavMeshData = data;
                if (isActiveAndEnabled)
                    AddData();
            }
        }

        static void Register(NavMeshSurface surface)
        {
            if (s_NavMeshSurfaces.Count == 0)
                NavMesh.onPreUpdate += UpdateActive;

            if (!s_NavMeshSurfaces.Contains(surface))
                s_NavMeshSurfaces.Add(surface);
        }

        static void Unregister(NavMeshSurface surface)
        {
            s_NavMeshSurfaces.Remove(surface);

            if (s_NavMeshSurfaces.Count == 0)
                NavMesh.onPreUpdate -= UpdateActive;
        }

        static void UpdateActive()
        {
            for (var i = 0; i < s_NavMeshSurfaces.Count; ++i)
                s_NavMeshSurfaces[i].UpdateDataIfTransformChanged();
        }

        void AppendModifierVolumes(ref List<NavMeshBuildSource> sources)
        {
            // Modifiers
            List<NavMeshModifierVolume> modifiers;
            if (m_CollectObjects == CollectObjects.Children)
            {
                modifiers = new List<NavMeshModifierVolume>(GetComponentsInChildren<NavMeshModifierVolume>());
                modifiers.RemoveAll(x => !x.isActiveAndEnabled);
            }
            else
            {
                modifiers = NavMeshModifierVolume.activeModifiers;
            }

            foreach (var m in modifiers)
            {
                if ((m_LayerMask & (1 << m.gameObject.layer)) == 0)
                    continue;
                if (!m.AffectsAgentType(m_AgentTypeID))
                    continue;
                var mcenter = m.transform.TransformPoint(m.center);
                var scale = m.transform.lossyScale;
                var msize = new Vector3(m.size.x * Mathf.Abs(scale.x), m.size.y * Mathf.Abs(scale.y), m.size.z * Mathf.Abs(scale.z));

                var src = new NavMeshBuildSource();
                src.shape = NavMeshBuildSourceShape.ModifierBox;
                src.transform = Matrix4x4.TRS(mcenter, m.transform.rotation, Vector3.one);
                src.size = msize;
                src.area = m.area;
                sources.Add(src);
            }
        }

        List<NavMeshBuildSource> CollectSources()
        {
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();

            List<NavMeshModifier> modifiers;
            if (m_CollectObjects == CollectObjects.Children)
            {
                modifiers = new List<NavMeshModifier>(GetComponentsInChildren<NavMeshModifier>());
                modifiers.RemoveAll(x => !x.isActiveAndEnabled);
            }
            else
            {
                modifiers = NavMeshModifier.activeModifiers;
            }

            foreach (var m in modifiers)
            {
                if ((m_LayerMask & (1 << m.gameObject.layer)) == 0)
                    continue;
                if (!m.AffectsAgentType(m_AgentTypeID))
                    continue;
                var markup = new NavMeshBuildMarkup();
                markup.root = m.transform;
                markup.overrideArea = m.overrideArea;
                markup.area = m.area;
                markup.ignoreFromBuild = m.ignoreFromBuild;
                markups.Add(markup);
            }

            if (m_CollectObjects == CollectObjects.All)
            {
                NavMeshBuilder.CollectSources(null, m_LayerMask, m_UseGeometry, m_DefaultArea, markups, sources);
            }
            else if (m_CollectObjects == CollectObjects.Children)
            {
                NavMeshBuilder.CollectSources(transform, m_LayerMask, m_UseGeometry, m_DefaultArea, markups, sources);
            }
            else if (m_CollectObjects == CollectObjects.Volume)
            {
                Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                var worldBounds = GetWorldBounds(localToWorld, new Bounds(m_Center, m_Size));
                NavMeshBuilder.CollectSources(worldBounds, m_LayerMask, m_UseGeometry, m_DefaultArea, markups, sources);
            }

            if (m_IgnoreNavMeshAgent)
                sources.RemoveAll((x) => (x.component != null && x.component.gameObject.GetComponent<NavMeshAgent>() != null));

            if (m_IgnoreNavMeshObstacle)
                sources.RemoveAll((x) => (x.component != null && x.component.gameObject.GetComponent<NavMeshObstacle>() != null));

            AppendModifierVolumes(ref sources);

            return sources;
        }

        static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
        {
            var absAxisX = Abs(mat.MultiplyVector(Vector3.right));
            var absAxisY = Abs(mat.MultiplyVector(Vector3.up));
            var absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
            var worldPosition = mat.MultiplyPoint(bounds.center);
            var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
            return new Bounds(worldPosition, worldSize);
        }

        Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
        {
            // Use the unscaled matrix for the NavMeshSurface
            Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            worldToLocal = worldToLocal.inverse;

            var result = new Bounds();
            foreach (var src in sources)
            {
                switch (src.shape)
                {
                    case NavMeshBuildSourceShape.Mesh:
                        {
                            var m = src.sourceObject as Mesh;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                            break;
                        }
                    case NavMeshBuildSourceShape.Terrain:
                        {
                            // Terrain pivot is lower/left corner - shift bounds accordingly
                            var t = src.sourceObject as TerrainData;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
                            break;
                        }
                    case NavMeshBuildSourceShape.Box:
                    case NavMeshBuildSourceShape.Sphere:
                    case NavMeshBuildSourceShape.Capsule:
                    case NavMeshBuildSourceShape.ModifierBox:
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                        break;
                }
            }
            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);
            return result;
        }

        bool HasTransformChanged()
        {
            if (m_LastPosition != transform.position) return true;
            if (m_LastRotation != transform.rotation) return true;
            return false;
        }

        void UpdateDataIfTransformChanged()
        {
            if (HasTransformChanged())
            {
                RemoveData();
                if (this.enabled)
                {
                    AddData();
                }
            }

#if UNITY_EDITOR
            UpdateDebugFlags();
#endif
        }

#if UNITY_EDITOR
        bool UnshareNavMeshAsset()
        {
            // Nothing to unshare
            if (m_BakedNavMeshData == null)
                return false;

            // Prefab parent owns the asset reference
            var prefabType = UnityEditor.PrefabUtility.GetPrefabType(this);
            if (prefabType == UnityEditor.PrefabType.Prefab)
                return false;

            // An instance can share asset reference only with its prefab parent
            var prefab = UnityEditor.PrefabUtility.GetPrefabParent(this) as NavMeshSurface;
            if (prefab != null && prefab.bakedNavMeshData == bakedNavMeshData)
                return false;

            // Don't allow referencing an asset that's assigned to another surface
            for (var i = 0; i < s_NavMeshSurfaces.Count; ++i)
            {
                var surface = s_NavMeshSurfaces[i];
                if (surface != this && surface.m_BakedNavMeshData == m_BakedNavMeshData)
                    return true;
            }

            // Asset is not referenced by known surfaces
            return false;
        }

        void OnValidate()
        {
            if (UnshareNavMeshAsset())
            {
                Debug.LogWarning("Duplicating NavMeshSurface does not duplicate the referenced navmesh data", this);
                m_BakedNavMeshData = null;
            }

            var settings = NavMesh.GetSettingsByID(m_AgentTypeID);
            if (settings.agentTypeID != -1)
            {
                // When unchecking the override control, revert to automatic value.
                const float kMinVoxelSize = 0.01f;
                if (!m_OverrideVoxelSize)
                    m_VoxelSize = settings.agentRadius / 3.0f;
                if (m_VoxelSize < kMinVoxelSize)
                    m_VoxelSize = kMinVoxelSize;

                // When unchecking the override control, revert to default value.
                const int kMinTileSize = 16;
                const int kMaxTileSize = 1024;
                const int kDefaultTileSize = 256;

                if (!m_OverrideTileSize)
                    m_TileSize = kDefaultTileSize;
                // Make sure tilesize is in sane range.
                if (m_TileSize < kMinTileSize)
                    m_TileSize = kMinTileSize;
                if (m_TileSize > kMaxTileSize)
                    m_TileSize = kMaxTileSize;
            }
        }
#endif
    }
}
