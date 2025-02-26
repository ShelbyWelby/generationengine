using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Lidgren.Network;
using System.Collections.Generic;

namespace MyRpgEngine
{
    class Game : GameWindow
    {
        private Player player;
        private NetClient client;
        private Dictionary<long, Vector2> otherPlayers;

        public Game() : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
        {
            Size = new Vector2i(800, 600),
            Title = "My Co-op RPG",
            Profile = ContextProfile.Compatability
        })
        {
            NetPeerConfiguration config = new NetPeerConfiguration("MyRpg");
            client = new NetClient(config);
            otherPlayers = new Dictionary<long, Vector2>();
        }

        protected override void OnLoad()
        {
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Viewport(0, 0, 800, 600);
            GL.Ortho(0, 800, 600, 0, -1, 1);
            GL.Disable(EnableCap.DepthTest);
            player = new Player();

            client.Start();
            client.Connect("127.0.0.1", 12345); // Localhost for testing
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            player.Update((float)args.Time, this.KeyboardState);

            // Send position to server
            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write(player.Position.X);
            msg.Write(player.Position.Y);
            client.SendMessage(msg, NetDeliveryMethod.Unreliable);

            // Receive updates from server
            NetIncomingMessage inc;
            while ((inc = client.ReadMessage()) != null)
            {
                if (inc.MessageType == NetIncomingMessageType.Data)
                {
                    int count = inc.ReadInt32();
                    otherPlayers.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        long id = inc.ReadInt64();
                        float x = inc.ReadFloat();
                        float y = inc.ReadFloat();
                        if (id != client.UniqueIdentifier) // Don’t add self
                            otherPlayers[id] = new Vector2(x, y);
                    }
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            player.Render(); // Your red square

            // Render other players as blue squares
            foreach (var pos in otherPlayers.Values)
            {
                GL.Begin(PrimitiveType.Quads);
                GL.Color3(0.0f, 0.0f, 1.0f); // Blue
                GL.Vertex2(pos.X - 50, pos.Y - 50);
                GL.Vertex2(pos.X + 50, pos.Y - 50);
                GL.Vertex2(pos.X + 50, pos.Y + 50);
                GL.Vertex2(pos.X - 50, pos.Y + 50);
                GL.End();
            }

            OpenTK.Graphics.OpenGL.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
                System.Console.WriteLine("OpenGL Error: " + error);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            client.Shutdown("Game closed");
            client.Shutdown("Client shutdown");
        }
    }


    class Player
    {
        public Vector2 Position { get; set; } = new Vector2(400, 300);
        public float Speed { get; set; } = 200f;

        public void Update(float deltaTime, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.A)) Position += new Vector2(-Speed * deltaTime, 0);
            if (keyboard.IsKeyDown(Keys.D)) Position += new Vector2(Speed * deltaTime, 0);
            if (keyboard.IsKeyDown(Keys.W)) Position += new Vector2(0, -Speed * deltaTime);
            if (keyboard.IsKeyDown(Keys.S)) Position += new Vector2(0, Speed * deltaTime);
        }

        public void Render()
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(1.0f, 0.0f, 0.0f); // Red
            GL.Vertex2(Position.X - 50, Position.Y - 50);
            GL.Vertex2(Position.X + 50, Position.Y - 50);
            GL.Vertex2(Position.X + 50, Position.Y + 50);
            GL.Vertex2(Position.X - 50, Position.Y + 50);
            GL.End();
        }
    }
}