using System;
using System.Collections.Generic;
using System.IO;
using ENet;

namespace Server
{

    class Program
    {
        struct Position
        {
            public float x;
            public float y;
        }

        static Host _server = new Host();
        private static Dictionary<uint, Position> _players = new Dictionary<uint, Position>();

        static void Main(string[] args)
        {
            const ushort port = 6005;
            const int maxClients = 100;
            Library.Initialize();

            _server = new Host();
            Address address = new Address();

            address.Port = port;
            _server.Create(address, maxClients);

            Console.WriteLine($"Circle ENet Server started on {port}");

            Event netEvent;
            while (!Console.KeyAvailable)
            {
                bool polled = false;

                while (!polled)
                {
                    if (_server.CheckEvents(out netEvent) <= 0)
                    {
                        if (_server.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            netEvent.Peer.Timeout(32, 1000, 4000);
                            break;

                        case EventType.Disconnect:
                            Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            HandleLogout(netEvent.Peer.ID);
                            break;

                        case EventType.Timeout:
                            Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            HandleLogout(netEvent.Peer.ID);
                            break;

                        case EventType.Receive:
                            //Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                            HandlePacket(ref netEvent);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }

                _server.Flush();
            }
            Library.Deinitialize();
        }

        enum PacketId : byte
        {
            LoginRequest = 1,
            LoginResponse = 2,
            LoginEvent = 3,
            PositionUpdateRequest = 4,
            PositionUpdateEvent = 5,
            LogoutEvent = 6
        }

        static void HandlePacket(ref Event netEvent)
        {
            var readBuffer = new byte[1024];
            var readStream = new MemoryStream(readBuffer);
            var reader = new BinaryReader(readStream);

            readStream.Position = 0;
            netEvent.Packet.CopyTo(readBuffer);
            var packetId = (PacketId)reader.ReadByte();

            if (packetId != PacketId.PositionUpdateRequest)
                Console.WriteLine($"HandlePacket received: {packetId}");

            if (packetId == PacketId.LoginRequest)
            {
                var playerId = netEvent.Peer.ID;
                SendLoginResponse(ref netEvent, playerId);
                BroadcastLoginEvent(playerId);
                foreach (var p in _players)
                {
                    SendLoginEvent(ref netEvent, p.Key);
                }
                _players.Add(playerId, new Position { x = 0.0f, y = 0.0f });
            }
            else if (packetId == PacketId.PositionUpdateRequest)
            {
                var playerId = reader.ReadUInt32();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                //Console.WriteLine($"ID: {playerId}, Pos: {x}, {y}");
                BroadcastPositionUpdateEvent(playerId, x, y);
            }
        }

        //Se llama cuando un cliente desea conectarse al server.
        static void SendLoginResponse(ref Event netEvent, uint playerId)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.LoginResponse, playerId);
            var packet = default(Packet);
            packet.Create(buffer);
            netEvent.Peer.Send(0, ref packet);
        }

        //Registro al player en el server, se llama luego del SendLoginResponse(ref Event netEvent, uint playerId)
        static void BroadcastLoginEvent(uint playerId)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.LoginEvent, playerId);
            var packet = default(Packet);
            packet.Create(buffer);
            _server.Broadcast(0, ref packet);
        }

        //Logeo el cliente al servidor (meto al cliente en el servidor).
        static void SendLoginEvent(ref Event netEvent, uint playerId)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.LoginEvent, playerId);
            var packet = default(Packet);
            packet.Create(buffer);
            netEvent.Peer.Send(0, ref packet);
        }

        //Updateo la posicion del player
        static void BroadcastPositionUpdateEvent(uint playerId, float x, float y)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.PositionUpdateEvent, playerId, x, y);
            var packet = default(Packet);
            packet.Create(buffer);
            _server.Broadcast(0, ref packet);
        }

        //La uso cuando me desconecto
        static void HandleLogout(uint playerId)
        {
            if (!_players.ContainsKey(playerId))
                return;

            _players.Remove(playerId);
            BroadcastLogoutEvent(playerId);
        }

        //Borra correctamente al jugador del servidor ( se debe llamar primero a la funcion HandleLogout(uint playerId) )
        static void BroadcastLogoutEvent(uint playerId)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.LogoutEvent, playerId);
            var packet = default(Packet);
            packet.Create(buffer);
            _server.Broadcast(0, ref packet);
        }
    }
}
