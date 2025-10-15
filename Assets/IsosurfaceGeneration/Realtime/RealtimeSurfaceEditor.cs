using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace IsosurfaceGeneration.RealtimeEditor
{
    /// <summary>
    /// Allows for realtime editing of isosurfaces via Shape Brushes.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Isosurface))]
    public class RealtimeSurfaceEditor : MonoBehaviour
    {
        public ShapeBrush[] Brushes;
        Shape[] m_ShapeQueue;

        Isosurface m_Isosurface;
        int m_NumBrushes;

        int3 m_CurrentDimentions;
        ChunkCellDimentions m_CurrentChunkSize;

        void OnEnable()
        {
            m_Isosurface = GetComponent<Isosurface>();

            EditorApplication.update += Update;
            GenerateSurface();
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            m_Isosurface.Destroy();

            m_ShapeQueue = null;
            m_NumBrushes = -1;
        }

        void Update()
        {
            if (m_Isosurface.Dimentions.x != m_CurrentDimentions.x ||
                m_Isosurface.Dimentions.y != m_CurrentDimentions.y ||
                m_Isosurface.Dimentions.z != m_CurrentDimentions.z ||
                m_Isosurface.ChunkSize != m_CurrentChunkSize)
                GenerateSurface();

            EvaluatePropertyChanged();
        }

        void GenerateSurface()
        {
            m_Isosurface.Destroy();
            m_Isosurface.Generate();

            m_CurrentDimentions = m_Isosurface.Dimentions;
            m_CurrentChunkSize = m_Isosurface.ChunkSize;

            EvaluatePropertyChanged();
        }

        void EvaluatePropertyChanged()
        {
            bool recomputeSurface = false;

            // Build brushes queue. Their order in the inspector becomes the order that they are applied.
            Brushes = GetComponentsInChildren<ShapeBrush>();

            // Initialize shape queue.
            m_ShapeQueue = new Shape[Brushes.Length];
            
            // Evaluate changes in brushes.
            for (int i = 0; i < Brushes.Length; i++)
            {
                ShapeBrush shaper = Brushes[i];

                // Check queue order.
                if (shaper.OrderInQueue != i)
                {
                    shaper.OrderInQueue = i;
                    recomputeSurface = true;
                }

                // Check property changed.
                if (shaper.PropertyChanged)
                {
                    shaper.PropertyChanged = false;
                    recomputeSurface = true;
                }

                // Add brush to shape queue.
                m_ShapeQueue[i] = shaper.GetShapeProperties();
            }

            // Evaluate changes in queue length.
            if (m_NumBrushes != m_ShapeQueue.Length)
            {
                m_NumBrushes = m_ShapeQueue.Length;
                recomputeSurface = true;
            }

            // Evaluate changes in the surface properties.
            if (m_Isosurface.PropertyChanged)
            {
                recomputeSurface = true;
                m_Isosurface.PropertyChanged = false;
            }

            // If a change was detected, recompute the surface.
            if (recomputeSurface)
                m_Isosurface.Recompute(m_ShapeQueue);
        }
    }
}
