using UnityEngine;

namespace IsosurfaceGeneration.Debugging
{
    [RequireComponent(typeof(Camera))]
    public class SurfacePainter : MonoBehaviour
    {
        public enum InputMode
        {
            Mouse,
            Camera
        }

        [SerializeField]
        InputMode m_InputMode = InputMode.Mouse;

        [SerializeField]
        float m_RayLength = 200;

        [SerializeField]
        Mesh m_PointerMesh;

        [SerializeField]
        Material m_PointerMaterial;

        GameObject m_Pointer;
        float m_LastInputTime;

        void OnEnable()
        {
            m_LastInputTime = float.NegativeInfinity;

            m_Pointer = new GameObject("Paint Pointer");
            MeshFilter meshFilter = m_Pointer.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = m_Pointer.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = m_PointerMesh;
            meshRenderer.sharedMaterial = m_PointerMaterial;
        }

        void OnDisable()
        {
            Destroy(m_Pointer);
        }

        void Update()
        {
            GetSurfaceAtMousePosition(out Isosurface surface, out Vector3 hitPoint);
            m_Pointer.transform.position = hitPoint;

            if (surface && Time.time > m_LastInputTime + 0.1f)
            {
                if (Input.GetMouseButton(0))
                {
                    ApplyShape(surface, hitPoint, BlendMode.Additive);
                }

                if (Input.GetMouseButton(1))
                {
                    ApplyShape(surface, hitPoint, BlendMode.Subtractive);
                }
            }
        }

        void ApplyShape(Isosurface surface, Vector3 position, BlendMode blendMode)
        {
            Shape shape = new()
            {
                matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one).inverse,
                shapeID = ShapeFuncion.Sphere,
                blendMode = blendMode,
                sharpness = 1,
                dimention1 = 1
            };

            surface.ApplyShapeAtPosition(shape, position);

            m_LastInputTime = Time.time;
        }

        void GetSurfaceAtMousePosition(out Isosurface surface, out Vector3 hitPoint)
        {
            Ray ray = new();
            
            switch (m_InputMode)
            {
                case InputMode.Mouse:
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    break;

                case InputMode.Camera:
                    ray.origin = transform.position;
                    ray.direction = transform.forward;
                    break;
            }
            
            Physics.Raycast(ray, out RaycastHit hitInfo, m_RayLength);

            if (hitInfo.collider)
            {
                surface = hitInfo.collider.GetComponentInParent<Isosurface>();
                hitPoint = hitInfo.point;
                return;
            }

            surface = null;
            hitPoint = Vector3.zero;
        }
    }
}
