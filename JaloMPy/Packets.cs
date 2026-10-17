using JaLoader;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JaloMPy
{
    public class RequestSavePacket : INetSerializable
    {
        public RequestSavePacket() { }

        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    public class SaveDataPacket : INetSerializable
    {
        public byte[] FileData;

        public SaveDataPacket() { }
        public SaveDataPacket(byte[] fileData)
        {
            FileData = fileData;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutBytesWithLength(FileData);
        }

        public void Deserialize(NetDataReader reader)
        {
            FileData = reader.GetBytesWithLength();
        }
    }

    public class SyncCompletedPacket : INetSerializable
    {
        public SyncCompletedPacket() { }

        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    public class StartGamePacket : INetSerializable
    {
        public StartGamePacket() { }

        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    public class LoadedGamePacket : INetSerializable
    {
        public LoadedGamePacket() { }

        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    public class LobbyInfo : INetSerializable
    {
        public bool IsUsingHostSave;

        public LobbyInfo() { }

        public void Serialize(NetDataWriter writer) { writer.Put(IsUsingHostSave); }
        public void Deserialize(NetDataReader reader) { IsUsingHostSave = reader.GetBool(); }
    }

    public class UsernamePacket : INetSerializable
    {
        public string Username;

        public UsernamePacket() { }

        public UsernamePacket(string username)
        {
            Username = username;
        }

        public void Serialize(NetDataWriter w)
        {
            w.Put(Username);
        }

        public void Deserialize(NetDataReader r)
        {
            Username = r.GetString();
        }
    }

    public class ChatMessagePacket
    {
        public string Message;
        public ChatMessagePacket() { }
        public ChatMessagePacket(string message) { Message = message; }
    }

    public class PlayerTransformPacket : INetSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public PlayerTransformPacket() { }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Position.x);
            writer.Put(Position.y);
            writer.Put(Position.z);

            writer.Put(Rotation.x);
            writer.Put(Rotation.y);
            writer.Put(Rotation.z);
            writer.Put(Rotation.w);
        }

        public void Deserialize(NetDataReader reader)
        {
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }

    public class CarTransformPacket : INetSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public CarTransformPacket() { }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Position.x);
            writer.Put(Position.y);
            writer.Put(Position.z);

            writer.Put(Rotation.x);
            writer.Put(Rotation.y);
            writer.Put(Rotation.z);
            writer.Put(Rotation.w);
        }

        public void Deserialize(NetDataReader reader)
        {
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }

    public class ObjectInteractionPacket : INetSerializable
    {
        public int NetworkObjectID;
        public bool PickedUp;
        public Vector3 CurrentPosition;
        public string ResourceName;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetworkObjectID);
            writer.Put(ResourceName);
            writer.Put(PickedUp);
            writer.Put(CurrentPosition.x);
            writer.Put(CurrentPosition.y);
            writer.Put(CurrentPosition.z);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetworkObjectID = reader.GetInt();
            ResourceName = reader.GetString();
            PickedUp = reader.GetBool();
            CurrentPosition = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }
}
