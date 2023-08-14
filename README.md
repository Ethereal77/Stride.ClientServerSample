# Stride.ClientServerSample

Simple Stride Game and its corresponding server that processes physics raycasts remotely.

Press space or right click to "fire" and server will check if the sphere is hit.

## Build

Make sure to run the server first, then the game.

Setting both projects as startup projects works fine too (right click on the solution -> **Set Startup Projects**).

## Future

Right now the server is a quick proof of concept that uses the Stride API manually to load a scene.

Later it might be easier to have a `HeadlessGame` type to automatize the loading of a scene without graphics API and still be able to process specific C# scripts.
