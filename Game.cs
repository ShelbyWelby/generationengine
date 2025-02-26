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
        private Dictionary<long, PlayerData> otherPlayers;

        public Game() : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
        {
            Size = new Vector2i(800, 600),
            Title = "My Co-op RPG",
            Profile = ContextProfile.Compatability
        })
        {
            NetPeerConfiguration config = new NetPeerConfiguration("MyRpg");
            client = new NetClient(config);
            otherPlayers = new Dictionary<long, PlayerData>();
        }

        protected override void OnLoad()
        {
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Viewport(0, 0, 800, 600);
            GL.Ortho(0, 800, 600, 0, -1, 1);
            GL.Disable(EnableCap.DepthTest);
            player = new Player();

            client.Start();
            client.Connect("127.0.0.1", 12345);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            player.Update((float)args.Time, this.KeyboardState);

            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write(player.Position.X);
            msg.Write(player.Position.Y);
            msg.Write(player.Health);
            msg.Write(player.Mana);
            client.SendMessage(msg, NetDeliveryMethod.Unreliable);

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
                        int health = inc.ReadInt32();
                        int mana = inc.ReadInt32();
                        if (id != client.UniqueIdentifier)
                            otherPlayers[id] = new PlayerData { Position = new Vector2(x, y), Health = health, Mana = mana };
                    }
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            player.Render();

            foreach (var data in otherPlayers.Values)
            {
                // Other player square
                GL.Begin(PrimitiveType.Quads);
                GL.Color3(0.0f, 0.0f, 1.0f); // Blue
                GL.Vertex2(data.Position.X - 50, data.Position.Y - 50);
                GL.Vertex2(data.Position.X + 50, data.Position.Y - 50);
                GL.Vertex2(data.Position.X + 50, data.Position.Y + 50);
                GL.Vertex2(data.Position.X - 50, data.Position.Y + 50);
                GL.End();

                // Health bar for other player
                float healthWidth = (data.Health / 100f) * 100;
                GL.Begin(PrimitiveType.Quads);
                GL.Color3(0.0f, 1.0f, 0.0f); // Green
                GL.Vertex2(data.Position.X - 50, data.Position.Y - 70);
                GL.Vertex2(data.Position.X - 50 + healthWidth, data.Position.Y - 70);
                GL.Vertex2(data.Position.X - 50 + healthWidth, data.Position.Y - 60);
                GL.Vertex2(data.Position.X - 50, data.Position.Y - 60);
                GL.End();
            }

            OpenTK.Graphics.OpenGL.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
                System.Console.WriteLine("OpenGL Error: " + error);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            client.Disconnect("Window closed");
            client.Shutdown("Client shutdown");
        }
    }

    class Player
    {
        public Vector2 Position { get; set; } = new Vector2(400, 300);
        public float Speed { get; set; } = 200f;
        public int Health { get; set; } = 100;
        public int Mana { get; set; } = 50;

        public void Update(float deltaTime, KeyboardState keyboard)
        {
            Vector2 newPos = Position;
            if (keyboard.IsKeyDown(Keys.A))
            {
                newPos += new Vector2(-Speed * deltaTime, 0);
                System.Console.WriteLine("A pressed");
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                newPos += new Vector2(Speed * deltaTime, 0);
                System.Console.WriteLine("D pressed");
            }
            if (keyboard.IsKeyDown(Keys.W))
            {
                newPos += new Vector2(0, -Speed * deltaTime);
                System.Console.WriteLine("W pressed");
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                newPos += new Vector2(0, Speed * deltaTime);
                System.Console.WriteLine("S pressed");
            }
            Position = Vector2.Clamp(newPos, new Vector2(50, 50), new Vector2(750, 550));
            System.Console.WriteLine("New Position: " + Position);
        }

        public void Render()
        {
            // Player square
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(1.0f, 0.0f, 0.0f); // Red
            GL.Vertex2(Position.X - 50, Position.Y - 50);
            GL.Vertex2(Position.X + 50, Position.Y - 50);
            GL.Vertex2(Position.X + 50, Position.Y + 50);
            GL.Vertex2(Position.X - 50, Position.Y + 50);
            GL.End();

            // Health bar: 100px wide max, 10px tall, 20px above player
            float healthWidth = (Health / 100f) * 100; // Scale 0-100
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(0.0f, 1.0f, 0.0f); // Green
            GL.Vertex2(Position.X - 50, Position.Y - 70); // Top-left
            GL.Vertex2(Position.X - 50 + healthWidth, Position.Y - 70); // Top-right
            GL.Vertex2(Position.X - 50 + healthWidth, Position.Y - 60); // Bottom-right
            GL.Vertex2(Position.X - 50, Position.Y - 60); // Bottom-left
            GL.End();
        }
    }
}