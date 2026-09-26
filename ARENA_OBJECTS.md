# Walkable arena

Stop Play Mode and choose **Wizard Prototype > Add Walkable Arena**. This adds
missing surfaces without rebuilding the scene or replacing existing surfaces.

In the Hierarchy, expand **Arena Surfaces** and select a named floor, stair or
platform. Enable Gizmos to see its collision box. Each object has:

- **Box Collider 2D:** position and size of its collision geometry.
- **Walkable:** enables the surface in the wizard's movement system.
- **Jump Through:** allows passage from below; landing from above still works.
- **Speed Multiplier:** walking speed while standing on this object.
- **Jump Multiplier:** jump strength when launching from this object.

The image itself is still background art. The separately named collision objects
align to its foreground floor, stairs and platforms; distant ruins are scenery.
Moving a collision object does not move the corresponding painted background art.
To create a new independent prop, add a Sprite Renderer and an Arena Surface
component to a GameObject and size its Box Collider 2D to the visible prop.

The wizard uses swept collision checks for this prototype, not Rigidbody2D forces.
Floor and stairs are solid; floating platforms can be jumped through from below.
The setup increases Jump Speed to 10.5 so the central and upper platforms are
reachable and moves the portal onto the unobstructed central floor.
