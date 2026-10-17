using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using JaLoader;
using UnityEngine.UI;
using Console = JaLoader.Console;

namespace JaloMPy
{
    public class NetworkUIManager : MonoBehaviour
    {
        public static NetworkUIManager instance { get; private set; }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            Initialize();
        }

        internal GameObject NetworkingUI;
        internal MainMenuBookC book;
        internal JaloMPy mod;

        void Initialize()
        {
            var parent = UIManager.Instance.JLCanvas.Find("JLPanel/BookUI");

            var title = Instantiate(parent.Find("Title"), parent);
            title.localPosition = new Vector3(title.localPosition.x, 0, title.localPosition.z);
            title.GetComponent<Text>().text = "MULTIPLAYER";

            var host = Instantiate(parent.Find("ModsButton"), parent);
            host.name = "HostButton";
            host.GetComponent<Button>().onClick.RemoveAllListeners();
            host.GetChild(0).GetComponent<Text>().text = "Host";
            host.localPosition = new Vector3(host.localPosition.x, -170, host.localPosition.z);
            host.GetComponent<Button>().onClick.AddListener(OpenHostUI);

            var join = Instantiate(parent.Find("ModsButton"), parent);
            join.name = "JoinButton";
            join.GetComponent<Button>().onClick.RemoveAllListeners();
            join.GetChild(0).GetComponent<Text>().text = "Join";
            join.localPosition = new Vector3(join.localPosition.x, -210, join.localPosition.z);
            join.GetComponent<Button>().onClick.AddListener(OpenJoinUI);

            book = FindObjectOfType<MainMenuBookC>();
        }

        internal void PrepNetworkingPanel()
        {
            NetworkingUI.Find("Host/HostButton").GetComponent<Button>().onClick.AddListener(HostButton);
            NetworkingUI.Find("Join/JoinButton").GetComponent<Button>().onClick.AddListener(JoinButton);
            NetworkingUI.Find("Host/BackButton").GetComponent<Button>().onClick.AddListener(CloseUI);
            NetworkingUI.Find("Join/BackButton").GetComponent<Button>().onClick.AddListener(CloseUI);
            NetworkingUI.Find("Lobby/DisconnectButton").GetComponent<Button>().onClick.AddListener(CloseUIAndDisconnect);
            NetworkingUI.Find("Lobby/StartButton").GetComponent<Button>().onClick.AddListener(StartButton);
            NetworkingUI.Find("Lobby/SyncButton").GetComponent<Button>().onClick.AddListener(SyncButton);

            CloseUIQuiet();
        }

        public void HostButton()
        {
            UIManager.Instance.PlayClickSound();
            var portText = NetworkingUI.Find("Host/Layout/Port/InputField").GetComponent<InputField>().text;

            if(!IsUsernameValid())
            {
                UIManager.Instance.ShowNotice("Invalid username", "The username must be no more than 20 characters, with no spaces. Open JaloMPy mod settings and change it!", false, true);
                return;
            }
            if (string.IsNullOrEmpty(portText) || !int.TryParse(portText, out int port))
            {
                UIManager.Instance.ShowNotice("Invalid port", "The port field is empty or invalid!", false, true);
                return;
            }

            bool useCurrentSave = NetworkingUI.Find("Host/Layout/UseCurrentSave").GetComponent<Dropdown>().value == 0 ? false : true;

            NetworkConnectionManager.instance.Host(port, useCurrentSave, NetworkingUI.Find("Host/Layout/Password/InputField").GetComponent<InputField>().text);

            NetworkingUI.Find("Lobby/SyncButton").gameObject.SetActive(false);
            NetworkingUI.Find("Lobby/StartButton").GetComponent<Button>().interactable = false;

            OpenServerLobbyUI();
        }

        internal void ShowBackupNotice()
        {
            if (mod.backedUpSave)
                return;

            UIManager.Instance.ShowNotice("BACKUP", "Your current save will be backed up. To revert to your original save later on, simply rename the file, trimming the date and time from the end.", false, true);

            mod.BackUpSave();
        }

        public void SyncButton()
        {
            NetworkingUI.Find("Lobby/SyncButton").GetComponent<Button>().interactable = false;

            NetworkConnectionManager.instance.SendRequestSave();

            if (!NetworkConnectionManager.instance.useCurrentSave)
                UpdateSyncedStatus();
        }

        internal void UpdateSyncedStatus()
        {
            NetworkingUI.Find("Lobby/Layout/Client").GetComponent<Text>().text = NetworkingUI.Find("Lobby/Layout/Client").GetComponent<Text>().text.Replace("(UNSYNCED)", "(SYNCED)");
            NetworkingUI.Find("Lobby/SyncButton").gameObject.SetActive(false);
            
            if (NetworkConnectionManager.instance.IsHost())
                NetworkingUI.Find("Lobby/StartButton").GetComponent<Button>().interactable = true;
        }

        private bool IsUsernameValid()
        {
            var text = mod.GetInputFieldValue("username");

            if (text.Length > 20)
                return false;

            if (text.Contains(" "))
                return false;

            if (string.IsNullOrEmpty(text))
                return false;

            return true;
        }

        internal string PlayerUsername()
        {
            return mod.GetInputFieldValue("username");
        }

        public void JoinButton()
        {
            UIManager.Instance.PlayClickSound();
            var portText = NetworkingUI.Find("Join/Layout/Port/InputField").GetComponent<InputField>().text;
            var ipText = NetworkingUI.Find("Join/Layout/IP/InputField").GetComponent<InputField>().text;

            if (!IsUsernameValid())
            {
                UIManager.Instance.ShowNotice("Invalid username", "The username must be no more than 20 characters, with no spaces! Open JaloMPy mod settings and change it!", false, true);
                return;
            }
            if (string.IsNullOrEmpty(portText) || !int.TryParse(portText, out int port))
            {
                UIManager.Instance.ShowNotice("Invalid port", "The port field is empty or invalid!", false, true);
                return;
            }
            if (string.IsNullOrEmpty(ipText))
            {
                UIManager.Instance.ShowNotice("Invalid IP", "The IP field is empty!", false, true);
                return;
            }

            NetworkConnectionManager.instance.Join(ipText, port, NetworkingUI.Find("Join/Layout/Password/InputField").GetComponent<InputField>().text);

            ShowConnecting();
            NetworkingUI.Find("Lobby/StartButton").gameObject.SetActive(false);
            NetworkingUI.Find("Lobby/SyncButton").gameObject.SetActive(true);
        }

        internal void ShowConnecting()
        {
            CloseUIQuiet();
            NetworkingUI.SetActive(true);
            NetworkingUI.Find("Connecting").gameObject.SetActive(true);
        }

        public void StartButton()
        {
            UIManager.Instance.PlayClickSound();
            NetworkConnectionManager.instance.SendStartPacket();
            mod.StartGameHelper(NetworkConnectionManager.instance.useCurrentSave);
        }

        public void OpenHostUI()
        {
            book.CloseBook();
            NetworkingUI.SetActive(true);
            NetworkingUI.Find("Host").gameObject.SetActive(true);
            UIManager.Instance.ToggleObstructRay();

            ShowBackupNotice();
        }

        public void OpenJoinUI()
        {
            book.CloseBook();
            NetworkingUI.SetActive(true);
            NetworkingUI.Find("Join").gameObject.SetActive(true);
            UIManager.Instance.ToggleObstructRay();

            ShowBackupNotice();
        }

        public void OpenServerLobbyUI()
        {
            NetworkingUI.Find("Host").gameObject.SetActive(false);
            NetworkingUI.Find("Join").gameObject.SetActive(false);
            NetworkingUI.Find("Connecting").gameObject.SetActive(false);
            NetworkingUI.Find("Lobby").gameObject.SetActive(true);

            if (NetworkConnectionManager.instance.IsHost())
            {
                NetworkingUI.Find("Lobby/Layout/Host").GetComponent<Text>().text = PlayerUsername() + " (HOST)";
                NetworkingUI.Find("Lobby/Layout/Client").gameObject.SetActive(false);
            }
            else
            {
                NetworkingUI.Find("Lobby/Layout/Client").gameObject.SetActive(true);
                NetworkingUI.Find("Lobby/Layout/Client").GetComponent<Text>().text = PlayerUsername() + " (UNSYNCED)";
                NetworkingUI.Find("Lobby/Layout/Host").gameObject.SetActive(false);
            }
        }

        internal void UpdateLobbyUsernames()
        {
            if (NetworkConnectionManager.instance.IsHost())
            {
                NetworkingUI.Find("Lobby/Layout/Client").gameObject.SetActive(true);
                NetworkingUI.Find("Lobby/Layout/Client").GetComponent<Text>().text = NetworkConnectionManager.instance.playerIdAndName[0] + " (UNSYNCED)";
            }
            else
            {
                NetworkingUI.Find("Lobby/Layout/Host").gameObject.SetActive(true);
                NetworkingUI.Find("Lobby/Layout/Host").GetComponent<Text>().text = NetworkConnectionManager.instance.playerIdAndName[0] + " (HOST)";
            }
        }

        internal void Disconnected()
        {
            CloseUIQuiet();

            UIManager.Instance.ShowNotice("DISCONNECTED", "The connection was interrupted.", false, false);
        }

        public void CloseUI()
        {
            NetworkingUI.Find("Host").gameObject.SetActive(false);
            NetworkingUI.Find("Join").gameObject.SetActive(false);
            NetworkingUI.Find("Lobby").gameObject.SetActive(false);
            NetworkingUI.Find("Connecting").gameObject.SetActive(false);
            NetworkingUI.SetActive(false);
            UIManager.Instance.ToggleObstructRay();
            UIManager.Instance.PlayClickSound();
        }

        public void CloseUIQuiet()
        {
            NetworkingUI.Find("Host").gameObject.SetActive(false);
            NetworkingUI.Find("Join").gameObject.SetActive(false);
            NetworkingUI.Find("Lobby").gameObject.SetActive(false);
            NetworkingUI.Find("Connecting").gameObject.SetActive(false);
            NetworkingUI.SetActive(false);
        }

        public void CloseUIAndDisconnect()
        {
            CloseUI();

            NetworkConnectionManager.instance.Disconnect();
        }
    }
}
