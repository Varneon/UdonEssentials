#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Playlist = Varneon.UdonPrefabs.Essentials.Editor.MusicLibrary.Playlist;
using Song = Varneon.UdonPrefabs.Essentials.Editor.MusicLibrary.Song;

namespace Varneon.UdonPrefabs.Essentials.Editor
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

        private string newPlaylistName;

        private bool creatingNewPlaylist, renamingPlaylist, pendingChanges;

        private UdonSharpProgramAsset playerProgram;

        private static readonly string LibraryPath = "Assets/Varneon/Udon Prefabs/Essentials/Music Player/Music Library/";

        private static readonly string LogPrefix = "[<color=#000099>Music Player Manager</color>]:";

        [MenuItem("Varneon/Udon Prefab Editors/Music Player Manager")]
        public static void Init()
        {
            EditorWindow window = GetWindow<MusicPlayerManager>();
            window.titleContent.text = "Music Player Manager";
            window.titleContent.image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Varneon/Udon Prefabs/Essentials/Music Player/Textures/Note.png");
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
                GetSongsFromPlaylist(playlistList.index);
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
                    new Rect(rect.x, rect.y, rect.width / 3f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Name"), GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width / 3f, rect.y, rect.width / 3f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("Artist"), GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + (rect.width / 3f) * 2, rect.y, rect.width / 3f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("URL"), GUIContent.none);
            };

            songList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(new Rect(rect.x + 15, rect.y, rect.width / 3f, rect.height), "Title");
                EditorGUI.LabelField(new Rect(rect.x + 10 + rect.width / 3f, rect.y, rect.width / 3f, rect.height), "Artist");
                EditorGUI.LabelField(new Rect(rect.x + 5 + rect.width / 1.5f, rect.y, rect.width / 3f, rect.height), "URL");
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

            GUILayout.BeginHorizontal(EditorStyles.helpBox, new GUILayoutOption[] { GUILayout.Height(175) });

            GUILayout.BeginVertical();

            DrawBanner();

            EditorGUI.BeginDisabledGroup(creatingNewPlaylist || renamingPlaylist);

            DrawFieldMusicPlayers();

            DrawFieldLibraryFile();

            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();

            EditorGUI.BeginDisabledGroup(activeLibrary == null);

            DrawFieldPlaylistList();

            EditorGUI.BeginDisabledGroup(creatingNewPlaylist || renamingPlaylist);

            GUILayout.EndHorizontal();

            DrawFieldSonglist();

            GUILayout.FlexibleSpace();

            DrawFieldPlaylistActions();

            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();
        }

        #region Panels
        private void DrawFieldMusicPlayers()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Active Music Player:", GUILayout.Width(120));

            DrawFieldHighlightPanel(player);

            EditorGUI.BeginChangeCheck();

            player = (Tunify)EditorGUILayout.ObjectField(player, typeof(Tunify), true);

            if (EditorGUI.EndChangeCheck())
            {
                GetPlayerIndex();

                RefreshPlayerStats();
            }

            EditorGUI.BeginChangeCheck();

            playerIndex = EditorGUILayout.Popup(playerIndex, playerNames, GUILayout.Width(120));

            if (EditorGUI.EndChangeCheck())
            {
                player = playersInScene[playerIndex];

                so.ApplyModifiedProperties();

                RefreshPlayerStats();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label($"Songs: {playerSongCount} | Playlists: {playerPlaylistCount}");

            EditorGUI.BeginDisabledGroup(player == null);

            if (GUILayout.Button("Save To File", GUILayout.MaxWidth(85)))
            {
                SavePlayerPlaylistsToLibrary();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawFieldLibraryFile()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Active Music Library:", GUILayout.Width(125));

            DrawFieldHighlightPanel(proxyLibrary);

            EditorGUI.BeginChangeCheck();

            proxyLibrary = (MusicLibrary)EditorGUILayout.ObjectField(proxyLibrary, typeof(MusicLibrary), false);

            if (EditorGUI.EndChangeCheck())
            {
                PendingChangesPrompt();

                SetActiveLibrary(proxyLibrary);

                if (activeLibrary != null)
                {
                    ImportPlaylistsFromLibrary();
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

                string path = EditorUtility.SaveFilePanelInProject("Create new music library file", "MusicLibrary", "asset", "Create new music library", LibraryPath);

                CreateNewLibraryFile(path);
            }

            EditorGUI.BeginDisabledGroup(!activeLibrary);

            if (isActiveLibraryDefault)
            {
                GUILayout.Label("Default Library", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
            }
            else if (GUILayout.Button("Set As Default", GUILayout.Width(100)))
            {
                SetActiveLibraryAsDefault();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if(isActiveLibraryDemo)
            {
                GUI.color = Color.yellow;

                GUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUI.color = Color.white;

                GUILayout.Label("DemoMusicLibrary is part of UdonEssentials files and will get overwritten when updating the package! Please create a new playlist or change the name of the file if you want to use this library.", EditorStyles.wordWrappedLabel);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void DrawFieldPlaylistList()
        {
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(Screen.width / 2f) });

            EditorGUI.BeginDisabledGroup(creatingNewPlaylist || renamingPlaylist);

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            scrollPosPlaylists = EditorGUILayout.BeginScrollView(scrollPosPlaylists);

            so.Update();

            playlistList.DoLayoutList();

            so.ApplyModifiedProperties();

            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();

            #region Playlist Actions
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

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

                EditorGUI.BeginDisabledGroup(tempPlaylist.Equals(new Playlist()));

                if (GUILayout.Button("Paste", GUILayout.Width(45)))
                {
                    Playlists.Add(tempPlaylist);

                    pendingChanges = true;
                }

                EditorGUI.EndDisabledGroup();

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

                EditorGUI.BeginDisabledGroup(Playlists.Count <= 1);

                if (GUILayout.Button(new GUIContent("X", "Remove"), GUILayout.Width(20)))
                {
                    RemovePlaylist();

                    pendingChanges = true;
                }

                EditorGUI.EndDisabledGroup();
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

            GUILayout.EndHorizontal();
            #endregion

            GUILayout.EndVertical();
        }

        private void DrawFieldSonglist()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            scrollPosSongs = EditorGUILayout.BeginScrollView(scrollPosSongs);

            EditorGUI.BeginChangeCheck();

            so.Update();

            songList.DoLayoutList();

            so.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                WriteSongsToPlaylist(playlistList.index);

                pendingChanges = true;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFieldPlaylistActions()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label("Library Actions:", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(90));

            GUILayout.FlexibleSpace();

            GUI.color = pendingChanges ? new Color(0.75f, 0.5f, 0f) : new Color(0f, 0.75f, 0f);

            GUILayout.Label(pendingChanges ? "Unsaved Changes!" : "Everything Saved!", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));

            GUI.color = Color.white;

            EditorGUI.BeginDisabledGroup(!pendingChanges);

            if (GUILayout.Button("Save Library", GUILayout.MaxWidth(90)))
            {
                SaveMusicLibrary();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(player == null);

            if (GUILayout.Button("Apply Library To Player", GUILayout.MaxWidth(150)))
            {
                ApplyPlaylistsToPlayer();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }

        private void DrawBanner()
        {
            GUI.color = new Color(0f, 0.5f, 1f);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.color = Color.white;

            GUILayout.Label("Varneon's Udon Prefabs", EditorStyles.whiteLargeLabel);

            GUILayout.BeginHorizontal();

            GUILayout.Label("Find more Udon prefabs at:", EditorStyles.whiteLabel, GUILayout.Width(160));

            if (GUILayout.Button("https://github.com/Varneon", EditorStyles.whiteLabel, GUILayout.Width(165)))
            {
                Application.OpenURL("https://github.com/Varneon");
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawFieldHighlightPanel(object obj)
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

            string path = EditorUtility.SaveFilePanelInProject("Create a new music library file", "MusicLibrary", "asset", "Create new music library", LibraryPath);

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

                AssetDatabase.CreateAsset(preferences, $"{LibraryPath}Preferences.asset");

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
            preferences = AssetDatabase.LoadAssetAtPath<Preferences>($"{LibraryPath}Preferences.asset");
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

            GetSongsFromPlaylist(playlistList.index = 0);

            playlistList.index = playlistList.index;
        }

        private void BrowseLibraryFile()
        {
            string path = EditorUtility.OpenFilePanel("Select music library", LibraryPath, "asset");

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

        private void GetSongsFromPlaylist(int playlistIndex)
        {
            Songs.Clear();

            foreach (Song song in Playlists[playlistIndex].Songs)
            {
                Songs.Add(song);
            }
        }

        private void WriteSongsToPlaylist(int playlistIndex)
        {
            List<Song> newSongList = new List<Song>();

            foreach (Song song in Songs)
            {
                newSongList.Add(song);
            }

            Playlists[playlistIndex] = new Playlist(Playlists[playlistIndex].Name, newSongList);
        }

        private void RemovePlaylist()
        {
            if (EditorUtility.DisplayDialog("Remove Playlist?", $"Are you sure you want to remove following playlist:\n\n{Playlists[playlistList.index].Name}", "Yes", "No"))
            {
                Playlists.RemoveAt(playlistList.index);

                GetSongsFromPlaylist(playlistList.index = 0);
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
            if (PrefabUtility.GetNearestPrefabInstanceRoot(player) != null)
            {
                if (!EditorUtility.DisplayDialog("Unpack prefab for playlist update?", "The player prefab has to be unpacked before changing the playlist data.\n\nContinue?", "OK", "Cancel")) { return; }

                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(player), PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }

            List<VRCUrl> urls = new List<VRCUrl>();
            List<string> titles = new List<string>();
            List<string> artists = new List<string>();
            List<int> playlistIndices = new List<int>();
            List<string> playlistNames = new List<string>();

            int currentSongIndex = 0;

            foreach (Playlist playlist in activeLibrary.Playlists)
            {
                playlistNames.Add(playlist.Name);

                playlistIndices.Add(currentSongIndex);

                currentSongIndex += playlist.Songs.Count;

                foreach (Song song in playlist.Songs)
                {
                    urls.Add(new VRCUrl(song.URL));

                    titles.Add(song.Name);

                    artists.Add(song.Artist);
                }
            }

            Undo.RecordObject(player.GetComponent<UdonBehaviour>(), "Apply music library to player");

            player.Urls = urls.ToArray();
            player.Titles = titles.ToArray();
            player.Artists = artists.ToArray();
            player.PlaylistIndices = playlistIndices.ToArray();
            player.PlaylistNames = playlistNames.ToArray();

            player.ApplyProxyModifications();

            RefreshPlayerStats();
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

            string filePath = EditorUtility.SaveFilePanelInProject("Save player's playlists to file", "PlayerLibrary", "asset", "Save playlists to file", LibraryPath);

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
#endif