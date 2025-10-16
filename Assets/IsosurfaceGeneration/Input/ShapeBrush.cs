using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    public class ShapeBrush : MonoBehaviour
    {
        /// <summary>
        /// Which shape function to use when applied to a surface.
        /// </summary>
        [Tooltip("Which shape function to use when applied to a surface.")]
        public ShapeFuncion Shape;

        /// <summary>
        /// Whether or not the shape is additive or subtractive.
        /// </summary>
        [Tooltip("Whether or not the shape is additive or subtractive.")]
        public BlendMode BlendMode;

        /// <summary>
        /// Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.
        /// </summary>
        [Range(0.1f, 1.0f), Tooltip("Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.")]
        public float Sharpness = 0.2f;

        public float Dimention1 = 4;
        public float Dimention2 = 4;

        public bool PropertyChanged
        {
            get
            {
                return m_PropertyChanged || transform.hasChanged;
            }
            set
            {
                m_PropertyChanged = value;
                transform.hasChanged = value;
            }
        }

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

        bool m_PropertyChanged;
        int m_OrderInQueue = -1;

        public Shape GetShapeProperties()
        {
            float3 pos = new(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
            quaternion rot = new(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w);
            float3 scale = new(transform.localScale.x, transform.localScale.y, transform.localScale.z);

            return new Shape()
            {
                matrix = math.inverse(new AffineTransform(pos, rot, scale)),
                shapeID = Shape,
                blendMode = BlendMode,
                sharpness = Sharpness,
                dimention1 = Dimention1,
                dimention2 = Dimention2,
            };
        }

        void UpdateName()
        {
            gameObject.name = $"{m_OrderInQueue}: {Shape} Brush ({BlendMode})";
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UpdateName();
            m_PropertyChanged = true;
        }

        public void DrawChunkVolume(Isosurface isosurface)
        {
            int3 chunkVolume = GetShapeProperties().ComputeChunkVolume(isosurface);
            isosurface.ComputeIndices(transform.position, out int3 chunkIndex, out int3 densityIndex);

            Vector3 centre = new Vector3(chunkIndex.x, chunkIndex.y, chunkIndex.z) + (Vector3.one / 2.0f);
            centre *= isosurface.ChunkSizeCells;

            Vector3 size = new Vector3(chunkVolume.x, chunkVolume.y, chunkVolume.z);
            size *= isosurface.ChunkSizeCells;

            Gizmos.DrawWireCube(centre, size);
        }
#endif
    }
}
