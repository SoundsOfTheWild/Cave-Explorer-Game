# Cave-Explorer-Game
A collection of scripts for a procedural cave platformer game

Caves:
Caves are procedurally generated using a base of noise, a number of smoothing iterations, followed by a connectivity procedure so that the
entire map is one conected room.
This is then converted into a mesh using matching squares.
The map can be generated with set seeds to reproduce results or random seeds for different results each time.

Player:
The player is controlled using a custom raycast based collision system that handles jumps, double jumps, wall sliding and slope ascending
and descending. The graphics for the player seen in some of the demo videos were made by a friend of mine and animated in Unity by myself.

For video demonstrations of these systems please watch the following:

https://youtu.be/sSbCTea-nmU

https://youtu.be/fzP0z9X7WNc

https://youtu.be/fyHizngdS9E

https://youtu.be/xexXHLziAz8

