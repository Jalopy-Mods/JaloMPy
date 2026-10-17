using JaLoader;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Console = JaLoader.Console;

namespace JaloMPy
{
    public class NetworkSceneManager : MonoBehaviour
    {
        internal JaloMPy mod;
        public static NetworkSceneManager instance { get; private set; }

        internal GameObject playerPrefab;
        internal int currentObjectId = 0;

        internal Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
        internal Dictionary<int, GameObject> syncedObjects = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
            {
                Destroy(instance.gameObject);
                instance = this;
            }
        }

        internal void Initialize()
        {
            playerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(playerPrefab.GetComponent<Collider>());
            playerPrefab.SetActive(false);

            ModHelper.Instance.player.AddComponent<NetworkPlayer>();
        }

        void Start()
        {
            
        }

        internal RemotePlayerInterpolation GetRemotePlayer(int id)
        {
            return players[id].GetComponent<RemotePlayerInterpolation>();
        }

        internal void AddSyncedObject(GameObject obj)
        {
            syncedObjects.Add(currentObjectId, obj);
            obj.AddComponent<NetworkID>();
            obj.GetComponent<NetworkID>().id = currentObjectId;
            currentObjectId++;
        }

        internal GameObject GetSyncedObject(int id)
        {
            if (id >= currentObjectId)
                return null;
            return syncedObjects[id];
        }

        internal void AddPlayer(int playerID, string username)
        {
            var go = Instantiate(playerPrefab);
            go.SetActive(true);
            go.AddComponent<RemotePlayerInterpolation>();
            players.Add(playerID, go);
        }

        internal GameObject GetPlayerObject(int playerID)
        {
            return players[playerID];
        }
    }

    public class NetworkID : MonoBehaviour
    {
        public int id;
    }
    
    public class RemotePlayerInterpolation : MonoBehaviour
    {
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        [SerializeField] private float interpolationSpeed = 15f;

        private void Start()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        public void OnNewTransformReceived(Vector3 newPos, Quaternion newRot)
        {
            targetPosition = newPos;
            targetRotation = newRot;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * interpolationSpeed);
        }
    }

    public class NetworkPlayer : MonoBehaviour
    {
        private void Awake()
        {
            NetworkConnectionManager.instance.On20HzTick += SendPositionPacket;
        }

        public void SendPositionPacket()
        {
            PlayerTransformPacket OutboundPacket = new PlayerTransformPacket
            {
                Position = transform.position,
                Rotation = transform.rotation
            };

            NetworkConnectionManager.SendPacketToAll<PlayerTransformPacket>(OutboundPacket, DeliveryMethod.Unreliable);
        }
    }
}
