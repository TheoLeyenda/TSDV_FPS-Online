using System;
using System.Collections.Generic;
using System.IO;
using ENet;

namespace Server
{

    class Program
    {
        struct InfoPlayer
        {
            public struct Position
            {
                public float x;
                public float y;
                public float z;
            }
            public struct EulerRotation
            {
                public float x;
                public float y;
                public float z;
            }
            public struct EularRotationPivotWeapon
            {
                public float x;
                public float y;
                public float z;
            }
            public Position position;
            public EulerRotation rotation;
            public EularRotationPivotWeapon rotationPivotWeapon;
        }

       
        static Host _server = new Host();
        private static Dictionary<uint, InfoPlayer> _players = new Dictionary<uint, InfoPlayer>();

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
            PositionAndRotationUpdateRequest = 4,
            PositionAndRotationUpdateEvent = 5,
            LogoutEvent = 6,
            SpawnBulletEvent = 7,
        }

        static void HandlePacket(ref Event netEvent)
        {
            InfoPlayer transform = new InfoPlayer();

            transform.position.x = 0;
            transform.position.y = 0;
            transform.position.z = 0;

            transform.rotation.x = 0;
            transform.rotation.y = 0;
            transform.rotation.z = 0;

            transform.rotationPivotWeapon.x = 0;
            transform.rotationPivotWeapon.y = 0;
            transform.rotationPivotWeapon.z = 0;

            var readBuffer = new byte[1024];
            var readStream = new MemoryStream(readBuffer);
            var reader = new BinaryReader(readStream);

            readStream.Position = 0;
            netEvent.Packet.CopyTo(readBuffer);
            var packetId = (PacketId)reader.ReadByte();

            if (packetId != PacketId.PositionAndRotationUpdateRequest)
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
                _players.Add(playerId, transform);
            }
            else if (packetId == PacketId.PositionAndRotationUpdateRequest)
            {
                var playerId = reader.ReadUInt32();

                InfoPlayer infoPlayer;

                infoPlayer.position.x = reader.ReadSingle();
                infoPlayer.position.y = reader.ReadSingle();
                infoPlayer.position.z = reader.ReadSingle();

                infoPlayer.rotation.x = reader.ReadSingle();
                infoPlayer.rotation.y = reader.ReadSingle();
                infoPlayer.rotation.z = reader.ReadSingle();

                infoPlayer.rotationPivotWeapon.x = reader.ReadSingle();
                infoPlayer.rotationPivotWeapon.y = reader.ReadSingle();
                infoPlayer.rotationPivotWeapon.z = reader.ReadSingle();

                //Console.WriteLine($"ID: {playerId}, Pos: {x}, {y}");
                BroadcastPositionUpdateEvent(playerId, ref infoPlayer);
            }
            else if(packetId == PacketId.SpawnBulletEvent)
            {
                var playerId = reader.ReadUInt32();
                SendSpawnBulletEvent(playerId);
            }
        }
        static void SendSpawnBulletEvent(uint playerId)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.SpawnBulletEvent, playerId);
            var packet = default(Packet);
            packet.Create(buffer);
            _server.Broadcast(0, ref packet);
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
        static void BroadcastPositionUpdateEvent(uint playerId, ref InfoPlayer infoPlayer)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize((byte)PacketId.PositionAndRotationUpdateEvent, playerId, 
                                            infoPlayer.position.x, infoPlayer.position.y, infoPlayer.position.z, 
                                            infoPlayer.rotation.x, infoPlayer.rotation.y, infoPlayer.rotation.z,
                                            infoPlayer.rotationPivotWeapon.x, infoPlayer.rotationPivotWeapon.y, infoPlayer.rotationPivotWeapon.z);
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
