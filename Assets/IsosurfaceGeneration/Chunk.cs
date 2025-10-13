using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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

        Vector3 m_Bounds;

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
        /// Create a new chunk object at the given index.
        /// </summary>
        public static Chunk New(int3 chunkIndex, int chunkSize)
        {
            Chunk newChunk = new GameObject($"{chunkIndex.x}, {chunkIndex.y}, {chunkIndex.z}").AddComponent<Chunk>();
            newChunk.Initialize(chunkIndex, chunkSize);
            return newChunk;
        }

        void Initialize(int3 chunkIndex, int chunkSize)
        {
            m_DensityMap = new DensityMap(chunkIndex, chunkSize);
            m_Bounds = new Vector3(chunkSize, chunkSize, chunkSize);

            m_MeshFilter = gameObject.AddComponent<MeshFilter>();
            m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            m_Collider = gameObject.AddComponent<MeshCollider>();

            m_Mesh = new Mesh();

            m_MeshFilter.sharedMesh = m_Mesh;
            m_Collider.sharedMesh = m_Mesh;

            gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        }

        public void Destroy()
        {
            DestroyImmediate(gameObject);
        }

        /// <summary>
        /// Assign mesh data from vertices, normals and indices list.
        /// </summary>
        public void SetMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            m_Mesh.Clear();

            if (vertices.Count > 2)
            {
                m_Mesh.SetVertices(vertices);
                m_Mesh.SetTriangles(triangles, 0, true);
                m_Mesh.SetNormals(normals);
            }
        }

        const MeshUpdateFlags updateFlags =
              MeshUpdateFlags.DontNotifyMeshUsers |
              MeshUpdateFlags.DontRecalculateBounds |
              MeshUpdateFlags.DontResetBoneBounds |
              MeshUpdateFlags.DontValidateIndices;

        /// <summary>
        /// Assign mesh data from a marching cubes mesher job.
        /// </summary>
        public void SetMesh(MarchingCubesMesherJob mesher)
        {
            var vertices = mesher.vertices.ToArray(Allocator.Temp);
            var indices = mesher.indices.ToArray(Allocator.Temp);

            if (vertices.Length > 2)
            {
                m_Mesh.SetVertexBufferParams(vertices.Length, Vertex.Format);
                m_Mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);

                SubMeshDescriptor subMeshDescriptor = new(0, indices.Length, MeshTopology.Triangles);
                m_Mesh.SetSubMesh(0, subMeshDescriptor, updateFlags);

                m_Mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, updateFlags);
                m_Mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);

                m_Mesh.bounds = new Bounds(Vector3.zero, m_Bounds  * 2.0f);
            }
            else
            {
                m_Mesh.Clear();
            }
        }

        /// <summary>
        /// Set the chunk material.
        /// </summary>
        public void SetMaterial(Material material)
        {
            m_MeshRenderer.sharedMaterial = material;
        }

        /// <summary>
        /// Whether or not the chunk is within the camera's view frustum, and within the preset render distance.
        /// </summary>
        public bool InViewFrustum(Camera camera)
        {
            Vector3 cameraPos = camera.transform.position;
            Vector3 chunkCentre = transform.position + (m_Bounds / 2.0f);

            if (Vector3.Distance(cameraPos, chunkCentre) > k_RenderDistance)
                return false;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Bounds cutoutBounds = GeometryUtility.CalculateBounds(new Vector3[] { Vector3.zero, m_Bounds * 2.0f }, transform.localToWorldMatrix);
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

            Gizmos.DrawWireCube(m_Bounds / 2.0f, m_Bounds * 0.999f);
        }
#endif
    }
}
