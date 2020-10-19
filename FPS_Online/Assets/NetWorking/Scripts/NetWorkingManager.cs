using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ENet;
using Server;
using Random = UnityEngine.Random;


public class NetWorkingManager : MonoBehaviour
{

    //public GameObject myPlayerFactory;
    public PlayerData otherPlayerFactory;

    [SerializeField] private PlayerData _myPlayer = null;
    private uint _myPlayerId;

    private Host _client;
    private Peer _peer;
    private int _skipFrame = 0;
    private Dictionary<uint, PlayerData> _players = new Dictionary<uint, PlayerData>();

    const int channelID = 0;

    void Start()
    {
        Screen.SetResolution(640, 480, false);
        Application.runInBackground = true;
        InitENet();
        //_myPlayer = Instantiate(myPlayerFactory);
    }
    void FixedUpdate()
    {
        UpdateENet();

        if (++_skipFrame < 3)
            return;

        SendPositionAndRotationUpdate();
        _skipFrame = 0;
    }

    void OnDestroy()
    {
        _client.Dispose();
        ENet.Library.Deinitialize();
    }

    private void InitENet()
    {
        const string ip = "127.0.0.1";
        const ushort port = 6005;

        //const string ip = "181.31.2.215";
        //const ushort port = 8900;

        ENet.Library.Initialize();
        _client = new Host();
        Address address = new Address();

        address.SetHost(ip);
        address.Port = port;
        _client.Create();
        Debug.Log("Connecting");
        _peer = _client.Connect(address);
    }

    private void UpdateENet()
    {
        ENet.Event netEvent;

        if (_client.CheckEvents(out netEvent) <= 0)
        {
            if (_client.Service(15, out netEvent) <= 0)
                return;
        }

        switch (netEvent.Type)
        {
            case ENet.EventType.None:
                break;

            case ENet.EventType.Connect:
                Debug.Log("Client connected to server - ID: " + _peer.ID);
                SendLogin();
                break;

            case ENet.EventType.Disconnect:
                Debug.Log("Client disconnected from server");
                break;

            case ENet.EventType.Timeout:
                Debug.Log("Client connection timeout");
                break;

            case ENet.EventType.Receive:
                Debug.Log("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                ParsePacket(ref netEvent);
                netEvent.Packet.Dispose();
                break;
        }
    }

    enum PacketId : byte
    {
        LoginRequest = 1,
        LoginResponse = 2,
        LoginEvent = 3,
        PositionAndRotationUpdateRequest = 4,
        PositionAndRotationUpdateEvent = 5,
        LogoutEvent = 6
    }

    private void SendPositionAndRotationUpdate()
    {
        float posX = _myPlayer.myPrefab.transform.position.x;
        float posY = _myPlayer.myPrefab.transform.position.y;
        float posZ = _myPlayer.myPrefab.transform.position.z;

        
        float rotX = _myPlayer.myPrefab.transform.eulerAngles.x;
        float rotY = _myPlayer.myPrefab.transform.eulerAngles.y;
        float rotZ = _myPlayer.myPrefab.transform.eulerAngles.z;

        float rotPivotWeaponX = _myPlayer.pivotWeaponGameObject.transform.eulerAngles.x;
        float rotPivotWeaponY = _myPlayer.pivotWeaponGameObject.transform.eulerAngles.y;
        float rotPivotWeaponZ = _myPlayer.pivotWeaponGameObject.transform.eulerAngles.z;

        var protocol = new Protocol();
        var buffer = protocol.Serialize((byte)PacketId.PositionAndRotationUpdateRequest, _myPlayerId, 
                                       posX, posY, posZ, 
                                       rotX, rotY, rotZ,
                                       rotPivotWeaponX, rotPivotWeaponY, rotPivotWeaponZ);
        var packet = default(Packet);
        packet.Create(buffer);
        _peer.Send(channelID, ref packet);
    }

    private void SendLogin()
    {
        Debug.Log("SendLogin");
        var protocol = new Protocol();
        var buffer = protocol.Serialize((byte)PacketId.LoginRequest, 0);
        var packet = default(Packet);
        packet.Create(buffer);
        _peer.Send(channelID, ref packet);
    }

    private void ParsePacket(ref ENet.Event netEvent)
    {
        var readBuffer = new byte[1024];
        var readStream = new MemoryStream(readBuffer);
        var reader = new BinaryReader(readStream);

        readStream.Position = 0;
        netEvent.Packet.CopyTo(readBuffer);
        var packetId = (PacketId)reader.ReadByte();

        //Debug.Log("ParsePacket received: " + packetId);

        if (packetId == PacketId.LoginResponse)
        {
            _myPlayerId = reader.ReadUInt32();
            Debug.Log("MyPlayerId: " + _myPlayerId);
        }
        else if (packetId == PacketId.LoginEvent)
        {
            var playerId = reader.ReadUInt32();
            Debug.Log("OtherPlayerId: " + playerId);
            SpawnOtherPlayer(playerId);
        }
        else if (packetId == PacketId.PositionAndRotationUpdateEvent)
        {
            var playerId = reader.ReadUInt32();
            
            //LEO POSICIONES
            var posX = reader.ReadSingle();
            var posY = reader.ReadSingle();
            var posZ = reader.ReadSingle();

            //LEO ROTACION
            var rotX = reader.ReadSingle();
            var rotY = reader.ReadSingle();
            var rotZ = reader.ReadSingle();

            var rotPivotWeaponX = reader.ReadSingle();
            var rotPivotWeaponY = reader.ReadSingle();
            var rotPivotWeaponZ = reader.ReadSingle();

            UpdatePosition(playerId, posX, posY, posZ);
            UpdateRotation(playerId, rotX, rotY, rotZ);
            UpdateRotationPivotWeapon(playerId, rotPivotWeaponX, rotPivotWeaponY, rotPivotWeaponZ);
        }
        else if (packetId == PacketId.LogoutEvent)
        {
            var playerId = reader.ReadUInt32();
            if (_players.ContainsKey(playerId))
            {
                Destroy(_players[playerId].myPrefab);
                _players.Remove(playerId);
            }
        }
    }

    private void SpawnOtherPlayer(uint playerId)
    {
        float randomRange = 10.0f;
        float altura = 5.0f;
        if (playerId == _myPlayerId)
            return;
        PlayerData newPlayer = Instantiate(otherPlayerFactory);
        newPlayer.myPrefab.transform.position = newPlayer.myPrefab.transform.position + new Vector3(Random.Range(-randomRange, randomRange), altura, Random.Range(-randomRange, randomRange));
        Debug.Log("Spawn other object " + playerId);
        _players[playerId] = newPlayer;
    }

    private void UpdatePosition(uint playerId, float x, float y, float z)
    {
        if (playerId == _myPlayerId)
            return;

        //Debug.Log("UpdatePosition " + playerId);
        _players[playerId].myPrefab.transform.position = new Vector3(x, y, z);
    }
    private void UpdateRotation(uint playerId, float x, float y, float z)
    {
        if (playerId == _myPlayerId)
            return;

        //Debug.Log("UpdateRotation " + playerId);
        _players[playerId].myPrefab.transform.eulerAngles = new Vector3(x, y, z);
    }
    private void UpdateRotationPivotWeapon(uint playerId, float x, float y, float z)
    {
        if (playerId == _myPlayerId)
            return;

        //Debug.Log("UpdateRotation " + playerId);
        _players[playerId].pivotWeaponGameObject.transform.eulerAngles = new Vector3(x, y, z);
    }
}
