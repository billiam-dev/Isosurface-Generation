using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [RequireComponent(typeof(Camera))]
    public class SurfacePainter : MonoBehaviour
    {
        public enum InputMode
        {
            Mouse,
            Camera
        }

        [Header("Shape")]
        [Range(1.0f, 32.0f)]
        public float Radius = 1.0f;

        [Range(0.1f, 10.0f)]
        public float Sharpness = 2.0f;

        [Header("Input")]
        [SerializeField]
        InputMode m_InputMode = InputMode.Mouse;

        [Range(0.0f, 0.5f)]
        public float PaintDelay = 0.05f;

        [Range(0.0f, 500f)]
        public float RayLength = 500.0f;

        [Header("Pointer")]
        [SerializeField]
        Mesh m_PointerMesh;

        [SerializeField]
        Material m_PointerMaterial;

        GameObject m_Pointer;
        float m_LastInputTime;

        const float k_BushSizeScrollSpeed = 50.0f;

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
            // Try find surface
            GetSurfaceAtMousePosition(out Isosurface surface, out Vector3 hitPoint);

            // Update pointer
            m_Pointer.SetActive(surface != null);
            if (surface != null)
            {
                m_Pointer.transform.position = hitPoint;
                m_Pointer.transform.localScale = 2.0f * Radius * Vector3.one;
            }

            // Radius input
            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") < 0.0f)
                Radius += k_BushSizeScrollSpeed * Time.deltaTime;

            if (UnityEngine.Input.GetAxis("Mouse ScrollWheel") > 0.0f)
                Radius -= k_BushSizeScrollSpeed * Time.deltaTime;

            Radius = Mathf.Clamp(Radius, 1.0f, 32.0f);

            // Shape input
            if (surface && Time.time > m_LastInputTime + PaintDelay)
            {
                if (UnityEngine.Input.GetMouseButton(0))
                    ApplyShape(surface, hitPoint, BlendMode.Additive);

                if (UnityEngine.Input.GetMouseButton(1))
                    ApplyShape(surface, hitPoint, BlendMode.Subtractive);
            }
        }

        void ApplyShape(Isosurface surface, Vector3 position, BlendMode blendMode)
        {
            Shape shape = new()
            {
                matrix = math.inverse(new AffineTransform(new float3(position), quaternion.identity, 1.0f)),
                shapeID = ShapeFuncion.Sphere,
                blendMode = blendMode,
                sharpness = Sharpness,
                dimention1 = Radius
            };

            surface.ApplyShape(shape);

            m_LastInputTime = Time.time;
        }

        void GetSurfaceAtMousePosition(out Isosurface surface, out Vector3 hitPoint)
        {
            Ray ray = new();
            
            switch (m_InputMode)
            {
                case InputMode.Mouse:
                    ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                    break;

                case InputMode.Camera:
                    ray.origin = transform.position;
                    ray.direction = transform.forward;
                    break;
            }
            
            Physics.Raycast(ray, out RaycastHit hitInfo, RayLength);

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
