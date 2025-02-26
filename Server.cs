using Lidgren.Network;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace MyRpgEngine
{
    class PlayerData
    {
        public Vector2 Position { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }   
    }

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
                    long id = msg.SenderConnection.RemoteUniqueIdentifier; // Moved here
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            float x = msg.ReadFloat();
                            float y = msg.ReadFloat();
                            int health = msg.ReadInt32();
                            int mana = msg.ReadInt32();
                            playerData[id] = new PlayerData { Position = new Vector2(x, y), Health = health, Mana = mana };

                            NetOutgoingMessage outMsg = server.CreateMessage();
                            outMsg.Write(playerData.Count);
                            foreach (var kvp in playerData)
                            {
                                outMsg.Write(kvp.Key);
                                outMsg.Write(kvp.Value.Position.X);
                                outMsg.Write(kvp.Value.Position.Y);
                                outMsg.Write(health); // Send health as well
                                outMsg.Write(mana); // Send mana as well
                            }
                            server.SendToAll(outMsg, NetDeliveryMethod.Unreliable);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            if (msg.SenderConnection.Status == NetConnectionStatus.Connected)
                                Console.WriteLine($"Player {id} connected");
                            else if (msg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                            {
                                Console.WriteLine($"Player {id} disconnected");
                                // Remove player from the list
                                if (playerData.ContainsKey(id))
                                    playerData.Remove(id);
                            }
                            playerData.Remove(id);
                            break;
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}