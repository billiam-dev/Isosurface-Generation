using UnityEngine;

namespace TerrainGeneration.RealtimeEditor
{
    public class ShapeBrush : MonoBehaviour
    {
        /// <summary>
        /// Which shape function to use when applied to a terrain.
        /// </summary>
        [Tooltip("Which shape function to use when applied to a terrain.")]
        public ShapeFuncion Shape;

        /// <summary>
        /// Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.
        /// </summary>
        [Range(0.1f, 1.0f), Tooltip("Value used in the smooth min function, which blends shapes together. The higher the value, the sharper the seams between objects will be.")]
        public float SmoothnessConstant = 0.2f;

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

        bool m_PropertyChanged;

        public TerrainShape GetShapeProperties()
        {
            return new TerrainShape()
            {
                matrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale).inverse,
                shapeID = Shape,
                smoothness = SmoothnessConstant,
                dimention1 = Dimention1,
                dimention2 = Dimention2,
            };
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            gameObject.name = $"{Shape} Brush";
            m_PropertyChanged = true;
        }
#endif
    }
}
