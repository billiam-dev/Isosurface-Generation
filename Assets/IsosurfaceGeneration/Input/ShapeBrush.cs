using System;
using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [DisallowMultipleComponent]
    public class ShapeBrush : MonoBehaviour
    {
        /// <summary>
        /// Which shape function to use when applied to a surface.
        /// </summary>
        [Tooltip("Which shape function to use when applied to a surface.")]
        [SerializeField]
        ShapeFunction m_ShapeType = ShapeFunction.Sphere;

        /// <summary>
        /// Whether or not the shape is additive or subtractive.
        /// </summary>
        [Tooltip("Whether or not the shape is additive or subtractive.")]
        [SerializeField]
        BlendMode m_BlendMode = BlendMode.Additive;

        /// <summary>
        /// Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.
        /// </summary>
        [Range(0.1f, 1.0f), Tooltip("Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.")]
        [SerializeField]
        float m_Sharpness = 0.2f;

        [Min(0)]
        [SerializeField]
        float m_Dimention1 = 4.0f;

        [Min(0)]
        [SerializeField]
        float m_Dimention2 = 4.0f;

        [Min(0)]
        [SerializeField]
        float m_Dimention3 = 4.0f;

        public bool IsDirty
        {
            get
            {
                return m_IsDirty || transform.hasChanged;
            }
            set
            {
                m_IsDirty = value;
                transform.hasChanged = value;
            }
        }

        bool m_IsDirty;

        public int OrderInQueue
        {
            get
            {
                return m_OrderInQueue;
            }
            set
            {
                m_OrderInQueue = value;
                UpdateName();
            }
        }

        int m_OrderInQueue = -1;

        public void SetType(ShapeFunction type)
        {
            m_ShapeType = type;
        }

        public Shape GetShapeProperties()
        {
            AffineTransform matrix = new((float3)transform.position, (quaternion)transform.rotation, (float3)transform.lossyScale);
            return new Shape()
            {
                matrix = matrix,
                shapeID = m_ShapeType,
                blendMode = m_BlendMode,
                sharpness = m_Sharpness,
                dimention1 = m_Dimention1,
                dimention2 = m_Dimention2,
                dimention3 = m_Dimention3,
            };
        }

        void UpdateName()
        {
            gameObject.name = $"{m_OrderInQueue}: {m_ShapeType} Brush ({m_BlendMode})";
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UpdateName();
            m_IsDirty = true;
        }

        public void DrawChunkVolume(Isosurface isosurface)
        {
            int3 chunkVolume = GetShapeProperties().ComputeChunkVolume(isosurface);
            isosurface.ComputeIndices(transform.position, out int3 chunkIndex, out int3 densityIndex);

            Vector3 centre = new(chunkIndex.x, chunkIndex.y, chunkIndex.z);
            centre *= isosurface.ChunkSizeCells;

            Vector3 size = new(chunkVolume.x, chunkVolume.y, chunkVolume.z);
            size *= isosurface.ChunkSizeCells;

            Gizmos.DrawWireCube(isosurface.transform.TransformPoint(centre), size);
        }
#endif
    }
}
