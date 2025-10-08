using UnityEngine;

namespace TerrainGeneration.Debugging
{
    [RequireComponent(typeof(Camera))]
    public class TerrainPainter : MonoBehaviour
    {
        const float k_RayLength = 200;
        float m_LastInputTime;

        void Start()
        {
            m_LastInputTime = float.NegativeInfinity;
        }

        void Update()
        {
            if (Time.time > m_LastInputTime + 0.1f)
            {
                if (Input.GetMouseButton(0))
                {
                    ApplyTerrainShape(BlendMode.Additive);
                    m_LastInputTime = Time.time;
                }

                if (Input.GetMouseButton(1))
                {
                    ApplyTerrainShape(BlendMode.Subtractive);
                    m_LastInputTime = Time.time;
                }
            }
        }

        void ApplyTerrainShape(BlendMode blendMode)
        {
            GetTerrainAtMousePosition(out ProceduralTerrain terrain, out Vector3 hitPoint);
            if (terrain)
            {
                TerrainShape shape = new()
                {
                    matrix = Matrix4x4.TRS(hitPoint, Quaternion.identity, Vector3.one).inverse,
                    shapeID = ShapeFuncion.Sphere,
                    blendMode = blendMode,
                    sharpness = 1,
                    dimention1 = 1
                };

                terrain.ApplyShapeAtPosition(shape, hitPoint);
            }
        }

        void GetTerrainAtMousePosition(out ProceduralTerrain terrain, out Vector3 hitPoint)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit hitInfo, k_RayLength);

            if (hitInfo.collider)
            {
                terrain = hitInfo.collider.GetComponentInParent<ProceduralTerrain>();
                hitPoint = hitInfo.point;
                return;
            }

            terrain = null;
            hitPoint = Vector3.zero;
        }
    }
}
