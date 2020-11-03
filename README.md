# ProceduralMeshSlicing
Dynamicsly slicing mesh objects in Unity

Works for both convex and concave meshes using the Ear Clipping algorithm

Place the MeshSlicer script on a gameobject you want to slice with and select the layer you want to slice.

A data correction can be done for the new slices which will result in cleaner meshes but takes a longer time to calculate.
The MeshSlicer works for both concave and convex meshes. When only slicing convex meshes 'Concave' should be unchecked. When slicing both concave and convex meshes 'Concave' should be checked.

When 'Concave' is checked some slices may throw errors. This has to do with the direction and slicing point of the slicing plane.

by Jasper van den Barg
