using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Isosurface))]
    public abstract class SurfaceInput : MonoBehaviour
    {
        internal Isosurface m_Isosurface;

        internal NativeList<Shape> m_ShapeQueue;
        internal bool m_RecomputeSurface;

        int3 m_CurrentDimentions;
        ChunkCellDimentions m_CurrentChunkSize;
        IcosurfaceGenerationMethod m_CurrentMeshingMethod;

        public virtual void OnEnable()
        {
            m_ShapeQueue = new(10, Allocator.Persistent);
            m_Isosurface = GetComponent<Isosurface>();
            EditorApplication.update += UpdateSurface;
            ReinitSurface();
        }

        public virtual void OnDisable()
        {
            EditorApplication.update -= UpdateSurface;
            DestroySurface();
            m_ShapeQueue.Dispose();
        }

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

        internal abstract void EvaluatePropertyChanged();
    }
}
