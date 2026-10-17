using JaLoader;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using UnityEngine;
using Console = JaLoader.Console;

namespace JaloMPy
{
    public class NetworkConnectionManager : MonoBehaviour
    {
        public static NetworkConnectionManager instance { get; private set; }

        internal NetManager netManager;
        private EventBasedNetListener listener;
        private NetPacketProcessor packetProcessor;
        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

        private const float TICK_RATE = 0.05f;
        private float tickTimer = 0f;
        private const float SLOW_TICK_RATE = 0.20f;
        private float slowTickTimer = 0f;

        internal List<int> players = new List<int>();
        internal Dictionary<int, string> playerIdAndName = new Dictionary<int, string>();

        internal bool inGame = false;
        internal bool useCurrentSave;
        internal bool allClientsIn;

        public delegate void NetworkTickEvent();
        public event NetworkTickEvent On20HzTick;

        internal JaloMPy mod;

        void Awake()
        {
            if(instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            Initialize();
        }

        internal bool IsHost()
        {
            return NetworkConnectionManager.instance.players.Contains(-1);
        }

        void Initialize()
        {
            InitializePacketRouter();
            EventsManager.Instance.OnFinishedAddingGameExtensions += SetInGameStatus;
        }

        public void SetInGameStatus()
        {
            if(netManager != null)
                inGame = true;

            var inGamePacket = new LoadedGamePacket();
            SendPacketToAll<LoadedGamePacket>(inGamePacket, DeliveryMethod.ReliableOrdered);

            if(!allClientsIn)
                mod.WaitForAllPlayersLoaded();
        }

        internal void Disconnect()
        {
            if (netManager != null)
            {
                netManager.Stop();
                netManager = null;
                listener = null;

                NetworkUIManager.instance.Disconnected();
            }

            playerIdAndName.Clear();
            players.Clear();
        }

        internal static void SendPacketToAll<T>(T packetToSend, DeliveryMethod deliveryMethod) where T: INetSerializable
        {
            NetworkConnectionManager.instance.netManager.SendToAll(NetworkConnectionManager.instance.packetProcessor.WriteNetSerializable<T>(packetToSend), deliveryMethod);
        }

        private void AddPlayer(int id)
        {
            players.Add(id);
        }

        private void AllPlayersLoaded()
        {
            allClientsIn = true;

            var go = Instantiate(new GameObject());
            go.name = "NetworkManager";
            var scr = go.AddComponent<NetworkSceneManager>();
            scr.mod = mod;
            scr.Initialize();

            scr.AddPlayer(0, playerIdAndName[0]);
        }

        private void SetPlayerUsername(int id, string username)
        {
            playerIdAndName.Add(id, username);
        }

        internal void SendRequestSave()
        {
            var requestPacket = new RequestSavePacket();

            netManager.FirstPeer.Send(
                packetProcessor.Write(requestPacket),
                DeliveryMethod.ReliableOrdered
            );
        }

        internal void Host(int port, bool currentSave, string password)
        {
            Disconnect();

            if (string.IsNullOrEmpty(password))
                password = "JalopyMultiplayerToken";

            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            if (mod.simulateExtremeLag)
            {
                netManager.SimulateLatency = true;
                netManager.SimulatePacketLoss = true;
                netManager.SimulationMinLatency = 2000;
                netManager.SimulationMaxLatency = 4000;
                netManager.SimulationPacketLossChance = 5;
            }

            netManager.Start(port);

            useCurrentSave = currentSave;

            listener.ConnectionRequestEvent += request =>
            {
                if (netManager.ConnectedPeersCount < 3)
                    request.AcceptIfKey(password);
                else
                    request.Reject();
            };

            AddPlayer(-1);
            playerIdAndName.Add(-1, NetworkUIManager.instance.PlayerUsername());

            listener.PeerConnectedEvent += (peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    AddPlayer(peer.Id);

                    string username = NetworkUIManager.instance.PlayerUsername();
                    var usernamePacket = new UsernamePacket(username);
                    peer.Send(packetProcessor.WriteNetSerializable<UsernamePacket>(usernamePacket), DeliveryMethod.ReliableOrdered);

                    var lobbyPacket = new LobbyInfo
                    {
                        IsUsingHostSave = useCurrentSave
                    };
                    peer.Send(packetProcessor.WriteNetSerializable<LobbyInfo>(lobbyPacket), DeliveryMethod.ReliableOrdered);
                });
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Disconnect();
            };

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                packetProcessor.ReadAllPackets(dataReader, fromPeer);
            };

            packetProcessor.SubscribeNetSerializable<RequestSavePacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    if (!useCurrentSave)
                    {
                        NetworkUIManager.instance.UpdateSyncedStatus();
                        return;
                    }

                    string hostSavePath = Path.Combine(Path.Combine(Application.persistentDataPath, "Saves"), "Save.sfminsk");

                    if (!File.Exists(hostSavePath))
                        return;

                    byte[] saveBytes = File.ReadAllBytes(hostSavePath);

                    var dataPacket = new SaveDataPacket(saveBytes);

                    peer.Send(
                        packetProcessor.WriteNetSerializable(dataPacket),
                        DeliveryMethod.ReliableOrdered
                    );

                });
            }, () => new RequestSavePacket());

            packetProcessor.SubscribeNetSerializable<SyncCompletedPacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    NetworkUIManager.instance.UpdateSyncedStatus();
                });
            }, () => new SyncCompletedPacket());
        }

        internal void Join(string ip, int port, string password)
        {
            Disconnect();

            if (string.IsNullOrEmpty(password))
                password = "JalopyMultiplayerToken";

            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            if (mod.simulateExtremeLag)
            {
                netManager.SimulateLatency = true;
                netManager.SimulatePacketLoss = true;
                netManager.SimulationMinLatency = 2000;
                netManager.SimulationMaxLatency = 4000;
                netManager.SimulationPacketLossChance = 5;
            }

            listener.PeerConnectedEvent += (peer) =>
            {
                EnqueueMainThreadAction(() => {
                    AddPlayer(peer.Id);

                    string username = NetworkUIManager.instance.PlayerUsername();
                    var usernamePacket = new UsernamePacket(username);
                    netManager.FirstPeer.Send(packetProcessor.WriteNetSerializable<UsernamePacket>(usernamePacket), DeliveryMethod.ReliableOrdered);

                    NetworkUIManager.instance.OpenServerLobbyUI();
                });
            };
            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Disconnect();
            };

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                packetProcessor.ReadAllPackets(dataReader, fromPeer);
            };

            packetProcessor.SubscribeNetSerializable<SaveDataPacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    string saveFilePath = Path.Combine(Path.Combine(Application.persistentDataPath, "Saves"), "Save.sfminsk");

                    File.WriteAllBytes(saveFilePath, packet.FileData);
                    NetworkUIManager.instance.UpdateSyncedStatus();

                    var completedPacket = new SyncCompletedPacket();
                    peer.Send(
                        packetProcessor.WriteNetSerializable(completedPacket),
                        DeliveryMethod.ReliableOrdered
                    );
                });
            }, () => new SaveDataPacket());

            packetProcessor.SubscribeNetSerializable<LobbyInfo, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    useCurrentSave = packet.IsUsingHostSave;
                });
            }, () => new LobbyInfo());

            packetProcessor.SubscribeNetSerializable<StartGamePacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    mod.StartGameHelper(useCurrentSave);
                });
            }, () => new StartGamePacket());

            netManager.Start();
            netManager.Connect(ip, port, password);
        }

        internal void SendStartPacket()
        {
            var startGamePacket = new StartGamePacket();
            SendPacketToAll<StartGamePacket>(startGamePacket, DeliveryMethod.ReliableOrdered);
        }

        private void InitializePacketRouter()
        {
            packetProcessor = new NetPacketProcessor();

            packetProcessor.SubscribeNetSerializable<LoadedGamePacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    AllPlayersLoaded();
                    mod.AllPlayersLoaded();
                });
            }, () => new LoadedGamePacket());

            packetProcessor.SubscribeNetSerializable<UsernamePacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    SetPlayerUsername(peer.Id, packet.Username);
                    NetworkUIManager.instance.UpdateLobbyUsernames();
                });
            }, () => new UsernamePacket());

            packetProcessor.Subscribe<ChatMessagePacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    Console.Log($"[CHAT] Peer {peer.Id}: {packet.Message}");
                });
            }, () => new ChatMessagePacket());

            packetProcessor.SubscribeNetSerializable<PlayerTransformPacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    RemotePlayerInterpolation puppet = NetworkSceneManager.instance.GetRemotePlayer(peer.Id);
                    puppet.OnNewTransformReceived(packet.Position, packet.Rotation);
                });
            }, () => new PlayerTransformPacket());

            /*packetProcessor.SubscribeNetSerializable<CarTransformPacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    ModHelper.Instance.laika.transform.position = packet.Position;
                    ModHelper.Instance.laika.transform.rotation = packet.Rotation;
                });
            }, () => new CarTransformPacket());

            packetProcessor.SubscribeNetSerializable<ObjectInteractionPacket, NetPeer>((packet, peer) =>
            {
                EnqueueMainThreadAction(() =>
                {
                    if (NetworkSceneManager.instance.GetSyncedObject(packet.NetworkObjectID) == null)
                    {
                        ObjectPickupC[] allItems = FindObjectsOfType<ObjectPickupC>();
                        float margin = 1f;

                        foreach (var item in allItems)
                        {
                            if (!item.gameObject.activeSelf)
                                continue;

                            if (item.gameObject.name != packet.ResourceName)
                                continue;

                            float dist = Vector3.Distance(item.transform.position, packet.CurrentPosition);
                            if (dist < margin)
                                NetworkSceneManager.instance.AddSyncedObject(item.gameObject);
                        }
                    }

                    var obj = NetworkSceneManager.instance.GetSyncedObject(packet.NetworkObjectID);

                    if (packet.PickedUp)
                    {
                        obj.transform.SetParent(NetworkSceneManager.instance.GetPlayerObject(peer.Id).transform);
                        obj.GetComponent<ObjectPickupC>().enabled = false;
                        obj.GetComponent<Rigidbody>().isKinematic = true;
                        obj.GetComponent<Collider>().enabled = false;
                        obj.transform.localPosition = new Vector3(1, 0, 0);
                        obj.transform.rotation = Quaternion.identity;
                    }
                    else
                    {
                        obj.transform.SetParent(null);
                        obj.transform.position = packet.CurrentPosition;
                        obj.GetComponent<Collider>().enabled = true;
                        obj.GetComponent<Rigidbody>().isKinematic = false;
                        obj.GetComponent<ObjectPickupC>().enabled = true;
                    }

                });
            }, () => new ObjectInteractionPacket());*/
        }

        private void EnqueueMainThreadAction(Action action)
        {
            lock (mainThreadQueue)
            {
                mainThreadQueue.Enqueue(action);
            }
        }

        void Update()
        {
            if (netManager == null || !netManager.IsRunning)
                return;

            netManager.PollEvents();

            lock (mainThreadQueue)
            {
                while (mainThreadQueue.Count > 0)
                {
                    try
                    {
                        mainThreadQueue.Dequeue().Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.LogError($"[MP Error] Main thread action crashed: {ex.Message}");
                    }
                }
            }

            if (!allClientsIn)
                return;

            if (netManager.ConnectedPeersCount > 0)
            {
                float dt = Time.deltaTime;

                tickTimer += dt;
                if (tickTimer >= TICK_RATE)
                {
                    tickTimer = 0f;
                    OnNetworkTick20Hz();
                }

                slowTickTimer += dt;
                if (slowTickTimer >= SLOW_TICK_RATE)
                {
                    slowTickTimer = 0f;
                    OnNetworkTick5Hz();
                }
            }
        }

        private void OnNetworkTick20Hz()
        {
            On20HzTick?.Invoke();

            /*PlayerTransformPacket OutboundPacket = new PlayerTransformPacket
            {
                Position = ModHelper.Instance.player.transform.position,
                Rotation = ModHelper.Instance.player.transform.rotation
            };

            CarTransformPacket OutboundCarPacket = new CarTransformPacket
            {
                Position = ModHelper.Instance.laika.transform.position,
                Rotation = ModHelper.Instance.laika.transform.rotation
            };

            netManager.SendToAll(packetProcessor.WriteNetSerializable<PlayerTransformPacket>(OutboundPacket), DeliveryMethod.Unreliable);
            netManager.SendToAll(packetProcessor.WriteNetSerializable<CarTransformPacket>(OutboundCarPacket), DeliveryMethod.Unreliable);*/
        }

        private void OnNetworkTick5Hz()
        {

        }
    }
}
