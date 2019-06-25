# MeshCutting
An implementation of run-time dynamic mesh cutting
Implementation based off https://youtu.be/1UsuZsaUUng

# Current  Issues
- Need to fix the box-cast used, as it doesn't quite match up to the line drawn for slicing
- Handle "inner" textures for cuts
- Prevent meshes from being cut if they do not intersect the cutting plane (related to boxcast)
