using Lidgren.Network;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace MyRpgEngine
{
    class Server
    {
        private NetServer server;
        private Dictionary<long, Vector2> playerPositions;

        public Server()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("MyRpg");
            config.Port = 12345;
            server = new NetServer(config);
            playerPositions = new Dictionary<long, Vector2>();
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
                            playerPositions[id] = new Vector2(x, y);

                            NetOutgoingMessage outMsg = server.CreateMessage();
                            outMsg.Write(playerPositions.Count);
                            foreach (var kvp in playerPositions)
                            {
                                outMsg.Write(kvp.Key);
                                outMsg.Write(kvp.Value.X);
                                outMsg.Write(kvp.Value.Y);
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
                                if (playerPositions.ContainsKey(id))
                                    playerPositions.Remove(id);
                            }
                                playerPositions.Remove(id);
                            break;
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }
    }
}