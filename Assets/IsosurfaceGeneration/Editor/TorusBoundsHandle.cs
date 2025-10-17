using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace IsosurfaceGeneration
{
    /// <summary>
    /// A compound handle to edit a torus-shaped bounding volume in the Scene view.
    /// </summary>
    public class TorusBoundsHandle : PrimitiveBoundsHandle
    {
        /// <summary>
        /// Returns or specifies the radius of the torus bounding volume.
        /// </summary>
        public float radius
        {
            get
            {
                Vector3 size = GetSize();
                float num = 0f;
                for (int i = 0; i < 3; i++)
                {
                    if (IsAxisEnabled(i))
                    {
                        num = Mathf.Max(num, Mathf.Abs(size[i]));
                    }
                }

                return num * 0.5f;
            }
            set
            {
                SetSize(2f * value * Vector3.one);
            }
        }

        /// <summary>
        /// Create a new instance of the TorusBoundsHandle class.
        /// </summary>
        public TorusBoundsHandle()
        {
            axes = Axes.X | Axes.Z;
        }

        /// <summary>
        /// Draw a wireframe torus for this instance.
        /// </summary>
        protected override void DrawWireframe()
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
        }

        protected override Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
        {
            Vector3 max = newBounds.max;
            Vector3 min = newBounds.min;
            int num = 0;
            switch (handle)
            {
                case HandleDirection.PositiveY:
                case HandleDirection.NegativeY:
                    num = 1;
                    break;
                case HandleDirection.PositiveZ:
                case HandleDirection.NegativeZ:
                    num = 2;
                    break;
            }

            float num2 = 0.5f * (max[num] - min[num]);
            for (int i = 0; i < 3; i++)
            {
                if (i != num)
                {
                    min[i] = center[i] - num2;
                    max[i] = center[i] + num2;
                }
            }

            return new Bounds((max + min) * 0.5f, max - min);
        }
    }
}
