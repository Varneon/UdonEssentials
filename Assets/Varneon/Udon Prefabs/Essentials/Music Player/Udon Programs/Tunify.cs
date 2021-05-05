#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable UNT0003 // https://vrchat.canny.io/vrchat-udon-closed-alpha-bugs/p/getcomponentst-functions-are-not-defined-internally-for-vrcsdk3-components
#pragma warning disable 649

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using Varneon.UdonPrefabs.RuntimeTools;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components;
using VRC.SDKBase;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using VRC.Udon;
#endif

namespace Varneon.UdonPrefabs.Essentials
{
    [RequireComponent(typeof(VRCUnityVideoPlayer))]
    public class Tunify : UdonSharpBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [SerializeField, Range(10f, 60f)]
        private float LoadingTimeout = 20f;

        [SerializeField]
        private bool Loop, Shuffle, PlayOnStart;

        [SerializeField, ColorUsage(false, false)]
        private Color HighlightColor = new Color(0f, 1f, 0f);
        
        [SerializeField]
        private AudioSource[] AudioSources;

        [Space]
        [Header("References")]
        [SerializeField]
        private RectTransform Playlists;
        
        [SerializeField]
        private RectTransform Songs;

        [SerializeField]
        private GameObject PlaylistItem, SongItem, ErrorPrompt;

        [SerializeField]
        private Text TimeElapsed, TimeLength, TextTitle, TextArtist, TextPlaylist;

        [SerializeField]
        private Button ButtonPlay, ButtonPause, ButtonShuffle, ButtonLoop;

        [SerializeField]
        private Slider TimeProgressBar, VolumeSlider;

        [Space]
        [Header("Debug")]
        [SerializeField]
        private UdonDebugger Debugger;
        #endregion

        #region Private Variables
        private VRCUnityVideoPlayer player;

        private readonly string LogPrefix = "[<color=#009999>Tunify</color>]:";

        [HideInInspector]
        public VRCUrl[] Urls = new VRCUrl[0];

        [HideInInspector]
        public string[] Titles = new string[0];

        [HideInInspector]
        public string[] Artists = new string[0];

        [HideInInspector]
        public int[] PlaylistIndices = new int[0];

        [HideInInspector]
        public string[] PlaylistNames = new string[0];

        private int selectedPlaylist;

        private int playlistStartIndex, playlistEndIndex;

        private int currentSongIndex = -1, currentSongPlaylistIndex = -1;
        
        private int nextSongIndex = -1, nextSongPlaylistIndex = -1;

        private bool loading;

        private float loadingTime, averageLoadingTime = 5f;

        private Slider loadingSlider;

        private float songDuration;

        private Transform nextSongListItem, currentSongListItem;
        #endregion

        #region Unity Methods
        private void Start()
        {
            player = (VRCUnityVideoPlayer)GetComponent(typeof(VRCUnityVideoPlayer));

            InitializePlaylists();

            UpdateSongList();

            if (PlayOnStart)
            {
                LoadAndPlaySong(UnityEngine.Random.Range(0, Urls.Length - 1));
            }

            SetButtonHighlight(ButtonShuffle, Shuffle);

            SetButtonHighlight(ButtonLoop, Loop);

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
            player.Pause();

            UpdatePlayPauseButton(false);
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

            if(loading || songListIndex < 0) { return; }

            int songIndex = PlaylistIndices[selectedPlaylist] + songListIndex;

            if (songIndex == currentSongIndex || songIndex == nextSongIndex) { return; }

            nextSongIndex = songIndex;
            
            LoadAndPlaySong(nextSongIndex);
        }

        public void _ToggleShuffle()
        {
            Shuffle ^= true;

            SetButtonHighlight(ButtonShuffle, Shuffle);
        }

        public void _ToggleLoop()
        {
            Loop ^= true;

            SetButtonHighlight(ButtonLoop, Loop);
        }

        public void _UpdateVolume()
        {
            float volume = Mathf.Clamp01(-Mathf.Log10(1f - VolumeSlider.value * 0.9f));

            foreach (AudioSource source in AudioSources)
            {
                source.volume = volume;
            }
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
            return TimeSpan.FromSeconds(seconds).ToString(@"m\:ss");
        }

        /// <summary>
        /// Get the playlist index of any song based on its library index
        /// </summary>
        /// <param name="songIndex"></param>
        /// <returns>Index of the playlist</returns>
        private int GetPlaylistIndexOfSong(int songIndex)
        {
            if (songIndex >= PlaylistIndices[PlaylistIndices.Length - 1]) { return PlaylistIndices.Length - 1; }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if(PlaylistIndices[i] <= songIndex) { continue; }

                return i - 1;
            }

            return 0;
        }
        #endregion

        /// <summary>
        /// Initialize the list of playlists when the program starts
        /// </summary>
        private void InitializePlaylists()
        {
            if(PlaylistIndices.Length == 0)
            {
                PlaylistIndices = new int[] { 0 };

                PlaylistNames = new string[] { "All Songs" };
            }

            for (int i = 0; i < PlaylistIndices.Length; i++)
            {
                if(i > 0) { AddNewListItem(Playlists, PlaylistItem); }

                Playlists.GetChild(i).GetChild(0).GetComponent<Text>().text = PlaylistNames[i];
            }
        }

        /// <summary>
        /// Updates the song list based on selected playlist
        /// </summary>
        private void UpdateSongList()
        {
            TextPlaylist.text = PlaylistNames[selectedPlaylist];

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
                else if(i >= songCount)
                {
                    Destroy(Songs.GetChild(i).gameObject);
                    continue;
                }

                Transform panel = Songs.GetChild(i);

                for(int j = 0; j < 3; j++)
                {
                    panel.GetChild(j).GetComponent<Text>().text = new string[] { Titles[songIndex], Artists[songIndex], Urls[songIndex].Get() }[j];
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
            if (!player.IsPlaying){ return; }

            float timeElapsed = player.GetTime();

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
            if (loading) { return; }

            nextSongIndex = index;

            nextSongPlaylistIndex = GetPlaylistIndexOfSong(nextSongIndex);

            GetActiveSongListItems();

            loading = true;

            loadingTime = 0f;

            Log($"Loading song: [{nextSongIndex}] {Titles[nextSongIndex]} - {Artists[nextSongIndex]} ({Urls[nextSongIndex]})");

            player.PlayURL(Urls[index]);
        }

        /// <summary>
        /// Load next song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayNextSong()
        {
            if (Shuffle) { LoadAndPlayRandomSongOnList(); return; }

            if (currentSongIndex >= GetLastPlaylistSongIndex(currentSongPlaylistIndex)) 
            {
                if (Loop)
                {
                    LoadAndPlaySong(PlaylistIndices[currentSongPlaylistIndex]);

                    return;
                }

                Log("Current song is last on the list, can't play next song");

                return;
            }

            Log("Loading next song...");

            LoadAndPlaySong(currentSongIndex + 1);
        }

        /// <summary>
        /// Load previous song on the playlist and play it automatically
        /// </summary>
        private void LoadAndPlayPreviousSong()
        {
            if (Shuffle) { LoadAndPlayRandomSongOnList(); return; }

            if (currentSongIndex <= PlaylistIndices[currentSongPlaylistIndex]) { Log("Current song is first on the list, can't play previous song"); return; }

            Log("Loading previous song...");

            LoadAndPlaySong(currentSongIndex - 1);
        }

        /// <summary>
        /// Load any random song on the list and play it automatically
        /// </summary>
        private void LoadAndPlayRandomSongOnList()
        {
            LoadAndPlaySong(UnityEngine.Random.Range(PlaylistIndices[currentSongPlaylistIndex], GetLastPlaylistSongIndex(currentSongPlaylistIndex)));
        }

        /// <summary>
        /// Automatically play the next or random song based on current options
        /// </summary>
        private void AutoPlayNextOrRandom()
        {
            if (Shuffle) { LoadAndPlayRandomSongOnList(); return; }

            LoadAndPlayNextSong();
        }

        /// <summary>
        /// Get the active selected and loading song list item after changing playlists
        /// </summary>
        private void GetActiveSongListItems()
        {
            if(selectedPlaylist == currentSongPlaylistIndex)
            {
                int songListIndex = currentSongIndex - PlaylistIndices[currentSongPlaylistIndex];

                currentSongListItem = Songs.GetChild(songListIndex);

                if (player.IsPlaying && currentSongIndex == nextSongIndex)
                {
                    nextSongListItem = currentSongListItem;

                    HighlightSongListItem(true);
                }
            }

            if(selectedPlaylist == nextSongPlaylistIndex)
            {
                int songListIndex = nextSongIndex - PlaylistIndices[nextSongPlaylistIndex];

                nextSongListItem = Songs.GetChild(songListIndex);

                loadingSlider = nextSongListItem.GetComponentInChildren<Slider>(true);

                loadingSlider.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Enable or disable the highlight on the active song list item
        /// </summary>
        /// <param name="highlighted"></param>
        private void HighlightSongListItem(bool highlighted)
        {
            Transform listItem = highlighted ? nextSongListItem : currentSongListItem;

            if (!Utilities.IsValid(listItem)) { return; }

            Color color = highlighted ? HighlightColor : new Color(1f, 1f, 1f);

            foreach(Text text in listItem.GetComponentsInChildren<Text>())
            {
                text.color = color;
            }
        }

        /// <summary>
        /// Shows the error message on the screen with the provided message
        /// </summary>
        /// <param name="error"></param>
        private void ShowError(string error)
        {
            ErrorPrompt.SetActive(true);

            ErrorPrompt.transform.GetChild(1).GetComponent<Text>().text = error;
        }

        /// <summary>
        /// Song loading process
        /// </summary>
        private void LoadSong()
        {
            if (!loading) { return; }

            loadingTime += Time.deltaTime;

            UpdateLoadingProgressBar();

            if (loadingTime < LoadingTimeout) { return; }

            ShowError("Loading timed out!");

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
        /// Set loaded song as currently playing
        /// </summary>
        private void SetNextSongAsCurrent()
        {
            currentSongPlaylistIndex = nextSongPlaylistIndex;

            currentSongIndex = nextSongIndex;

            HighlightSongListItem(false);

            currentSongListItem = nextSongListItem;

            HighlightSongListItem(true);
        }

        /// <summary>
        /// Proxy for printing messages in logs
        /// </summary>
        /// <param name="text"></param>
        private void Log(string text)
        {
            if (Debugger) { Debugger.WriteLine($"{LogPrefix} {text}"); }

            Debug.Log($"{LogPrefix} {text}");
        }
        #endregion

        #region VRC Video Methods
        public override void OnVideoEnd()
        {
            Log(nameof(OnVideoEnd));

            UpdatePlayPauseButton(false);

            ResetSongInfo();

            if (loading) { return; }

            HighlightSongListItem(false);

            AutoPlayNextOrRandom();
        }

        public override void OnVideoStart() 
        {
            Log(nameof(OnVideoStart));

            UpdatePlayPauseButton(true);

            TextTitle.text = Titles[nextSongIndex];

            TextArtist.text = Artists[nextSongIndex];

            songDuration = player.GetDuration();

            TimeLength.text = GetFormattedTimeFromSeconds(songDuration);

            SetNextSongAsCurrent();
        }

        public override void OnVideoReady()
        {
            Log(nameof(OnVideoReady));

            averageLoadingTime = (averageLoadingTime + loadingTime) / 2f;

            FinishLoading();
        }

        public override void OnVideoError(VideoError videoError)
        {
            Log($"<color=#990000>{nameof(OnVideoError)}</color> {videoError}");

            SetNextSongAsCurrent();

            HighlightSongListItem(false);

            ResetSongInfo();

            FinishLoading();

            ShowError(videoError.ToString());
        }
        #endregion
    }

    #region Custom Editor
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(Tunify))]
    public class TunifyEditor : Editor
    {
        private Tunify tunify;

        private void OnEnable()
        {
            tunify = (Tunify)target;

            UdonBehaviour ub = tunify.GetComponent<UdonBehaviour>();

            if (ub == null)
            {
                UdonSharpEditorUtility.ConvertToUdonBehaviours(new UdonSharpBehaviour[] { tunify });

                return;
            }

            ub.AllowCollisionOwnershipTransfer = false;
            ub.SynchronizePosition = false;
        }

        public override void OnInspectorGUI()
        {
            DrawBanner();

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        private void DrawBanner()
        {
            GUI.color = new Color(0f, 0.5f, 1f);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.color = Color.white;

            GUILayout.Label("Varneon's UdonEssentials - Tunify", EditorStyles.whiteLargeLabel);

            GUILayout.BeginHorizontal();

            GUILayout.Label("Find more Udon prefabs at:", EditorStyles.whiteLabel, GUILayout.Width(160));

            if (GUILayout.Button("https://github.com/Varneon", EditorStyles.whiteLabel, GUILayout.Width(165)))
            {
                Application.OpenURL("https://github.com/Varneon");
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label($"[Library] Songs: {tunify.Urls.Length} | Playlists: {tunify.PlaylistIndices.Length}");

            GUILayout.EndHorizontal();
        }
    }
#endif
    #endregion
}