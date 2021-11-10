#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Essentials
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Playerlist : UdonSharpBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField]
        private string CreatorName;

        [SerializeField]
        private Color CreatorColor;

        [SerializeField]
        private TextAsset Group1Namelist, Group2Namelist;

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

        [UdonSynced] private long instanceStartTime = 0;
        private long utcNow;
        private long localJoinTime = 0;
        private string[] group1Names, group2Names;
        private VRCPlayerApi localPlayer;
        private VRCPlayerApi[] players;
        private int playerCount;
        private float updateTimer = 0f;
        private const int
            INDEX_ID = 1,
            INDEX_VR = 4,
            INDEX_GROUP1 = 5,
            INDEX_GROUP2 = 6,
            INDEX_TEXT_ID = 0,
            INDEX_TEXT_NAME = 1,
            INDEX_TEXT_TIME = 2;

        #endregion

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            group1Names = Group1Namelist ? Group1Namelist.text.Split(new char[] { '\n', '\r' }) : new string[0];
            group2Names = Group2Namelist ? Group2Namelist.text.Split(new char[] { '\n', '\r' }) : new string[0];

            UpdateInstanceMaster();

            UpdateUtcTime();

            localJoinTime = utcNow;

            if (localPlayer.isMaster)
            {
                instanceStartTime = utcNow;

                RequestSerialization();
            }

            PlayerListItem.transform.GetChild(INDEX_GROUP1).GetComponent<Image>().sprite = Group1Icon;

            PlayerListItem.transform.GetChild(INDEX_GROUP2).GetComponent<Image>().sprite = Group2Icon;
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

            Text[] texts = t.GetComponentsInChildren<Text>(true);

            texts[INDEX_TEXT_ID].text = player.playerId.ToString();
            texts[INDEX_TEXT_NAME].text = player.displayName;
            texts[INDEX_TEXT_TIME].text = (player.playerId < localPlayer.playerId) ? "Joined before you" : DateTime.UtcNow.ToLocalTime().ToString("dd MMMM yyyy hh:mm:ss");

            if (player.IsUserInVR()) { t.GetChild(INDEX_VR).gameObject.SetActive(true); }

            if (IsNameInGroup(player.displayName, group1Names)) { t.GetChild(INDEX_GROUP1).gameObject.SetActive(true); }

            if (IsNameInGroup(player.displayName, group2Names)) { t.GetChild(INDEX_GROUP2).gameObject.SetActive(true); }
        }

        private void RemovePlayer(int id)
        {
            for(int i = 0; i < PlayerList.childCount; i++)
            {
                Transform item = PlayerList.GetChild(i);

                if(Convert.ToInt32(item.GetChild(INDEX_ID).GetComponent<Text>().text) == id)
                {
                    Destroy(item.gameObject);

                    return;
                }
            }
        }

        private void UpdateInstanceMaster()
        {
            VRCPlayerApi master = Networking.GetOwner(gameObject);

            if (Utilities.IsValid(master)) { TextInstanceMaster.text = master.displayName; }
        }

        private bool IsNameInGroup(string name, string[] group)
        {
            foreach (string s in group)
            {
                if (string.Equals(s, name))
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
            if (Utilities.IsValid(player)) { RemovePlayer(player.playerId); }

            UpdateInstanceMaster();

            UpdateTotalPlayerCount(-1);
        }
    }
}