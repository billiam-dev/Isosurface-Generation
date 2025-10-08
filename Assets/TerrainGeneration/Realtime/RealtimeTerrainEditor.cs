using UnityEditor;
using UnityEngine;

namespace TerrainGeneration.RealtimeEditor
{
    /// <summary>
    /// Allows for realtime editing of terrains via Shape Brushes.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(ProceduralTerrain))]
    public class RealtimeTerrainEditor : MonoBehaviour
    {
        public ShapeBrush[] Brushes;
        ProceduralTerrain m_Terrain;
        int m_NumBrushes;

        void OnEnable()
        {
            EditorApplication.update += EvaluatePropertyChanged;

            m_Terrain = GetComponent<ProceduralTerrain>();
            m_Terrain.Generate();

            TerrainShape[] shapeQueue = new TerrainShape[Brushes.Length];
            for (int i = 0; i < Brushes.Length; i++)
                shapeQueue[i] = Brushes[i].GetShapeProperties();

            m_Terrain.Recompute(shapeQueue);
        }

        void OnDisable()
        {
            EditorApplication.update -= EvaluatePropertyChanged;
            m_Terrain.Destroy();
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
            TerrainShape[] shapeQueue = new TerrainShape[Brushes.Length];
            bool recalculateTerrain = false;
            
            // Evaluate changes in brushes.
            for (int i = 0; i < Brushes.Length; i++)
            {
                ShapeBrush shaper = Brushes[i];
                shapeQueue[i] = shaper.GetShapeProperties();

                if (shaper.PropertyChanged)
                {
                    shaper.PropertyChanged = false;
                    recalculateTerrain = true;
                }
            }

            // Evaluate changes in queue length.
            if (m_NumBrushes != shapeQueue.Length)
            {
                m_NumBrushes = shapeQueue.Length;
                recalculateTerrain = true;
            }

            // Evaluate changes in the terrain properties.
            if (m_Terrain.PropertyChanged)
            {
                recalculateTerrain = true;
                m_Terrain.PropertyChanged = false;
            }

            // If a change was detected, recompute the terrain.
            if (recalculateTerrain)
                m_Terrain.Recompute(shapeQueue);
        }
    }
}
