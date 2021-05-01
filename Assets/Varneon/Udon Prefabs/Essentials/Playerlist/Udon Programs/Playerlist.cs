#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Essentials
{
    public class Playerlist : UdonSharpBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField]
        private string CreatorName;

        [SerializeField]
        private Color CreatorColor;

        [SerializeField]
        private string[] Group1Names, Group2Names;

        [SerializeField]
        private Sprite Group1Icon, Group2Icon;

        [Space]
        [Header("References")]
        [SerializeField]
        private Transform PlayerList;

        [SerializeField]
        private Text TextPlayersOnline, TextInstanceLifetime, TextInstanceMaster, TextTimeInWorld;

        [SerializeField]
        private GameObject PlayerListItem;

        #endregion

        #region Private Variables

        [UdonSynced] private long instanceStartTime = 0; // This will be optimized after the new Udon networking update
        private long utcNow;
        private long localJoinTime = 0;
        private VRCPlayerApi localPlayer;
        private VRCPlayerApi[] players;
        private int playerCount;
        private float updateTimer = 0f;

        #endregion

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            UpdateInstanceMaster();

            UpdateUtcTime();

            localJoinTime = utcNow;

            if (localPlayer.isMaster == true && instanceStartTime == 0)
            {
                instanceStartTime = utcNow;
            }

            PlayerListItem.transform.GetChild(5).GetComponent<Image>().sprite = Group1Icon;

            PlayerListItem.transform.GetChild(6).GetComponent<Image>().sprite = Group2Icon;
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;

            if(updateTimer >= 1f)
            {
                UpdateUtcTime();

                TextInstanceLifetime.text = GetDuration(instanceStartTime);
                TextTimeInWorld.text = GetDuration(localJoinTime);

                updateTimer = 0f;
            }
        }

        private string GetDuration(long ticks)
        {
            return TimeSpan.FromTicks(utcNow - ticks).ToString(@"hh\:mm\:ss");
        }

        private void UpdateUtcTime()
        {
            utcNow = DateTime.UtcNow.ToFileTimeUtc();
        }

        private void UpdateTotalPlayerCount(int count)
        {
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];

            VRCPlayerApi.GetPlayers(players);

            playerCount = (count > playerCount) ? count : playerCount;

            TextPlayersOnline.text = $"{players.Length - ((count < 0f) ? 1 : 0)} / {playerCount}";
        }

        private void AddPlayer(VRCPlayerApi player)
        {
            if(player.playerId > playerCount) { UpdateTotalPlayerCount(player.playerId); }

            GameObject newPlayerlistPanel = VRCInstantiate(PlayerListItem);

            if(player.displayName == CreatorName)
            {
                newPlayerlistPanel.GetComponent<Image>().color = CreatorColor;
            }

            Transform t = newPlayerlistPanel.transform;
            t.SetParent(PlayerList.transform);
            t.localPosition = new Vector3();
            t.localEulerAngles = new Vector3();
            t.localScale = new Vector3(1, 1, 1);

            t.GetChild(1).GetComponent<Text>().text = player.playerId.ToString();
            t.GetChild(2).GetComponent<Text>().text = player.displayName;
            t.GetChild(3).GetComponent<Text>().text = (player.playerId < localPlayer.playerId) ? "Joined before you" : DateTime.UtcNow.ToLocalTime().ToString("dd MMMM yyyy hh:mm:ss");

            if (player.IsUserInVR()) { t.GetChild(4).gameObject.SetActive(true); }

            if (IsNameInGroup(player.displayName, Group1Names)) { t.GetChild(5).gameObject.SetActive(true); }

            if (IsNameInGroup(player.displayName, Group2Names)) { t.GetChild(6).gameObject.SetActive(true); }
        }

        private void RemovePlayer(int id)
        {
            for(int i = 0; i < PlayerList.childCount; i++)
            {
                Transform item = PlayerList.GetChild(i);

                if(Convert.ToInt32(item.GetChild(1).GetComponent<Text>().text) == id)
                {
                    Destroy(item.gameObject);

                    return;
                }
            }
        }

        private void UpdateInstanceMaster()
        {
            TextInstanceMaster.text = Networking.GetOwner(gameObject).displayName;
        }

        private bool IsNameInGroup(string name, string[] group)
        {
            foreach (string s in group)
            {
                if (s == name)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            AddPlayer(player);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RemovePlayer(player.playerId);

            UpdateInstanceMaster();

            UpdateTotalPlayerCount(-1);
        }
    }
}