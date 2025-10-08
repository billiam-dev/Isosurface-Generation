#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TerrainGeneration.Debugging
{
    [ExecuteInEditMode]
    public class TerrainDebugger : MonoBehaviour
    {
        public ProceduralTerrain Terrain;
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
            if (!Terrain)
                return;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.1f);

            Vector3 pos = Terrain.transform.InverseTransformPoint(transform.position);
            Terrain.ComputeIndices(pos, out int3 chunkIndex, out int3 densityIndex);
            Terrain.GetChunk(chunkIndex).DrawBoundsGizmo();

            string label = string.Empty;
            label += $"Chunk: {chunkIndex.x}, {chunkIndex.y}, {chunkIndex.z}";
            label += $"\nInterchunk: {densityIndex.x}, {densityIndex.y}, {densityIndex.z}";
            label += $"\nDensity: {Terrain.SampleDensity(pos)}";

            Handles.Label(transform.position, label, m_Style);
        }
    }
}
#endif
