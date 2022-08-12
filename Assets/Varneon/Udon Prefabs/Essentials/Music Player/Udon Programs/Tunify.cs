#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable UNT0003 // https://vrchat.canny.io/vrchat-udon-closed-alpha-bugs/p/getcomponentst-functions-are-not-defined-internally-for-vrcsdk3-components
#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDKBase;
using Varneon.UdonPrefabs.Abstract;

namespace Varneon.UdonPrefabs.Essentials
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(VRCUnityVideoPlayer))] //AVPro support coming soon
    public class Tunify : UdonSharpBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [SerializeField, Range(10f, 60f)]
        private float LoadingTimeout = 20f;

        [SerializeField]
        private bool Repeat, Shuffle, ShufflePlaylist, PlayOnStart, OnlyFirstPlaylistOnStart, DisableCopyrightedAutoplay;

        [SerializeField, ColorUsage(false, false)]
        private Color HighlightColor = new Color(0f, 1f, 0f);

        [SerializeField]
        private AudioSource[] AudioSources;

        //Placeholder for upcoming HUD
        /*
        [Space]
        [Header("Optional")]
        [SerializeField]
        private HUD HUD;
        */

        [Space]
        [Header("References")]
        [SerializeField]
        private RectTransform Playlists;

        [SerializeField]
        private RectTransform Songs;

        [SerializeField]
        private GameObject PlaylistItem, SongItem, ErrorPrompt, SyncedPlaybackOverlay, OwnershipLockToggle, OwnershipLockedIcon, OwnershipUnlockedIcon;

        [SerializeField]
        private Text ErrorText, TimeElapsed, TimeLength, TextTitle, TextArtist, TextPlaylist, TextLoading, TextPlaylistDescription, SyncedStateInfo;

        [SerializeField]
        private RectTransform LoadingIcon, VolumeIcons, CopyrightedPlaylistNotice;

        [SerializeField]
        private Button ButtonPlay, ButtonPause, ButtonNext, ButtonPrev, ButtonShuffle, ButtonShufflePlaylist, ButtonRepeat, ButtonRepeatOne, ButtonClaimOwnership;

        [SerializeField]
        private Toggle ToggleAllowCopyrightedPlaylists;

        [SerializeField]
        private Slider TimeProgressBar, VolumeSlider;

        [Space]
        [Header("Debug")]
        [SerializeField]
        private UdonLogger Logger;
        #endregion

        #region Private Variables
        private VRCUnityVideoPlayer player;

        private const string LogPrefix = "[<color=#009999>Tunify</color>]:";

        [HideInInspector]
        public VRCUrl[] Urls = new VRCUrl[0];

        [HideInInspector]
        public string[] Titles = new string[0];

        [HideInInspector]
        public string[] Artists = new string[0];

        [HideInInspector]
        public string[] Tags = new string[0];

        [HideInInspector]
        public int[] PlaylistIndices = new int[0];

        [HideInInspector]
        public string[] PlaylistNames = new string[0];

        [HideInInspector]
        public string[] PlaylistArgs = new string[0];

        [HideInInspector]
        public string[] PlaylistDescriptions = new string[0];

        [HideInInspector]
        public int[] AutoplayPlaylistIndices = new int[0];

        [HideInInspector]
        public int[] CopyrightFreePlaylistIndices = new int[0];

        [HideInInspector]
        public int[] AutoplayCopyrightFreePlaylistIndices = new int[0];

        private int selectedPlaylist;

        private int playlistStartIndex, playlistEndIndex;

        private int currentSongIndex = -1, currentSongPlaylistIndex = -1;

        private int nextSongIndex = -1, nextSongPlaylistIndex = -1;

        private bool loading, seeking;

        private float loadingTime, averageLoadingTime = 5f;

        private Slider loadingSlider;

        private float songDuration;

        private Transform nextSongListItem, currentSongListItem;

        private Image[] volumeIcons;

        private float originalVolume;

        private bool repeatOne;

        private bool hasUserConfirmedError;

        private bool recoveringFromError;

        private bool isRateLimited;

        private TunifySync sync;

        private bool isSynced;

        private int syncedStartTimeInServerMS;

        private float syncedTime;

        private bool isLocalPlayerOwner;

        private bool allowOwnershipClaims;

        private const float RATE_LIMIT_SECONDS = 5f;
        #endregion

        #region Internal Properties
        internal bool AllowOwnershipClaims
        {
            get => allowOwnershipClaims;
            set
            {
                Log($"AllowOwnershipClaims = {value}");

                if(allowOwnershipClaims != value)
                {
                    _SetAllowOwnershipClaims(value);

                    sync._SetAllowOwnershipClaims(value);
                }
            }
        }
        #endregion

        #region Unity Methods
        private void Start()
        {
            sync = GetComponent<TunifySync>();

            if(sync != null)
            {
                sync._Link(this);

                isSynced = true;

                OwnershipLockToggle.SetActive(true);

                Log("Successfully linked to TunifySync!");
            }

            player = (VRCUnityVideoPlayer)GetComponent(typeof(VRCUnityVideoPlayer));

            volumeIcons = VolumeIcons.GetComponentsInChildren<Image>(true);

            ToggleAllowCopyrightedPlaylists.isOn = !DisableCopyrightedAutoplay;

            TextTitle.text = string.Empty;

            TextArtist.text = string.Empty;

            InitializePlaylists();

            UpdateSongList();

            if (!Shuffle) { ShufflePlaylist = false; }
            else if (ShufflePlaylist) { Shuffle = false; SetShuffleButtonMode(true); }

            if (PlayOnStart)
            {
                if (!OnlyFirstPlaylistOnStart)
                {
                    LoadAndPlayAnyRandomSong();
                }
                else
                {
                    if (!DisableCopyrightedAutoplay || (CopyrightFreePlaylistIndices.Length > 0 && CopyrightFreePlaylistIndices[0] == 0))
                    {
                        selectedPlaylist = nextSongPlaylistIndex = 0;
                        if (Shuffle || ShufflePlaylist) { LoadAndPlayRandomSongOnList(nextSongPlaylistIndex); }
                        else { LoadAndPlaySong(0); }
                    }
                    else
                    {
                        ShowPromptNoCopyrightFreeSongs();
                    }
                }

                UpdateSongList();
            }

            SetButtonHighlight(ButtonShuffle, Shuffle);

            SetButtonHighlight(ButtonRepeat, Repeat);

            SetButtonHighlight(ButtonRepeatOne, true);

            SetButtonHighlight(ButtonShufflePlaylist, true);

            _UpdateVolume();
        }

        private void Update()
        {
            UpdateTimeInfo();

            LoadSong();
        }
        #endregion

        #region Public Control Methods
        public void _Play()
        {
            player.Play();
        }

        public void _Stop()
        {
            player.Stop();
        }

        public void _Pause()
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongStopped();
            }

            player.Pause();

            UpdatePlayPauseButton(false);

            UpdatePlayingPlaylistIcon(false);
        }

        public void _Next()
        {
            LoadAndPlayNextSong();
        }

        public void _Prev()
        {
            LoadAndPlayPreviousSong();
        }

        public void _SelectPlaylist()
        {
            selectedPlaylist = GetPressedButtonIndex(Playlists);

            ResetLoadingProgressBar();

            HighlightSongListItem(false);

            loadingSlider = null;

            nextSongListItem = null;

            currentSongListItem = null;

            UpdateSongList();
        }

        public void _SelectSong()
        {
            int songListIndex = GetPressedButtonIndex(Songs);

            if (loading || songListIndex < 0 || isRateLimited) { return; }

            int songIndex = PlaylistIndices[selectedPlaylist] + songListIndex;

            if (songIndex == currentSongIndex || songIndex == nextSongIndex) { return; }

            _SelectSong(songIndex);
        }

        public void _SelectSong(int songIndex)
        {
            nextSongIndex = songIndex;

            if (PlayOnStart) { PlayOnStart = false; }

            LoadAndPlaySong(nextSongIndex);
        }

        public void _ToggleShuffle()
        {
            Shuffle ^= true;

            SetButtonHighlight(ButtonShuffle, Shuffle);

            if (Shuffle) { return; }

            ShufflePlaylist = true;

            SetShuffleButtonMode(true);
        }

        public void _ToggleShufflePlaylist()
        {
            ShufflePlaylist = false;

            SetShuffleButtonMode(false);
        }

        public void _ToggleRepeat()
        {
            Repeat ^= true;

            SetButtonHighlight(ButtonRepeat, Repeat);

            if (Repeat) { return; }

            repeatOne = true;

            ButtonRepeatOne.gameObject.SetActive(true);
            ButtonRepeat.gameObject.SetActive(false);
        }

        public void _ToggleRepeatOne()
        {
            repeatOne = false;

            SetButtonHighlight(ButtonRepeat, false);

            ButtonRepeat.gameObject.SetActive(true);
            ButtonRepeatOne.gameObject.SetActive(false);
        }

        public void _UpdateVolume()
        {
            float volume = Mathf.Clamp01(-Mathf.Log10(1f - VolumeSlider.value * 0.9f));

            foreach (AudioSource source in AudioSources)
            {
                source.volume = volume;
            }

            UpdateVolumeIcon();
        }

        public void _BeginVolumeChange()
        {
            SetSliderHighlight(VolumeSlider, true);
        }

        public void _EndVolumeChange()
        {
            SetSliderHighlight(VolumeSlider, false);
        }

        public void _ToggleMute()
        {
            bool muted = VolumeSlider.value == VolumeSlider.minValue;

            if (!muted) { originalVolume = VolumeSlider.value; }

            VolumeSlider.value = muted ? originalVolume : VolumeSlider.minValue;
        }

        public void _BeginSeek()
        {
            SetSliderHighlight(TimeProgressBar, seeking = true);
        }

        public void _EndSeek()
        {
            float seconds = TimeProgressBar.value * songDuration;

            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongStarted(currentSongIndex, seconds);
            }

            player.SetTime(seconds);

            SetSliderHighlight(TimeProgressBar, seeking = false);
        }

        public void _ToggleAllowCopyrightedPlaylists()
        {
            DisableCopyrightedAutoplay = !ToggleAllowCopyrightedPlaylists.isOn;

            Log($"DisableCopyrightedAutoplay: <color=#4887BF>{DisableCopyrightedAutoplay}</color>");
        }

        public void _TryToRecoverFromError()
        {
            recoveringFromError = false;

            if (isRateLimited || hasUserConfirmedError) { return; }

            _Next();
        }

        public void _DisableRateLimiting()
        {
            Log($"<color=#DCDCAA>{nameof(_DisableRateLimiting)}</color>()");

            isRateLimited = false;

            SetNavigationButtonsInteractable(true);
        }

        public void _ConfirmError()
        {
            hasUserConfirmedError = true;

            ErrorPrompt.SetActive(false);
        }

        public void _ApplySyncedStartTime(int startTimeInServerMS, float offset = 0f)
        {
            syncedStartTimeInServerMS = startTimeInServerMS;

            syncedTime = offset;
        }

        public void _SetPlaybackTime(float time)
        {
            player.SetTime(time);
        }

        public void _ToggleAllowOwnershipClaims()
        {
            Log("_ToggleAllowOwnershipClaims()");

            if (isSynced && isLocalPlayerOwner)
            {
                AllowOwnershipClaims ^= true;
            }
        }

        public void _ClaimOwnership()
        {
            Log("_ClaimOwnership()");

            sync._ClaimOwnership();
        }

        internal void _SetAllowOwnershipClaims(bool allowClaims)
        {
            Log($"_SetAllowOwnershipClaims({allowClaims})");

            allowOwnershipClaims = allowClaims;

            GenerateSyncedPlaybackStateInfo();

            ButtonClaimOwnership.interactable = allowClaims;

            OwnershipLockedIcon.SetActive(!allowClaims);

            OwnershipUnlockedIcon.SetActive(allowClaims);

            ButtonClaimOwnership.interactable = allowClaims;
        }

        internal void _SetLocalPlayerOwnerStatus(bool isOwner)
        {
            isLocalPlayerOwner = isOwner;

            SyncedPlaybackOverlay.SetActive(!isOwner);

            GenerateSyncedPlaybackStateInfo();
        }
        #endregion

        #region Private Methods

        #region Return Methods
        /// <summary>
        /// Abstract method for getting the index of the pressed button on a list
        /// </summary>
        /// <param name="root"></param>
        /// <returns>Index of the button pressed</returns>
        private int GetPressedButtonIndex(RectTransform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Button button = root.GetChild(i).GetComponent<Button>();

                if (button.interactable) { continue; }

                button.interactable = true;

                return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets the index of the last song on the playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns>Index of the last song on the playlist</returns>
        private int GetLastPlaylistSongIndex(int playlist)
        {
            return ((PlaylistIndices.Length > playlist + 1) ? PlaylistIndices[playlist + 1] : Urls.Length) - 1;
        }

        /// <summary>
        /// Get the formatted time string based on seconds
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns>Formatted string from seconds in following format: M:SS</returns>
        private string GetFormattedTimeFromSeconds(float seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString($@"{(seconds >= 3600f ? @"h\:m" : string.Empty)}m\:ss");
        }

        /// <summary>
        /// Get the playlist index of any song based on its library index
        /// </summary>
        /// <param name="songIndex"></param>
        /// <returns>Index of the playlist</returns>
        private int GetPlaylistIndexOfSong(int songIndex)
        {
            if (songIndex < 0) { return -1; }

            if (songIndex >= PlaylistIndices[PlaylistIndices.Length - 1]) { return PlaylistIndices.Length - 1; }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if (PlaylistIndices[i] <= songIndex) { continue; }

                return i - 1;
            }

            return 0;
        }
        #endregion

        private void GenerateSyncedPlaybackStateInfo()
        {
            SyncedStateInfo.text = string.Format("Owner: {0}\n\nAllow ownership claim: {1}", Networking.GetOwner(gameObject).displayName, AllowOwnershipClaims);
        }

        /// <summary>
        /// Initialize the list of playlists when the program starts
        /// </summary>
        private void InitializePlaylists()
        {
            if (PlaylistIndices.Length == 0)
            {
                PlaylistIndices = new int[] { 0 };

                PlaylistNames = new string[] { "All Songs" };
            }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if (i > 0) { AddNewListItem(Playlists, PlaylistItem); }

                Playlists.GetChild(i).GetChild(0).GetComponent<Text>().text = PlaylistNames[i];
            }
        }

        /// <summary>
        /// Updates the song list based on selected playlist
        /// </summary>
        private void UpdateSongList()
        {
            TextPlaylist.text = PlaylistNames[selectedPlaylist];

            string description = PlaylistDescriptions[selectedPlaylist];

            TextPlaylistDescription.text = string.IsNullOrEmpty(description) ? "No description" : description;

            bool hasCopyrightedSongs = true;

            for (int i = 0; i < CopyrightFreePlaylistIndices.Length; i++)
            {
                if (CopyrightFreePlaylistIndices[i] == selectedPlaylist) { hasCopyrightedSongs = false; break; }
            }

            CopyrightedPlaylistNotice.gameObject.SetActive(hasCopyrightedSongs);

            playlistStartIndex = (PlaylistIndices.Length == 0) ? 0 : PlaylistIndices[selectedPlaylist];

            playlistEndIndex = GetLastPlaylistSongIndex(selectedPlaylist) + 1;

            int songCount = playlistEndIndex - playlistStartIndex;

            int itemCount = Songs.childCount;

            for (int i = 0; i < Mathf.Max(songCount, itemCount); i++)
            {
                int songIndex = playlistStartIndex + i;

                if (i >= itemCount)
                {
                    AddNewListItem(Songs, SongItem);
                }
                else if (i >= songCount)
                {
                    Destroy(Songs.GetChild(i).gameObject);
                    continue;
                }

                Transform panel = Songs.GetChild(i);

                string[] content = new string[] { Titles[songIndex], Artists[songIndex], Tags[songIndex] };

                for (int j = 0; j < 3; j++)
                {
                    panel.GetChild(j).GetComponent<Text>().text = content[j];
                }
            }

            GetActiveSongListItems();
        }

        /// <summary>
        /// Add abstract list item to any UI list with automatic layout
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        private void AddNewListItem(RectTransform list, GameObject item)
        {
            Transform newPlaylistItem = VRCInstantiate(item).transform;

            newPlaylistItem.SetParent(list);

            newPlaylistItem.localPosition = new Vector3();

            newPlaylistItem.localRotation = new Quaternion();

            newPlaylistItem.localScale = new Vector3(1f, 1f, 1f);
        }

        /// <summary>
        /// Update the time progress bar and texts based on info from the player
        /// </summary>
        private void UpdateTimeInfo()
        {
            if (!player.IsPlaying && !seeking) { return; }

            float timeElapsed = seeking ? TimeProgressBar.value * songDuration : player.GetTime();

            TimeElapsed.text = GetFormattedTimeFromSeconds(timeElapsed);

            TimeProgressBar.value = Mathf.Clamp01(timeElapsed / songDuration);
        }

        /// <summary>
        /// Update the progress bar on the song list item while a song is loading
        /// </summary>
        private void UpdateLoadingProgressBar()
        {
            if (loadingSlider == null) { return; }

            loadingSlider.value = Mathf.Clamp01(loadingTime / averageLoadingTime);
        }

        /// <summary>
        /// Finish the loading process after successful load
        /// </summary>
        private void FinishLoading()
        {
            loading = false;

            loadingTime = 0f;

            LoadingIcon.gameObject.SetActive(false);

            TextLoading.gameObject.SetActive(false);

            ResetLoadingProgressBar();
        }

        /// <summary>
        /// Resets loading progress bar status
        /// </summary>
        private void ResetLoadingProgressBar()
        {
            if (loadingSlider == null) { return; }

            loadingSlider.value = 0f;

            loadingSlider.gameObject.SetActive(false);
        }

        /// <summary>
        /// Resets all info about the song playing
        /// </summary>
        private void ResetSongInfo()
        {
            TextTitle.text = Titles[nextSongIndex];

            TextArtist.text = Artists[nextSongIndex];

            TimeLength.text = GetFormattedTimeFromSeconds(0f);

            TimeElapsed.text = GetFormattedTimeFromSeconds(0f);

            TimeProgressBar.value = 0f;
        }

        /// <summary>
        /// Load a new song and automatically play it after done loading
        /// </summary>
        /// <param name="index"></param>
        private void LoadAndPlaySong(int index)
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongSelected(index);

                //SetPlayPauseButtonsInteractable(false);
            }

            if (loading) { return; }

            nextSongIndex = index;

            nextSongPlaylistIndex = GetPlaylistIndexOfSong(nextSongIndex);

            GetActiveSongListItems();

            loading = true;

            loadingTime = 0f;

            LoadingIcon.gameObject.SetActive(true);

            TextLoading.gameObject.SetActive(true);

            isRateLimited = true;

            SetNavigationButtonsInteractable(false);

            SendCustomEventDelayedSeconds(nameof(_DisableRateLimiting), isSynced ? RATE_LIMIT_SECONDS * 2f : RATE_LIMIT_SECONDS);

            Log($"<color=#DCDCAA>{nameof(LoadAndPlaySong)}</color>(<color=#4887BF>int</color> <color=#9CDCFE>index</color>: <color=#B5CEA8>{index}</color>) | <color=Grey><color=Silver>{Titles[nextSongIndex]}</color> - <color=Silver>{Artists[nextSongIndex]}</color> (<color=Silver>{Urls[nextSongIndex]}</color>)</color>");

            player.PlayURL(Urls[index]);
        }

        /// <summary>
        /// Load next song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayNextSong()
        {
            if (ShufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (Shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            if (currentSongIndex >= GetLastPlaylistSongIndex(currentSongPlaylistIndex))
            {
                if (Repeat)
                {
                    LoadAndPlaySong(PlaylistIndices[currentSongPlaylistIndex]);

                    return;
                }

                Log("<color=Grey>Current song is last on the list, can't play next song</color>");

                return;
            }

            Log("<color=Grey>Loading next song...</color>");

            LoadAndPlaySong(currentSongIndex + 1);
        }

        /// <summary>
        /// Load previous song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayPreviousSong()
        {
            if (ShufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (Shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            if (currentSongIndex <= PlaylistIndices[currentSongPlaylistIndex]) { Log("<color=Grey>Current song is first on the list, can't play previous song</color>"); return; }

            Log("<color=Grey>Loading previous song...</color>");

            LoadAndPlaySong(currentSongIndex - 1);
        }

        /// <summary>
        /// Load any random song from any one of the playlists and play it automatically
        /// </summary>
        private void LoadAndPlayAnyRandomSong()
        {
            if (PlayOnStart)
            {
                int[] playlistIndices = DisableCopyrightedAutoplay ? AutoplayCopyrightFreePlaylistIndices : AutoplayPlaylistIndices;

                if (playlistIndices.Length == 0) { PlayOnStart = false; LoadAndPlayAnyRandomSong(); return; }

                nextSongPlaylistIndex = playlistIndices[UnityEngine.Random.Range(0, playlistIndices.Length)];
            }
            else if (DisableCopyrightedAutoplay)
            {
                if (CopyrightFreePlaylistIndices.Length == 0)
                {
                    ShowPromptNoCopyrightFreeSongs();

                    return;
                }

                nextSongPlaylistIndex = CopyrightFreePlaylistIndices[UnityEngine.Random.Range(0, CopyrightFreePlaylistIndices.Length)];
            }
            else
            {
                nextSongPlaylistIndex = UnityEngine.Random.Range(0, PlaylistIndices.Length - 1);
            }

            LoadAndPlayRandomSongOnList(nextSongPlaylistIndex);
        }

        /// <summary>
        /// Load any random song on the list and play it automatically
        /// </summary>
        private void LoadAndPlayRandomSongOnList(int playlistIndex)
        {
            if (playlistIndex < 0) { LogError("Can't load song: Invalid playlist index!"); return; }

            Log($"<color=#DCDCAA>{nameof(LoadAndPlayRandomSongOnList)}</color>(<color=#4887BF>int</color> <color=#9CDCFE>playlistIndex</color>: <color=#B5CEA8>{playlistIndex}</color>)");

            LoadAndPlaySong(UnityEngine.Random.Range(PlaylistIndices[playlistIndex], GetLastPlaylistSongIndex(playlistIndex) + 1));
        }

        /// <summary>
        /// Automatically play the next or random song based on current options
        /// </summary>
        private void AutoPlayNextOrRandom()
        {
            if (ShufflePlaylist) { LoadAndPlayAnyRandomSong(); return; }
            else if (Shuffle) { LoadAndPlayRandomSongOnList(currentSongPlaylistIndex); return; }

            LoadAndPlayNextSong();
        }

        /// <summary>
        /// Get the active selected and loading song list item after changing playlists
        /// </summary>
        private void GetActiveSongListItems()
        {
            if (selectedPlaylist == currentSongPlaylistIndex)
            {
                int songListIndex = currentSongIndex - PlaylistIndices[currentSongPlaylistIndex];

                currentSongListItem = Songs.GetChild(songListIndex);

                if (player.IsPlaying && currentSongIndex == nextSongIndex)
                {
                    nextSongListItem = currentSongListItem;

                    HighlightSongListItem(true);
                }
            }

            if (selectedPlaylist == nextSongPlaylistIndex)
            {
                int songListIndex = nextSongIndex - PlaylistIndices[nextSongPlaylistIndex];

                nextSongListItem = Songs.GetChild(songListIndex);

                loadingSlider = nextSongListItem.GetComponentInChildren<Slider>(true);

                loadingSlider.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Shows the speaker icon next to the playlist to indicate from which playlist the song is playing from
        /// </summary>
        /// <param name="enabled"></param>
        private void UpdatePlayingPlaylistIcon(bool enabled)
        {
            if (currentSongPlaylistIndex >= 0 && currentSongPlaylistIndex != nextSongPlaylistIndex) { Playlists.GetChild(currentSongPlaylistIndex).GetChild(1).gameObject.SetActive(false); }
            Playlists.GetChild(nextSongPlaylistIndex).GetChild(1).gameObject.SetActive(enabled);
        }

        /// <summary>
        /// Enable or disable the highlight on the active song list item
        /// </summary>
        /// <param name="highlighted"></param>
        private void HighlightSongListItem(bool highlighted)
        {
            if (highlighted && selectedPlaylist != nextSongPlaylistIndex) { return; }

            Transform listItem = highlighted ? nextSongListItem : currentSongListItem;

            if (!Utilities.IsValid(listItem)) { return; }

            Color color = highlighted ? HighlightColor : new Color(1f, 1f, 1f);

            foreach (Text text in listItem.GetComponentsInChildren<Text>(true))
            {
                text.color = color;
            }
        }

        /// <summary>
        /// Show prompt and error indicating that no copyright free songs could be found in the library
        /// </summary>
        private void ShowPromptNoCopyrightFreeSongs()
        {
            LogWarning("Couldn't find any copyright free playlists in the library, try allowing playback of copyrighted content");

            ShowError("Couldn't find copyright free playlists in the library");
        }

        /// <summary>
        /// Shows the error message on the screen with the provided message
        /// </summary>
        /// <param name="error"></param>
        private void ShowError(string error)
        {
            hasUserConfirmedError = false;

            ErrorPrompt.SetActive(true);

            ErrorText.text = error;
        }

        /// <summary>
        /// Song loading process
        /// </summary>
        private void LoadSong()
        {
            if (!loading) { return; }

            loadingTime += Time.deltaTime;

            LoadingIcon.localEulerAngles = new Vector3(0f, 0f, -loadingTime * 360f);

            TextLoading.color = Color.white * (0.75f + Mathf.Sin(loadingTime * 5f) * 0.25f);

            UpdateLoadingProgressBar();

            if (loadingTime < LoadingTimeout) { return; }

            ShowError($"LOADING_TIMEOUT");

            FinishLoading();
        }

        /// <summary>
        /// Change the highlighted status on button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="highlight"></param>
        private void SetButtonHighlight(Button button, bool highlight)
        {
            button.image.color = highlight ? HighlightColor : new Color(1f, 1f, 1f);
        }

        /// <summary>
        /// Switches the Play and Pause buttons based on whether or not the player is currently playing
        /// </summary>
        /// <param name="isPlaying"></param>
        private void UpdatePlayPauseButton(bool isPlaying)
        {
            ButtonPlay.gameObject.SetActive(!isPlaying);

            ButtonPause.gameObject.SetActive(isPlaying);
        }

        /// <summary>
        /// Sets shuffle button to either standard shuffle or playlist shuffle
        /// </summary>
        /// <param name="mode"></param>
        private void SetShuffleButtonMode(bool playlistShuffle)
        {
            ButtonShufflePlaylist.gameObject.SetActive(playlistShuffle);
            ButtonShuffle.gameObject.SetActive(!playlistShuffle);
        }

        /// <summary>
        /// Set loaded song as currently playing
        /// </summary>
        private void SetNextSongAsCurrent()
        {
            if (currentSongIndex == nextSongIndex) { return; }

            //Placeholder for upcoming hud
            //if (HUD) { HUD._ShowMusicNotification(TextTitle.text, TextArtist.text); }

            currentSongPlaylistIndex = nextSongPlaylistIndex;

            currentSongIndex = nextSongIndex;

            HighlightSongListItem(false);

            currentSongListItem = nextSongListItem;

            HighlightSongListItem(true);
        }

        /// <summary>
        /// Set seeking mode active
        /// </summary>
        /// <param name="active"></param>
        private void SetSeekingActive(bool active)
        {
            SetSliderHighlight(TimeProgressBar, active);

            seeking = active;
        }

        /// <summary>
        /// Highlights the slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="highlight"></param>
        private void SetSliderHighlight(Slider slider, bool highlight)
        {
            slider.fillRect.GetComponent<Image>().color = highlight ? HighlightColor : Color.white;
            slider.handleRect.gameObject.SetActive(highlight);
        }

        /// <summary>
        /// Sets the interactable state on song navigation buttons
        /// </summary>
        /// <param name="interactable"></param>
        private void SetNavigationButtonsInteractable(bool interactable)
        {
            ButtonNext.interactable = interactable;
            ButtonPrev.interactable = interactable;
        }

        /// <summary>
        /// Sets the interactable state on play and pause buttons
        /// </summary>
        /// <param name="interactable"></param>
        private void SetPlayPauseButtonsInteractable(bool interactable)
        {
            ButtonPlay.interactable = interactable;
            ButtonPause.interactable = interactable;
        }

        /// <summary>
        /// Updates the volume icon next to the volume slider based on the slider's value
        /// </summary>
        private void UpdateVolumeIcon()
        {
            for (int i = 0; i < volumeIcons.Length; i++)
            {
                volumeIcons[i].enabled = Mathf.CeilToInt((VolumeSlider.value - VolumeSlider.minValue) * (volumeIcons.Length - 1f)) == i;
            }
        }

        /// <summary>
        /// Proxy for printing messages in logs
        /// </summary>
        /// <param name="text"></param>
        private void Log(string text)
        {
            if (Logger) { Logger._Log($"{LogPrefix} {text}"); }

            Debug.Log($"{LogPrefix} {text}");
        }

        /// <summary>
        /// Proxy for printing warnings in logs
        /// </summary>
        /// <param name="text"></param>
        private void LogWarning(string text)
        {
            if (Logger) { Logger._LogWarning($"{LogPrefix} {text}"); }

            Debug.LogWarning($"{LogPrefix} {text}");
        }

        /// <summary>
        /// Proxy for printing errors in logs
        /// </summary>
        /// <param name="text"></param>
        private void LogError(string text)
        {
            if (Logger) { Logger._LogError($"{LogPrefix} {text}"); }

            Debug.LogError($"{LogPrefix} {text}");
        }
        #endregion

        #region VRC Video Methods
        public override void OnVideoEnd()
        {
            if (isSynced && isLocalPlayerOwner)
            {
                sync._OnSongStopped();
            }

            Log($"<color=#DCDCAA>{nameof(OnVideoEnd)}</color>()");

            TimeProgressBar.fillRect.gameObject.SetActive(false);

            UpdatePlayPauseButton(false);

            ResetSongInfo();

            if (loading) { return; }

            if (repeatOne && (isSynced == isLocalPlayerOwner)) { player.SetTime(0f); player.Play(); return; }

            HighlightSongListItem(false);

            if (isSynced && !isLocalPlayerOwner) { return; }

            AutoPlayNextOrRandom();
        }

        public override void OnVideoStart()
        {
            Log($"<color=#DCDCAA>{nameof(OnVideoStart)}</color>()");

            TimeProgressBar.fillRect.gameObject.SetActive(true);

            UpdatePlayPauseButton(true);

            TextTitle.text = Titles[nextSongIndex];

            TextArtist.text = Artists[nextSongIndex];

            songDuration = player.GetDuration();

            TimeLength.text = GetFormattedTimeFromSeconds(songDuration);

            UpdatePlayingPlaylistIcon(true);

            SetNextSongAsCurrent();

            if (isSynced)
            {
                sync._OnSongStarted(currentSongIndex, player.GetTime());
            }
        }

        public override void OnVideoReady()
        {
            Log($"<color=#DCDCAA>{nameof(OnVideoReady)}</color>()");

            averageLoadingTime = (averageLoadingTime + loadingTime) / 2f;

            SetPlayPauseButtonsInteractable(true);

            FinishLoading();
        }

        public override void OnVideoError(VideoError videoError)
        {
            string error = videoError.ToString();

            ShowError(error);

            LogError($"<color=#CC0000>{nameof(OnVideoError)}</color>: {error}");

            FinishLoading();

            if (recoveringFromError) { return; }

            bool isPlaying = player.IsPlaying;

            Log($"<color=#DCDCAA>{nameof(OnVideoError)}</color> | <color=#4887BF>bool</color> <color=#9CDCFE>isPlaying</color>: <color=#4887BF>{isPlaying}</color>");

            if (isPlaying) { return; }

            if (error != "RateLimited") { SendCustomEventDelayedSeconds(nameof(_TryToRecoverFromError), RATE_LIMIT_SECONDS); recoveringFromError = true; return; }

            UpdatePlayingPlaylistIcon(false);

            SetNextSongAsCurrent();

            if (!isPlaying) { HighlightSongListItem(false); }

            ResetSongInfo();
        }
        #endregion
    }
}
