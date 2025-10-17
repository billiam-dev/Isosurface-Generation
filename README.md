# Isosurface Generation
This is my test project which I used to try and make a highly optimized mesh generator for signed distance fields. This is easily adaptable into a level editor or terrain generator, as I have done for my own project, though note that it is impractical to have all chunks loaded at once for large surfaces.

Note: my first approach was to utilize compute shaders which I naturally assumed would be faster, however the GPU readback slowed it to a crawl. This was before I learned that the predominant approach is to use multi-threading on the CPU.

Built with Unity 6000.2.6f2

## Benchmarks
Around 0.6 ms per chunk of size 32^3 voxels:
^ Density generation: 0.2 ms
^ Marching Cubes : 0.4 ms

Note that this is dependent on the number and size of SDF's.

12th Gen Intel Core I9-12900K

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
