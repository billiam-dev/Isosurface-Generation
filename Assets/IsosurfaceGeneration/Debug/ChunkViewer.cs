#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace IsosurfaceGeneration.Debugging
{
    [ExecuteInEditMode]
    public class ChunkViewer : MonoBehaviour
    {
        public Isosurface Surface;
        GUIStyle m_Style;

        void OnEnable()
        {
            m_Style = new GUIStyle()
            {
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            };
        }

        void OnDrawGizmos()
        {
            if (!Surface || !Surface.IsGenerated)
                return;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.1f);

            Vector3 pos = Surface.transform.InverseTransformPoint(transform.position);
            Surface.ComputeIndices(pos, out int3 chunkIndex, out int3 densityIndex);
            Surface.GetChunk(chunkIndex).DrawBoundsGizmo();

            string label = string.Empty;
            label += $"Chunk: {chunkIndex.x}, {chunkIndex.y}, {chunkIndex.z}";
            label += $"\nInterchunk: {densityIndex.x}, {densityIndex.y}, {densityIndex.z}";
            label += $"\nDensity: {Surface.SampleDensity(pos)}";

            Handles.Label(transform.position, label, m_Style);
        }
    }
}
#endif
