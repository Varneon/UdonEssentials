using System;
using System.Collections.Generic;
using UnityEngine;

namespace Varneon.UdonPrefabs.Essentials.MusicPlayerEditor
{
    [CreateAssetMenu(fileName = "My Music Library", menuName = "ScriptableObjects/VFabs Tunify Library", order = 1)]
    public class MusicLibrary : ScriptableObject
    {
        public List<Playlist> Playlists = new List<Playlist>();

        [Serializable]
        public struct Playlist
        {
            public string Name;
            public List<Song> Songs;
            public string Args;
            public string Description;

            public Playlist(string name, List<Song> songs = null, string args = "-a -c", string description = "")
            {
                Name = name;
                Songs = songs ?? new List<Song>();
                Args = args;
                Description = description;
            }
        }

        [Serializable]
        public struct Song
        {
            public string Name;
            public string Artist;
            public string URL;
            public string Tags;

            public Song(string name, string artist, string url, string tags = null)
            {
                Name = name;
                Artist = artist;
                URL = url;
                Tags = tags;
            }
        }
    }
}