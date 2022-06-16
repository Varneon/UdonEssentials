using System;
using System.Collections.Generic;
using System.IO;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Playlist = Varneon.UdonPrefabs.Essentials.MusicPlayerEditor.MusicLibrary.Playlist;
using Song = Varneon.UdonPrefabs.Essentials.MusicPlayerEditor.MusicLibrary.Song;
using Object = UnityEngine.Object;

namespace Varneon.UdonPrefabs.Essentials.MusicPlayerEditor
{
    public class MusicPlayerManager : EditorWindow
    {
        public List<Song> Songs = new List<Song>();

        public List<Playlist> Playlists = new List<Playlist>();

        private Tunify player;

        private string[] playerNames;

        private int playerIndex;

        private int playerSongCount, playerPlaylistCount;

        private readonly List<Tunify> playersInScene = new List<Tunify>();

        private MusicLibrary activeLibrary, proxyLibrary;

        private bool isActiveLibraryDefault, isActiveLibraryDemo;

        private Preferences preferences;

        private ReorderableList songList, playlistList;

        private Vector2 scrollPosSongs, scrollPosPlaylists;

        private SerializedObject so;

        private Playlist tempPlaylist;

        private string newPlaylistName, playlistDescription;

        private bool creatingNewPlaylist, renamingPlaylist, pendingChanges;

        private PlaylistArguments playlistArguments = new PlaylistArguments();

        private UdonSharpProgramAsset playerProgram;

        private const string LogPrefix = "[<color=#4488CC>Music Player Manager</color>]:";

        private struct PlaylistArguments
        {
            public bool UseAutoplay;
            public bool IsCopyrighted;
        }

        [MenuItem("Varneon/Udon Prefab Editors/Music Player Manager")]
        public static void Init()
        {
            EditorWindow window = GetWindow<MusicPlayerManager>();
            window.titleContent.text = "Music Player Manager";
            window.titleContent.image = Resources.Load<Texture2D>("Icons/Note");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            so = new SerializedObject(this);

            playlistList = new ReorderableList(so, so.FindProperty(nameof(Playlists)), true, false, false, false);

            playlistList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = playlistList.serializedProperty.GetArrayElementAtIndex(index);

                rect.y += 2;

                GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.75f, EditorGUIUtility.singleLineHeight), Playlists[index].Name, EditorStyles.label);

                GUI.Label(new Rect(rect.x + rect.width - 100, rect.y, 100, EditorGUIUtility.singleLineHeight), Playlists[index].Songs.Count.ToString());
            };

            playlistList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(new Rect(rect.x + 15, rect.y, rect.width - 85, rect.height), "Name");
                EditorGUI.LabelField(new Rect(rect.x + rect.width - 100, rect.y, 100, rect.height), "Song Count");
            };

            playlistList.onSelectCallback = (ReorderableList l) =>
            {
                LoadPlaylistFromLibrary(playlistList.index);
            };

            playlistList.onReorderCallback = (ReorderableList l) =>
            {
                pendingChanges = true;
            };

            songList = new ReorderableList(so, so.FindProperty(nameof(Songs)), true, false, true, true);

            songList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = songList.serializedProperty.GetArrayElementAtIndex(index);

                rect.y += 2;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width / 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Name"), GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width / 4f, rect.y, rect.width / 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Artist"), GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + (rect.width / 4f) * 2, rect.y, rect.width / 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("URL"), GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + (rect.width / 4f) * 3, rect.y, rect.width / 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Tags"), GUIContent.none);
            };

            songList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(new Rect(rect.x + 15, rect.y, rect.width / 4f, rect.height), "Title");
                EditorGUI.LabelField(new Rect(rect.x + 10 + rect.width / 4f, rect.y, rect.width / 4f, rect.height), "Artist");
                EditorGUI.LabelField(new Rect(rect.x + 5 + rect.width / 4f * 2, rect.y, rect.width / 4f, rect.height), "URL");
                EditorGUI.LabelField(new Rect(rect.x + 5 + rect.width / 4f * 3, rect.y, rect.width / 4f, rect.height), "Tags");
            };

            songList.onReorderCallback = (ReorderableList l) =>
            {
                pendingChanges = true;

                WriteSongsToPlaylist(playlistList.index);
            };

            playerProgram = (UdonSharpProgramAsset)AssetDatabase.LoadAssetAtPath("Assets/Varneon/Udon Prefabs/Essentials/Music Player/Udon Programs/Tunify.asset", typeof(UdonSharpProgramAsset));

            LoadPreferences();

            TryToLoadDefaultLibrary();

            GetPlayersInScene();
        }

        private void OnDestroy()
        {
            PendingChangesPrompt();
        }

        private void OnFocus()
        {
            if(activeLibrary == null)
            {
                TryToLoadDefaultLibrary();
            }

            GetPlayersInScene();
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlaying) return;

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox, new GUILayoutOption[] { GUILayout.Height(175) }))
            {
                using (new GUILayout.VerticalScope())
                {
                    DrawBanner();

                    using (new EditorGUI.DisabledScope(creatingNewPlaylist || renamingPlaylist))
                    {
                        DrawFieldMusicPlayers();

                        DrawFieldLibraryFile();
                    }
                }

                using (new EditorGUI.DisabledScope(activeLibrary == null))
                {
                    DrawFieldPlaylistList();
                }
            }

            using (new EditorGUI.DisabledScope(creatingNewPlaylist || renamingPlaylist || activeLibrary == null))
            {
                DrawFieldPlaylistArguments();

                DrawFieldSonglist();

                GUILayout.FlexibleSpace();

                DrawFieldLibraryActions();
            }
        }

        #region Panels
        private void DrawFieldMusicPlayers()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Active Music Player:");

                BeginHorizontalHighlightPanel(player);

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    player = (Tunify)EditorGUILayout.ObjectField(player, typeof(Tunify), true);

                    if (scope.changed)
                    {
                        GetPlayerIndex();

                        RefreshPlayerStats();
                    }
                }

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    playerIndex = EditorGUILayout.Popup(playerIndex, playerNames, GUILayout.Width(120));

                    if (scope.changed)
                    {
                        player = playersInScene[playerIndex];

                        so.ApplyModifiedProperties();

                        RefreshPlayerStats();
                    }
                }

                GUILayout.EndHorizontal();

                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label($"Songs: {playerSongCount} | Playlists: {playerPlaylistCount}");

                    using (new EditorGUI.DisabledScope(player == null))
                    {
                        if (GUILayout.Button("Save To File", GUILayout.MaxWidth(85)))
                        {
                            SavePlayerPlaylistsToLibrary();
                        }
                    }
                }
            }
        }

        private void DrawFieldLibraryFile()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Active Music Library:");

                BeginHorizontalHighlightPanel(proxyLibrary);

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    proxyLibrary = (MusicLibrary)EditorGUILayout.ObjectField(proxyLibrary, typeof(MusicLibrary), false);

                    if (scope.changed)
                    {
                        PendingChangesPrompt();

                        SetActiveLibrary(proxyLibrary);

                        if (activeLibrary != null)
                        {
                            ImportPlaylistsFromLibrary();
                        }
                    }
                }

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    PendingChangesPrompt();

                    BrowseLibraryFile();
                }
                else if (GUILayout.Button("New", GUILayout.Width(40)))
                {
                    PendingChangesPrompt();

                    string path = EditorUtility.SaveFilePanelInProject("Create new music library file", "MusicLibrary", "asset", "Create new music library");

                    CreateNewLibraryFile(path);
                }

                using (new EditorGUI.DisabledScope(!activeLibrary))
                {
                    if (isActiveLibraryDefault)
                    {
                        GUILayout.Label("Default Library", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
                    }
                    else if (GUILayout.Button("Set As Default", GUILayout.Width(100)))
                    {
                        SetActiveLibraryAsDefault();
                    }
                }

                GUILayout.EndHorizontal();

                if (isActiveLibraryDemo)
                {
                    GUI.color = Color.yellow;

                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUI.color = Color.white;

                        GUILayout.Label("DemoMusicLibrary is part of UdonEssentials files and will get overwritten when updating the package! Please create a new playlist or change the name of the file if you want to use this library.", EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }

        private void DrawFieldPlaylistList()
        {
            using (new GUILayout.VerticalScope(new GUILayoutOption[] { GUILayout.Width(Screen.width / 2f) }))
            {
                using (new EditorGUI.DisabledScope(creatingNewPlaylist || renamingPlaylist))
                {
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        using (var scope = new EditorGUILayout.ScrollViewScope(scrollPosPlaylists))
                        {
                            scrollPosPlaylists = scope.scrollPosition;

                            so.Update();

                            playlistList.DoLayoutList();

                            so.ApplyModifiedProperties();
                        }
                    }
                }

                #region Playlist Actions
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (!creatingNewPlaylist && !renamingPlaylist)
                    {
                        GUILayout.Label("Playlist Actions:", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(90));

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Copy", GUILayout.Width(45)))
                        {
                            tempPlaylist = new Playlist(Playlists[playlistList.index].Name);

                            foreach (Song song in Songs)
                            {
                                tempPlaylist.Songs.Add(song);
                            }
                        }

                        using (new EditorGUI.DisabledScope(tempPlaylist.Equals(new Playlist())))
                        {
                            if (GUILayout.Button("Paste", GUILayout.Width(45)))
                            {
                                Playlists.Add(tempPlaylist);

                                pendingChanges = true;
                            }
                        }

                        if (GUILayout.Button("Rename", GUILayout.Width(65)))
                        {
                            newPlaylistName = Playlists[playlistList.index].Name;

                            renamingPlaylist = true;
                        }

                        if (GUILayout.Button("Add", GUILayout.Width(40)))
                        {
                            newPlaylistName = "New Playlist";

                            creatingNewPlaylist = true;
                        }

                        using (new EditorGUI.DisabledScope(Playlists.Count <= 1))
                        {
                            if (GUILayout.Button(new GUIContent("X", "Remove"), GUILayout.Width(20)))
                            {
                                RemovePlaylist();

                                pendingChanges = true;
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Playlist Name:");

                        newPlaylistName = EditorGUILayout.TextField(newPlaylistName);

                        if (GUILayout.Button(renamingPlaylist ? "Rename" : "Create", GUILayout.Width(70)))
                        {
                            if (renamingPlaylist)
                            {
                                WriteSongsToPlaylist(playlistList.index);

                                Playlists[playlistList.index] = new Playlist(newPlaylistName, Playlists[playlistList.index].Songs);
                            }
                            else
                            {
                                Playlists.Add(new Playlist(newPlaylistName));
                            }

                            pendingChanges = true;

                            ResetPlaylistNameField();
                        }
                        else if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                        {
                            ResetPlaylistNameField();
                        }
                    }
                }
                #endregion
            }
        }

        private void DrawFieldPlaylistArguments()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    playlistArguments.UseAutoplay = GUILayout.Toggle(playlistArguments.UseAutoplay, new GUIContent("Autoplay", "Can this playlist be played automatically at the beginning of the instance or randomly selected while playlist shuffle is active"), GUILayout.ExpandWidth(false));

                    playlistArguments.IsCopyrighted = GUILayout.Toggle(playlistArguments.IsCopyrighted, new GUIContent("Copyrighted", "Does this playlist contain copyrighted content that could potentially lead to issues in e.g. livestreams or videos"), GUILayout.ExpandWidth(false));

                    if (scope.changed)
                    {
                        Playlist playlist = Playlists[playlistList.index];

                        playlist.Args = GeneratePlaylistArgumentString(playlistArguments);

                        Playlists[playlistList.index] = playlist;

                        pendingChanges = true;
                    }
                }

                GUILayout.Label(new GUIContent("Description:", "Playlist description"), GUILayout.Width(72));

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    playlistDescription = GUILayout.TextField(playlistDescription);

                    if (scope.changed)
                    {
                        Playlist playlist = Playlists[playlistList.index];

                        playlist.Description = playlistDescription;

                        Playlists[playlistList.index] = playlist;

                        pendingChanges = true;
                    }
                }
            }
        }

        private void DrawFieldSonglist()
        {
            using ( new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosSongs))
                {
                    scrollPosSongs = scrollView.scrollPosition;

                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        so.Update();

                        songList.DoLayoutList();

                        so.ApplyModifiedProperties();

                        if (scope.changed)
                        {
                            WriteSongsToPlaylist(playlistList.index);

                            pendingChanges = true;
                        }
                    }
                }
            }
        }

        private void DrawFieldLibraryActions()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Library Actions:", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(90));

                GUILayout.FlexibleSpace();

                GUI.color = pendingChanges ? new Color(0.75f, 0.5f, 0f) : new Color(0f, 0.75f, 0f);

                GUILayout.Label(pendingChanges ? "Unsaved Changes!" : "Everything Saved!", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));

                GUI.color = Color.white;

                using (new EditorGUI.DisabledScope(!pendingChanges))
                {
                    if (GUILayout.Button("Save Library", GUILayout.MaxWidth(90)))
                    {
                        SaveMusicLibrary();
                    }
                }

                using (new EditorGUI.DisabledScope(player == null))
                {
                    if (GUILayout.Button("Apply Library To Player", GUILayout.MaxWidth(150)))
                    {
                        ApplyPlaylistsToPlayer();
                    }
                }
            }
        }

        private void DrawBanner()
        {
            GUI.color = new Color(0f, 0.5f, 1f);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.color = Color.white;

                GUILayout.Label("Varneon's Udon Prefabs", EditorStyles.whiteLargeLabel);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Find more Udon prefabs at:", EditorStyles.whiteLabel, GUILayout.Width(160));

                    if (GUILayout.Button("https://github.com/Varneon", EditorStyles.whiteLabel, GUILayout.Width(165)))
                    {
                        Application.OpenURL("https://github.com/Varneon");
                    }
                }
            }
        }

        private void BeginHorizontalHighlightPanel(object obj)
        {
            GUI.color = obj == null ? new Color(1f, 0f, 0f) : Color.white;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUI.color = Color.white;
        }
        #endregion

        #region Playlist Methods
        private void TryToLoadDefaultLibrary()
        {
            SetActiveLibrary(LoadDefaultOrAnyLibrary());

            if (activeLibrary != null)
            {
                ImportPlaylistsFromLibrary();

                return;
            }

            if (!EditorUtility.DisplayDialog("Couldn't find any music libraries in the project!", "Would you like to create a new music library file?", "Yes", "No"))
            {
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("Create a new music library file", "MusicLibrary", "asset", "Create new music library");

            CreateNewLibraryFile(path);
        }

        private void SetActiveLibraryAsDefault()
        {
            isActiveLibraryDefault = true;

            if (preferences != null)
            {
                preferences.DefaultLibrary = activeLibrary;

                SaveAsset(preferences);

                Debug.Log($"{LogPrefix} Preferences saved successfully!");

                return;
            }

            try
            {
                preferences = ScriptableObject.CreateInstance<Preferences>();

                preferences.DefaultLibrary = activeLibrary;

                string path = "Assets/Varneon/Udon Prefabs/Essentials/Music Player/Resources/Preferences.asset";

                string directory = Path.GetDirectoryName(path);

                Debug.Log(directory);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(preferences, path);

                SaveAsset(preferences);

                Debug.Log($"{LogPrefix} Preferences saved successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} {e}");
            }
        }

        private void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
        }

        private void LoadPreferences()
        {
            preferences = Resources.Load<Preferences>($"Preferences");
        }

        private MusicLibrary LoadDefaultOrAnyLibrary()
        {
            if (preferences != null && preferences.DefaultLibrary != null)
            {
                return preferences.DefaultLibrary;
            }

            string[] librariesInProject = AssetDatabase.FindAssets($"t:{typeof(MusicLibrary)}");

            if (librariesInProject.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<MusicLibrary>(AssetDatabase.GUIDToAssetPath(librariesInProject[0]));
            }

            return null;
        }

        private void CreateNewLibraryFile(string path, List<Playlist> playlists = null)
        {
            if (string.IsNullOrEmpty(path)) { return; }

            try
            {
                MusicLibrary newMusicLibrary = ScriptableObject.CreateInstance<MusicLibrary>();

                newMusicLibrary.Playlists = playlists ?? new List<Playlist>() { new Playlist("All Songs") };

                AssetDatabase.CreateAsset(newMusicLibrary, $"{path}");

                SetActiveLibrary(newMusicLibrary);

                ImportPlaylistsFromLibrary();

                SaveAsset(newMusicLibrary);
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Could not create new Music Library! ({e})");

                return;
            }
        }

        private void ImportPlaylistsFromLibrary()
        {
            Playlists.Clear();

            foreach (Playlist playlist in activeLibrary.Playlists)
            {
                Playlists.Add(playlist);
            }

            LoadPlaylistFromLibrary(playlistList.index = 0);

            playlistList.index = playlistList.index;
        }

        private void BrowseLibraryFile()
        {
            string path = EditorUtility.OpenFilePanel("Select music library", "Assets", "asset");

            if (System.IO.File.Exists(path))
            {
                MusicLibrary library;

                library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(path.Replace($"{System.IO.Directory.GetCurrentDirectory().Replace(@"\", "/")}/", ""));

                if (library != null)
                {
                    SetActiveLibrary(library);

                    ImportPlaylistsFromLibrary();
                }
            }
        }

        private void SaveMusicLibrary()
        {
            try
            {
                activeLibrary.Playlists.Clear();

                foreach (Playlist playlist in Playlists)
                {
                    activeLibrary.Playlists.Add(playlist);
                }

                SaveAsset(activeLibrary);

                pendingChanges = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Could not save Music Library! ({e})");

                return;
            }

            Debug.Log($"{LogPrefix} Music Library Saved Successfully!");
        }

        private void LoadPlaylistFromLibrary(int playlistIndex)
        {
            playlistDescription = Playlists[playlistIndex].Description;

            playlistArguments = ParsePlaylistArguments(Playlists[playlistIndex].Args);

            Songs.Clear();

            foreach (Song song in Playlists[playlistIndex].Songs)
            {
                Songs.Add(song);
            }
        }

        private PlaylistArguments ParsePlaylistArguments(string args)
        {
            PlaylistArguments newPlaylistArguments = new PlaylistArguments();

            if (string.IsNullOrEmpty(args)) { return newPlaylistArguments; }

            foreach(string arg in args.Split(' '))
            {
                switch (arg.TrimStart('-')[0])
                {
                    case 'a':
                        newPlaylistArguments.UseAutoplay = true;
                        break;
                    case 'c':
                        newPlaylistArguments.IsCopyrighted = true;
                        break;
                }
            }

            return newPlaylistArguments;
        }

        private string GeneratePlaylistArgumentString(PlaylistArguments args)
        {
            List<string> output = new List<string>();

            if (args.UseAutoplay) { output.Add("-a");  }
            if (args.IsCopyrighted) { output.Add("-c");  }

            return string.Join(" ", output.ToArray());
        }

        private void WriteSongsToPlaylist(int playlistIndex)
        {
            List<Song> newSongList = new List<Song>();

            foreach (Song song in Songs)
            {
                newSongList.Add(song);
            }

            Playlist playlist = Playlists[playlistIndex];

            playlist.Songs = newSongList;

            Playlists[playlistIndex] = playlist;
        }

        private void RemovePlaylist()
        {
            if (EditorUtility.DisplayDialog("Remove Playlist?", $"Are you sure you want to remove following playlist:\n\n{Playlists[playlistList.index].Name}", "Yes", "No"))
            {
                Playlists.RemoveAt(playlistList.index);

                LoadPlaylistFromLibrary(playlistList.index = 0);
            }
        }

        private void ResetPlaylistNameField()
        {
            newPlaylistName = string.Empty;

            creatingNewPlaylist = false;

            renamingPlaylist = false;

            GUI.FocusControl(null);
        }

        private void PendingChangesPrompt()
        {
            if (!pendingChanges) { return; }

            pendingChanges = false;

            if (!EditorUtility.DisplayDialog("You have pending changes to the active music library", "Would you like to save the changes to the active music library?", "Yes", "No")) { return; }

            SaveMusicLibrary();
        }

        private void SetActiveLibrary(MusicLibrary library)
        {
            activeLibrary = library;

            proxyLibrary = activeLibrary;

            isActiveLibraryDefault = preferences != null && activeLibrary == preferences.DefaultLibrary;

            isActiveLibraryDemo = activeLibrary != null && activeLibrary.name == "DemoMusicLibrary";
        }
        #endregion

        #region Player Actions
        private void ApplyPlaylistsToPlayer()
        {
            List<int> autoplayPlaylistIndices = new List<int>();
            List<int> copyrightFreePlaylistIndices = new List<int>();
            List<int> autoplayCopyrightFreePlaylistIndices = new List<int>();

            List<VRCUrl> urls = new List<VRCUrl>();
            List<string> titles = new List<string>();
            List<string> artists = new List<string>();
            List<string> tags = new List<string>();
            List<int> playlistIndices = new List<int>();
            List<string> playlistNames = new List<string>();
            List<string> playlistArgs = new List<string>();
            List<string> playlistDescriptions = new List<string>();

            int currentSongIndex = 0;

            foreach (Playlist playlist in activeLibrary.Playlists)
            {
                if (playlist.Args.Contains("-a")) { autoplayPlaylistIndices.Add(playlistIndices.Count); }

                if (!playlist.Args.Contains("-c")) { copyrightFreePlaylistIndices.Add(playlistIndices.Count); }

                playlistNames.Add(playlist.Name);

                playlistIndices.Add(currentSongIndex);

                playlistArgs.Add(playlist.Args);

                playlistDescriptions.Add(playlist.Description);

                currentSongIndex += playlist.Songs.Count;

                foreach (Song song in playlist.Songs)
                {
                    urls.Add(new VRCUrl(song.URL));

                    titles.Add(song.Name);

                    artists.Add(song.Artist);

                    tags.Add(song.Tags ?? string.Empty);
                }
            }

            foreach(int index in autoplayPlaylistIndices)
            {
                if (copyrightFreePlaylistIndices.Contains(index)) { autoplayCopyrightFreePlaylistIndices.Add(index); }
            }

            UdonBehaviour udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(player);

            IUdonVariableTable variables = udonBehaviour.publicVariables;

            bool isUdonSharpOne = variables.TryGetVariableValue("___UdonSharpBehaviourVersion___", out _);

            Undo.RecordObject(isUdonSharpOne ? player : (Object)udonBehaviour, "Apply music library to player");

            player.Urls = urls.ToArray();
            player.Titles = titles.ToArray();
            player.Artists = artists.ToArray();
            player.Tags = tags.ToArray();
            player.PlaylistIndices = playlistIndices.ToArray();
            player.PlaylistNames = playlistNames.ToArray();
            player.PlaylistArgs = playlistArgs.ToArray();
            player.PlaylistDescriptions = playlistDescriptions.ToArray();
            player.AutoplayPlaylistIndices = autoplayPlaylistIndices.ToArray();
            player.CopyrightFreePlaylistIndices = copyrightFreePlaylistIndices.ToArray();
            player.AutoplayCopyrightFreePlaylistIndices = autoplayCopyrightFreePlaylistIndices.ToArray();

            if(!isUdonSharpOne)
            {
                player.ApplyProxyModifications();
            }

            EditorUtility.SetDirty(isUdonSharpOne ? player : (Object)udonBehaviour);

            RefreshPlayerStats();

            Debug.Log($"{LogPrefix} Applied Music Library To Player Successfully!");
        }

        private void SavePlayerPlaylistsToLibrary()
        {
            VRCUrl[] urls = player.Urls;
            string[] titles = player.Titles;
            string[] artists = player.Artists;
            int[] playlistIndices = player.PlaylistIndices;
            string[] playlistNames = player.PlaylistNames;

            int songCount = Math.Min(urls.Length, Math.Min(titles.Length, artists.Length));

            if (songCount == 0) { Debug.Log($"{LogPrefix} There are no songs in the player!"); return; }

            List<Playlist> playlists = new List<Playlist>();

            for (int i = 0; i < playlistIndices.Length; i++)
            {
                Playlist playlist = new Playlist(playlistNames[i]);

                int playlistStartIndex = (playlistIndices.Length == 0) ? 0 : playlistIndices[i];

                int playlistEndIndex = (playlistIndices.Length > i + 1) ? playlistIndices[i + 1] : urls.Length;

                List<Song> songs = new List<Song>();

                for (int j = playlistStartIndex; j < playlistEndIndex; j++)
                {
                    songs.Add(new Song(titles[j], artists[j], urls[j].Get()));
                }

                playlist.Songs = songs;

                playlists.Add(playlist);
            }

            string filePath = EditorUtility.SaveFilePanelInProject("Save player's playlists to file", "PlayerLibrary", "asset", "Save playlists to file");

            CreateNewLibraryFile(filePath, playlists);
        }

        private void GetPlayerIndex()
        {
            for (int i = 0; i < playersInScene.Count; i++)
            {
                if (player == playersInScene[i])
                {
                    playerIndex = i;

                    return;
                }
            }
        }

        private void GetPlayersInScene()
        {
            playersInScene.Clear();

            UdonBehaviour[] udonBehaviours = FindObjectsOfType<UdonBehaviour>();

            List<string> names = new List<string>();

            foreach (UdonBehaviour ub in udonBehaviours)
            {
                if (ub.programSource == playerProgram)
                {
                    names.Add($"{ub.name} (ID: {ub.GetInstanceID()})");

                    playersInScene.Add(ub.GetUdonSharpComponent<Tunify>());
                }
            }

            playerNames = names.ToArray();

            if (playersInScene.Count == 0) { player = null; return; }

            if (player == null) { player = playersInScene[0]; }

            RefreshPlayerStats();
        }

        private void RefreshPlayerStats()
        {
            if(player == null) { return; }

            playerSongCount = player.Urls.Length;

            playerPlaylistCount = player.PlaylistIndices.Length;
        }
        #endregion
    }
}
