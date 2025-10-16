# Isosurface Generation
This is my test project which I used to try and make a highly optimized mesh generator for signed distance fields. This can be easily adapted into a level editor or terrain generator, as I have done for my own project, though note that changes will need to be made so not all of your chunks are loaded at once.

Note: my first approach was to utilize compute shaders which I naturally assumed would be faster, however the GPU readback slowed it to a crawl. This was before I learned that the predominant approach is to use multi-threading on the CPU.

## Features
- Per-shape sharpness values to blend shapes together.
- Realtime shape editing, additive & subtractive SDF's.
- Fast surface painting and chunk regeneration.
- Marching Cubes and Surface Nets meshing methods.
- Optimized with Unity JOBS and SIMD instructions.

## Materials
https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
https://paulbourke.net/geometry/polygonise/
https://eetumaenpaa.fi/blog/marching-cubes-optimizations-in-unity/#voxelcorners-vs-stackalloc
https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff
