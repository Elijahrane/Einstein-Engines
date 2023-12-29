Hey, sorry that maps are in textures.
The Textures folder seems to be one of the only ones the client can load from. I cannot find documentation on how that works so I'll just shove these in here.

Format documented below:

_wallAtlas[,,] contains the arrays for the walls, floor, and ceiling in that order.

Make sure all 3 arrays are the same size.
You have 9 slots in your atlas, effectively, unless you want to go change stuff to hexadecimal real quick. 0 is reserved and has special behaviors: For the wall array, it represents a floor. For the ceiling array, it represents lack of a ceiling (you can see the skybox), for the floor array, it is reserved for I dunno, pits or something.

Terminate all lines with @ (All of the fun functions that let us standardize end lines after reading a file are blocked by sandbox, as are basically all text reading ones)


