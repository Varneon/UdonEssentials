#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable 649

using System;
using System.Globalization;
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
        private Groups groups;

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
        private const NumberStyles HEX_NUMSTYLE = NumberStyles.HexNumber;

        #endregion

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            UpdateInstanceMaster();

            UpdateUtcTime();

            localJoinTime = utcNow;

            if (localPlayer.isMaster)
            {
                instanceStartTime = utcNow;

                RequestSerialization();
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;

            if (updateTimer >= 1f)
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

            TextPlayersOnline.text = $"{players.Length - ((count < 0) ? 1 : 0)} / {playerCount}";
        }

        private void AddPlayer(VRCPlayerApi player)
        {
            if (player.playerId > playerCount) { UpdateTotalPlayerCount(player.playerId); }

            GameObject newPlayerlistPanel = VRCInstantiate(PlayerListItem);

            newPlayerlistPanel.SetActive(true);

            Transform t = newPlayerlistPanel.transform;
            t.SetParent(PlayerList.transform);
            t.localPosition = Vector3.zero;
            t.localEulerAngles = Vector3.zero;
            t.localScale = Vector3.one;

            Text[] texts = t.GetComponentsInChildren<Text>(true);

            texts[INDEX_TEXT_ID].text = player.playerId.ToString();
            texts[INDEX_TEXT_NAME].text = player.displayName;
            texts[INDEX_TEXT_TIME].text = (player.playerId < localPlayer.playerId) ? "Joined before you" : DateTime.UtcNow.ToLocalTime().ToString("dd MMMM yyyy hh:mm:ss");

            if (player.IsUserInVR()) { t.GetChild(INDEX_VR).gameObject.SetActive(true); }

            if (groups) { ApplyGroupsInfo(t, player.displayName); }
        }

        private void ApplyGroupsInfo(Transform playlistPanel, string displayName)
        {
            int[] playerGroupIndices = groups._GetGroupIndicesOfPlayer(displayName);

            int shownGroupCount = 0;

            bool customColorApplied = false;

            for (int i = 0; i < playerGroupIndices.Length; i++)
            {
                int groupIndex = playerGroupIndices[i];

                string groupArguments = groups._GetGroupArguments(groupIndex);

                if (!customColorApplied && groupArguments.Contains("-playerlistFrameColor"))
                {
                    string hex = GetArgumentValue(groupArguments, "-playerlistFrameColor").Trim('#');

                    playlistPanel.GetComponent<Image>().color = new Color(
                        byte.Parse(hex.Substring(0, 2), HEX_NUMSTYLE) / 255f,
                        byte.Parse(hex.Substring(2, 2), HEX_NUMSTYLE) / 255f,
                        byte.Parse(hex.Substring(4, 2), HEX_NUMSTYLE) / 255f
                        );

                    customColorApplied = true;

                    if (shownGroupCount == 2) { break; }
                }

                if (shownGroupCount < 2)
                {
                    GameObject imageGO = playlistPanel.GetChild(shownGroupCount == 0 ? INDEX_GROUP1 : INDEX_GROUP2).gameObject;

                    if (!groupArguments.Contains("-noPlayerlistIcon"))
                    {
                        Sprite icon = groups._GetGroupIcon(groupIndex);

                        if (icon != null)
                        {
                            imageGO.SetActive(true);

                            imageGO.GetComponent<Image>().sprite = icon;

                            shownGroupCount++;
                        }
                    }
                }
            }
        }

        private string GetArgumentValue(string args, string arg)
        {
            int argPos = args.IndexOf(arg);

            if (argPos >= 0)
            {
                argPos += arg.Length;

                int argBreak = args.IndexOf(' ', argPos);

                return args.Substring(argPos + 1, argBreak < 0 ? args.Length - argPos - 1 : argBreak - argPos);
            }

            return string.Empty;
        }

        private void RemovePlayer(int id)
        {
            for (int i = 0; i < PlayerList.childCount; i++)
            {
                Transform item = PlayerList.GetChild(i);

                if (Convert.ToInt32(item.GetChild(INDEX_ID).GetComponent<Text>().text) == id)
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