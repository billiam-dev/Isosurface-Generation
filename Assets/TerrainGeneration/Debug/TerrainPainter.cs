using UnityEngine;

namespace TerrainGeneration.Debugging
{
    [RequireComponent(typeof(Camera))]
    public class TerrainPainter : MonoBehaviour
    {
        const float k_RayLength = 100;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
                TryMineTerrain();
        }

        void TryMineTerrain()
        {
            GetTerrainAtMousePosition(out ProceduralTerrain terrain, out Vector3 hitPoint);
            if (terrain)
            {
                TerrainShape shape = new()
                {
                    matrix = Matrix4x4.TRS(hitPoint, Quaternion.identity, Vector3.one).inverse,
                    shapeID = ShapeFuncion.Sphere,
                    smoothness = 1,
                    dimention1 = 2
                };

                terrain.ApplyShapeAtPosition(shape, hitPoint);
            }
        }

        /*
        void TryBuild()
        {
            // TODO
        }
        */

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
