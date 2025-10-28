
# Isosurface Generation
This is my test project which I used to try and make a highly optimized mesh generator for signed distance fields. I tested both marching cubes and surface nets with the Unity JOBS system, which ended up being faster than my prior experiments with Compute Shaders.
This is intended for use as reference, not as a game-ready system. I will be using the results of this experiment for a terrain system for my latest game project.

Built with Unity 6000.2.8f1

## Benchmarks
Around 0.6 ms per chunk of size 32^3 voxels:
- Density generation: 0.2 ms
- Marching Cubes : 0.4 ms

This was tested with a 12th Gen Intel Core I9-12900K, and is dependent on the number and size of shapes in the scene.

## Features
- Per-shape blending sharpness value.
- Realtime shape editing, additive & subtractive SDF's.
- Fast surface painting and chunk regeneration.
- Smooth normals and vertex de-duplication for optimized meshes.
- Marching Cubes and Surface Nets meshing methods.
- Optimized with Unity JOBS and SIMD instructions.

## Materials
https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
https://paulbourke.net/geometry/polygonise/
https://eetumaenpaa.fi/blog/marching-cubes-optimizations-in-unity/#voxelcorners-vs-stackalloc
https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff