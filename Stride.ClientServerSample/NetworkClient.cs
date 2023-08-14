// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Engine.Network;
using Stride.Core.Serialization;

namespace Stride.ClientServerSample;

/// <summary>
///   Script that connects to a server, sends raycast commands that are processed
///   by the server, and receives the result of those raycasts to display to the user.
/// </summary>
public class NetworkClient : AsyncScript
{
    private bool? lastResult;
    private TimeSpan lastResultTime;

    public override async Task Execute()
    {
        var socket = new SimpleSocket();
        await socket.StartClient(address: "localhost", port: 2655, needAck: true);
        var writer = new BinarySerializationWriter(socket.WriteStream);

        while (Game.IsRunning)
        {
            // Do stuff every new frame
            await Script.NextFrame();

            if (Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsKeyPressed(Keys.Space))
            {
                var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

                // Ask server
                lastResult = await Task.Run(() =>
                {
                    writer.Write(Entity.Transform.Position);
                    writer.Write(Entity.Transform.Position + rotation.Forward * 1000.0f);
                    writer.Flush();

                    // Get result
                    return socket.ReadStream.ReadByte() == 1;
                });
                lastResultTime = Game.UpdateTime.Total;
            }

            // Display last result (max 2 seconds)
            if (lastResult.HasValue)
            {
                DebugText.Print(lastResult.Value ? "Hit!" : "Miss...", new Int2(GraphicsDevice.Presenter.BackBuffer.Width / 2, (int)(GraphicsDevice.Presenter.BackBuffer.Height * 0.6f)));
                if ((Game.UpdateTime.Total - lastResultTime) > TimeSpan.FromSeconds(2.0f))
                    lastResult = null;
            }
        }
    }
}
