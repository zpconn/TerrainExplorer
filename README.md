TerrainExplorer
===============

Terrain Explorer is a terrain renderer built using XNA 2.0. (Note: it has not been updated to work with newer versions; don't necessarily expect forward-compatibility.) It uses a custom version of Chunked LOD to dynamically manage the resolution of the terrain based on the viewer's position and the complexity of the geometry. It minimizes the amount of geometry stored in memory by creating a single small grid and dynamically displacing it in a vertex shader to draw each terrain chunk using vertex texture fetches. The terrain is also dynamically normal mapped. 

Finally, it has a rudimentary grass renderer in place that will render grass if the camera is in close enough proximity. 

In future versions, real-time water and some sort of sky (ideally atmospheric scattering) will be included. 

I've included the full source to the project and all the project files in the belief that I should give a little back to a community from which I've learned a lot.
