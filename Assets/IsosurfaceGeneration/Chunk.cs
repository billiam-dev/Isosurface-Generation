using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public class Chunk : MonoBehaviour
    {
        [SerializeField]
        MeshFilter m_MeshFilter;

        [SerializeField]
        MeshRenderer m_MeshRenderer;

        [SerializeField]
        MeshCollider m_Collider;

        public DensityMap DensityMap
        {
            get
            {
                return m_DensityMap;
            }
        }

        DensityMap m_DensityMap;
        Mesh m_Mesh;

        const float k_RenderDistance = 128.0f;

        /// <summary>
        /// Create a new chunk at the given sizeX, sizeY, sizeZ chunk index.
        /// </summary>
        public static Chunk New(int3 chunkIndex, int size)
        {
            Chunk newChunk = new GameObject($"{chunkIndex.x}, {chunkIndex.y}, {chunkIndex.z}").AddComponent<Chunk>();
            newChunk.Initialize(chunkIndex, size);
            return newChunk;
        }

        void Initialize(int3 chunkIndex, int size)
        {
            m_DensityMap = new DensityMap(chunkIndex, size);

            m_MeshFilter = gameObject.AddComponent<MeshFilter>();
            m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            m_Collider = gameObject.AddComponent<MeshCollider>();

            gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        }

        public void Destroy()
        {
            DestroyImmediate(gameObject);
        }

        public void SetMesh(Mesh mesh)
        {
            m_Mesh = mesh;
            m_MeshFilter.sharedMesh = mesh;
            m_Collider.sharedMesh = mesh;
        }

        public void SetMaterial(Material material)
        {
            m_MeshRenderer.sharedMaterial = material;
        }

        public bool InViewFrustum(Camera camera)
        {
            Vector3 dimentions = m_DensityMap.sizeInWorld;
            Vector3 cameraPos = camera.transform.position;
            Vector3 chunkCentre = transform.position + (dimentions / 2.0f);

            if (Vector3.Distance(cameraPos, chunkCentre) > k_RenderDistance)
                return false;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Bounds cutoutBounds = GeometryUtility.CalculateBounds(new Vector3[] { Vector3.zero, dimentions * 2.0f }, transform.localToWorldMatrix);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, cutoutBounds);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            DrawBoundsGizmo();
        }

        public void DrawBoundsGizmo()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 1, 1, 0.5f);

            Vector3 dimentions = m_DensityMap.sizeInWorld - Vector3.one;
            Gizmos.DrawWireCube(dimentions / 2.0f, dimentions * 0.999f);
        }
#endif
    }
}
