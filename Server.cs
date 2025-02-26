using Lidgren.Network;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace MyRpgEngine
{
    class Server
    {
        private NetServer server;
        private Dictionary<long, PlayerData> playerData;

        public Server()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("MyRpg");
            config.Port = 12345;
            server = new NetServer(config);
            playerData = new Dictionary<long, PlayerData>();
            server.Start();
            Console.WriteLine("Server started on port 12345...");
        }

        public void Run()
        {
            while (true)
            {
                NetIncomingMessage msg;
                while ((msg = server.ReadMessage()) != null)
                {
                    long id = msg.SenderConnection.RemoteUniqueIdentifier;
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            float x = msg.ReadFloat();
                            float y = msg.ReadFloat();
                            int health = msg.ReadInt32();
                            int mana = msg.ReadInt32();
                            bool isAttacking = msg.ReadBoolean();
                            Console.WriteLine($"Received - ID {id} Health: {health}, Attacking: {isAttacking}");

                            // Update player data
                            if (!playerData.ContainsKey(id))
                                playerData[id] = new PlayerData();
                            playerData[id].Position = new Vector2(x, y);
                            playerData[id].Mana = mana;
                            playerData[id].IsAttacking = isAttacking;

                            // Apply combat
                            if (isAttacking)
                            {
                                foreach (var target in playerData)
                                {
                                    if (target.Key != id)
                                    {
                                        float distance = Vector2.Distance(playerData[id].Position, target.Value.Position);
                                        if (distance < 150)
                                        {
                                            Console.WriteLine($"Before Hit - Target {target.Key} Health: {target.Value.Health}");
                                            target.Value.Health = Math.Max(0, target.Value.Health - 10);
                                            Console.WriteLine($"After Hit - Target {target.Key} Health: {target.Value.Health}");
                                        }
                                    }
                                }
                            }

                            // Send updated data
                            NetOutgoingMessage outMsg = server.CreateMessage();
                            outMsg.Write(playerData.Count);
                            foreach (var kvp in playerData)
                            {
                                outMsg.Write(kvp.Key);
                                outMsg.Write(kvp.Value.Position.X);
                                outMsg.Write(kvp.Value.Position.Y);
                                outMsg.Write(kvp.Value.Health);
                                outMsg.Write(kvp.Value.Mana);
                                outMsg.Write(kvp.Value.IsAttacking);
                                Console.WriteLine($"Sending - ID {kvp.Key} Health: {kvp.Value.Health}");
                            }
                            server.SendToAll(outMsg, NetDeliveryMethod.Unreliable);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (msg.SenderConnection.Status == NetConnectionStatus.Connected)
                                Console.WriteLine($"Player {id} connected");
                            else if (msg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                Console.WriteLine($"Player {id} disconnected");
                                playerData.Remove(id);
                            }
                            break;
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }
    }

    class PlayerData
    {
        public Vector2 Position { get; set; }
        public int Health { get; set; } = 100; // Default value
        public int Mana { get; set; } = 50;
        public bool IsAttacking { get; set; }
    }
}