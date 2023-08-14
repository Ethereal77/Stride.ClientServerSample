// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Network;
using Stride.Games;
using Stride.Physics;

namespace Stride.ClientServerSample.Server;

/// <summary>
///   A <see cref="Game"/>-like object that implements the typical game lifetime
///   and loop methods, but does not initialize graphics system or any other
///   non-necessary systems.
/// </summary>
internal class HeadlessGame
{
    /// <inheritdoc cref="GameBase.Services"/>
    public ServiceRegistry Services { get; private set; }

    /// <inheritdoc cref="GameBase.Content"/>
    public ContentManager Content { get; private set; }

    /// <summary>
    ///   Server application entry point.
    /// </summary>
    private static void Main() => new HeadlessGame().Run().Wait();

    /// <inheritdoc cref="GameBase.Run"/>
    public async Task Run()
    {
        Services = new ServiceRegistry();

        // Database file provider
        var objDb = ObjectDatabase.CreateDefaultDatabase();
        var dbFileProvider = new DatabaseFileProvider(objDb);
        var dbFileProviderService = new DatabaseFileProviderService(dbFileProvider);

        Services.AddService<IDatabaseFileProviderService>(dbFileProviderService);

        // Content manager
        Content = new ContentManager(Services);
        Services.AddService<IContentManager>(Content);
        Services.AddService(Content);

        //Services.AddService<IGraphicsDeviceService>(new GraphicsDeviceServiceLocal(null));

        // Game systems
        var gameSystems = new GameSystemCollection(Services);
        Services.AddService<IGameSystemCollection>(gameSystems);
        gameSystems.Initialize();

        // Load scene (physics only)
        var loadSettings = new ContentManagerLoaderSettings
        {
            // Ignore all references (Model, etc...)
            ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType()
        };
        var scene = await Content.LoadAsync<Scene>("MainScene", loadSettings);
        var sceneInstance = new SceneInstance(Services, scene, ExecutionMode.None);
        var sceneSystem = new SceneSystem(Services)
        {
            SceneInstance = sceneInstance
        };
        Services.AddService(sceneSystem);

        var physics = new PhysicsProcessor();
        sceneInstance.Processors.Add(physics);

        var socket = new SimpleSocket();
        socket.Connected += clientSocket =>
        {
            Console.WriteLine("Client connected");

            var reader = new BinarySerializationReader(clientSocket.ReadStream);
            while (true)
            {
                // Receive ray start/end
                var start = reader.Read<Vector3>();
                var end = reader.Read<Vector3>();

                // Raycast
                var result = physics.Simulation.Raycast(start, end);
                Console.WriteLine($"Performing raycast: {(result.Succeeded ? "hit" : "miss")}");

                // Send result
                clientSocket.WriteStream.WriteByte((byte)(result.Succeeded ? 1 : 0));
                clientSocket.WriteStream.Flush();
            }
        };
        await socket.StartServer(port: 2655, singleConnection: false);

        Console.WriteLine("Server listening. Press a key to exit");
        Console.ReadKey();
    }
}
