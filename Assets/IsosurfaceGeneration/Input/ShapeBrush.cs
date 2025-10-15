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
            return new Shape()
            {
                matrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale).inverse,
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
#endif
    }
}
