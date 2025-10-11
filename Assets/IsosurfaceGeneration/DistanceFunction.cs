using UnityEngine;

namespace IsosurfaceGeneration
{
    public static class DistanceFunction
    {
        public static float Sphere(Vector3 centre, float radius)
        {
            return centre.magnitude - radius;
        }
    }
}
