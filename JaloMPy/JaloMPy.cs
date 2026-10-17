using JaLoader;
using JaLoader.Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Xml;
using UnityEngine;
using Console = JaLoader.Console;

namespace JaloMPy
{
    public class JaloMPy : Mod
    {
        public override string ModID => "leaxx.jalompy";
        public override string ModName => "JaloMPy";
        public override string ModAuthor => "Leaxx";
        public override string ModDescription => "Adds 2-player multiplayer to Jalopy";
        public override string ModVersion => "1.0.0";
        public override string GitHubLink => "https://github.com/Jalopy-Mods/JaloMPy";
        public override string NexusModsLink => "";
        public override JaLoader.Common.WhenToInit WhenToInitMod => JaLoader.Common.WhenToInit.InMenu;
        public override List<(string, string, string)> Dependencies => new List<(string, string, string)>()
        {
        };
        public override List<(string, string, string)> Incompatibilities => new List<(string, string, string)>()
        {
        };

        public override bool UseAssets => true;
        internal bool backedUpSave = false;

        internal bool simulateExtremeLag = true;

        public override void EventsDeclaration()
        {
            base.EventsDeclaration();

            EventsManager.Instance.OnObjectPickedUp += OnObjectPickedUp;
            EventsManager.Instance.OnObjectDropped += OnObjectDropped;
        }

        internal void BackUpSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "Saves");
            if (File.Exists(Path.Combine(path, "Save.sfminsk")))
            {
                string backupExtension = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFileName = $"Save.sfminsk.{backupExtension}.bak";

                File.Copy(Path.Combine(path, "Save.sfminsk"), Path.Combine(path, backupFileName), false);
            }

            backedUpSave = true;
        }

        public override void SettingsDeclaration()
        {
            base.SettingsDeclaration();

            InstantiateSettings();

            AddInputField("username", "Username:", "Player", "Your desired name");
        }

        public override void CustomObjectsRegistration()
        {
            base.CustomObjectsRegistration();
        }

        internal void CreateNetworkManager()
        {
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            go.AddComponent<NetworkConnectionManager>();
            go.AddComponent<NetworkUIManager>();
        }

        public override void Preload()
        {
            base.Preload();

            var asset = LoadAsset<GameObject>("jlmp_ui", "JLMPNetworkingPanel", "", ".prefab");
            var obj = Instantiate(asset);
            obj.transform.SetParent(UIManager.Instance.JLCanvas.transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(false);
            obj.transform.SetSiblingIndex(5);

            DontDestroyOnLoad(obj);

            CreateNetworkManager();

            NetworkUIManager.instance.NetworkingUI = obj;
            NetworkUIManager.instance.mod = this;
            NetworkUIManager.instance.PrepNetworkingPanel();

            NetworkConnectionManager.instance.mod = this;
        }

        internal void StartGameHelper(bool useCurrentSave)
        {
            var go = Instantiate(new GameObject("JLMPStartHelper"));
            go.AddComponent<MainMenuClickersC>();
            var comp = go.GetComponent<MainMenuClickersC>();
            comp.book = GameObject.Find("Map");

            if (useCurrentSave)
                comp.ContinueGame();
            else
                comp.StartNewGameSkipTutorial();

            NetworkUIManager.instance.CloseUIQuiet();
        }

        private void OnObjectPickedUp(ObjectEventArgs args)
        {
            if(!args.gameObject.GetComponent<NetworkID>())
                NetworkSceneManager.instance.AddSyncedObject(args.gameObject);

            ObjectInteractionPacket OutboundPacket = new ObjectInteractionPacket
            {
                NetworkObjectID = args.gameObject.GetComponent<NetworkID>() ? args.gameObject.GetComponent<NetworkID>().id : NetworkSceneManager.instance.currentObjectId - 1,
                PickedUp = true,
                CurrentPosition = args.gameObject.transform.position,
                ResourceName = args.gameObjectName
            };

            NetworkConnectionManager.SendPacketToAll<ObjectInteractionPacket>(OutboundPacket, DeliveryMethod.ReliableOrdered);
        }

        private void OnObjectDropped(ObjectEventArgs args)
        {
            ObjectInteractionPacket OutboundPacket = new ObjectInteractionPacket
            {
                NetworkObjectID = args.gameObject.GetComponent<NetworkID>().id,
                PickedUp = false,
                CurrentPosition = args.gameObject.transform.position,
                ResourceName = args.gameObjectName
            };

            NetworkConnectionManager.SendPacketToAll<ObjectInteractionPacket>(OutboundPacket, DeliveryMethod.ReliableOrdered);
        }

        internal void WaitForAllPlayersLoaded()
        {
            Console.Log("Waiting for other client...");
            Time.timeScale = 0f;
        }

        internal void AllPlayersLoaded()
        {
            Time.timeScale = 1f;
            Console.Log("All clients loaded");
        }

        public void DisconnectCommand()
        {
            NetworkConnectionManager.instance.Disconnect();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DisconnectCommand();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            DisconnectCommand();
        }

        public override void OnReload()
        {
            base.OnReload();
            DisconnectCommand();
        }

        public override void OnSettingsSaved()
        {
            base.OnSettingsSaved();
        }

        public override void OnSettingValueChanged(string ID)
        {
            base.OnSettingValueChanged(ID);
        }

        public override void OnSettingsReset()
        {
            base.OnSettingsReset();
        }

        public override void OnSettingsLoaded()
        {
            base.OnSettingsLoaded();
        }
    }
}
