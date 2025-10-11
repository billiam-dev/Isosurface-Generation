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
        Isosurface m_Isosurface;
        int m_NumBrushes;

        void OnEnable()
        {
            EditorApplication.update += EvaluatePropertyChanged;

            m_Isosurface = GetComponent<Isosurface>();
            if (!m_Isosurface.IsGenerated)
                m_Isosurface.Generate();

            Shape[] shapeQueue = new Shape[Brushes.Length];
            for (int i = 0; i < Brushes.Length; i++)
                shapeQueue[i] = Brushes[i].GetShapeProperties();

            m_Isosurface.Recompute(shapeQueue);
        }

        void OnDisable()
        {
            EditorApplication.update -= EvaluatePropertyChanged;
            m_Isosurface.Destroy();
        }

        void Update()
        {
            EvaluatePropertyChanged();
        }

        void EvaluatePropertyChanged()
        {
            // Build brushes queue. Their order in the inspector becomes the order that they are applied.
            Brushes = GetComponentsInChildren<ShapeBrush>();

            // Initialize shape queue.
            Shape[] shapeQueue = new Shape[Brushes.Length];
            bool recalculateSurface = false;
            
            // Evaluate changes in brushes.
            for (int i = 0; i < Brushes.Length; i++)
            {
                ShapeBrush shaper = Brushes[i];

                // Check queue order.
                if (shaper.OrderInQueue != i)
                {
                    shaper.OrderInQueue = i;
                    recalculateSurface = true;
                }

                // Check property changed.
                if (shaper.PropertyChanged)
                {
                    shaper.PropertyChanged = false;
                    recalculateSurface = true;
                }

                // Add brush to shape queue.
                shapeQueue[i] = shaper.GetShapeProperties();
            }

            // Evaluate changes in queue length.
            if (m_NumBrushes != shapeQueue.Length)
            {
                m_NumBrushes = shapeQueue.Length;
                recalculateSurface = true;
            }

            // Evaluate changes in the surface properties.
            if (m_Isosurface.PropertyChanged)
            {
                recalculateSurface = true;
                m_Isosurface.PropertyChanged = false;
            }

            // If a change was detected, recompute the surface.
            if (recalculateSurface)
                m_Isosurface.Recompute(shapeQueue);
        }
    }
}
