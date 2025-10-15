using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    public class RealtimeShapesInput : SurfaceInput
    {
        [SerializeField]
        ShapeBrush[] m_Brushes;

        int m_NumBrushes;

        public override void OnDisable()
        {
            base.OnDisable();
            m_NumBrushes = 0;
        }

        internal override void EvaluatePropertyChanged()
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
                m_ShapeQueue.Add(shaper.GetShapeProperties());
            }

            // Evaluate changes in queue length.
            if (m_NumBrushes != m_Brushes.Length)
            {
                m_NumBrushes = m_Brushes.Length;
                m_RecomputeSurface = true;
            }
        }
    }
}
