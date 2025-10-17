using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Isosurface))]
    public class RealtimeShapesInput : MonoBehaviour
    {
        Isosurface m_Isosurface;

        NativeList<Shape> m_ShapeQueue;
        bool m_RecomputeSurface;

        int3 m_CurrentDimentions;
        ChunkCellDimentions m_CurrentChunkSize;
        IcosurfaceGenerationMethod m_CurrentMeshingMethod;

        [SerializeField]
        ShapeBrush[] m_Brushes;

        int m_NumBrushes = -1;

        void OnEnable()
        {
            m_ShapeQueue = new(10, Allocator.Persistent);
            m_Isosurface = GetComponent<Isosurface>();
#if UNITY_EDITOR
            EditorApplication.update += UpdateSurface;
#endif
            ReinitSurface();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateSurface;
#endif
            DestroySurface();
            m_ShapeQueue.Dispose();
            m_NumBrushes = -1;
        }

#if !UNITY_EDITOR
        void Update()
        {
            UpdateSurface();
        }
#endif

        void UpdateSurface()
        {
            if (m_CurrentMeshingMethod != m_Isosurface.MeshingMethod ||
                m_Isosurface.Dimentions.x != m_CurrentDimentions.x ||
                m_Isosurface.Dimentions.y != m_CurrentDimentions.y ||
                m_Isosurface.Dimentions.z != m_CurrentDimentions.z ||
                m_Isosurface.ChunkSize != m_CurrentChunkSize)
                ReinitSurface();

            if (m_Isosurface.PropertyChanged)
            {
                m_RecomputeSurface = true;
                m_Isosurface.PropertyChanged = false;
            }

            EvaluatePropertyChanged();

            if (m_RecomputeSurface)
                m_Isosurface.Recompute(m_ShapeQueue.AsArray());

            m_RecomputeSurface = false;
        }

        void ReinitSurface()
        {
            m_Isosurface.Destroy();
            m_Isosurface.Generate();

            m_CurrentMeshingMethod = m_Isosurface.MeshingMethod;
            m_CurrentDimentions = m_Isosurface.Dimentions;
            m_CurrentChunkSize = m_Isosurface.ChunkSize;

            EvaluatePropertyChanged();
        }

        void DestroySurface()
        {
            m_Isosurface.Destroy();
        }

        void EvaluatePropertyChanged()
        {
            m_ShapeQueue.Clear();
            m_Brushes = GetComponentsInChildren<ShapeBrush>();

            // Evaluate changes in brushes and build shape queue.
            for (int i = 0; i < m_Brushes.Length; i++)
            {
                ShapeBrush shaper = m_Brushes[i];

                // Check queue order.
                if (shaper.OrderInQueue != i)
                {
                    shaper.OrderInQueue = i;
                    m_RecomputeSurface = true;
                }

                // Check property changed.
                if (shaper.PropertyChanged)
                {
                    shaper.PropertyChanged = false;
                    m_RecomputeSurface = true;
                }

                // Add brush to shape queue.
                m_ShapeQueue.Add(shaper.GetShapeProperties(m_Isosurface));
            }

            // Evaluate changes in queue length.
            if (m_NumBrushes != m_Brushes.Length)
            {
                m_NumBrushes = m_Brushes.Length;
                m_RecomputeSurface = true;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new(0, 1, 0, 0.1f);

            foreach (ShapeBrush brush in m_Brushes)
            {
                if (Selection.Contains(brush.gameObject))
                    brush.DrawChunkVolume(m_Isosurface);
            }
        }
#endif
    }
}
