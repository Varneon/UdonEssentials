using System;
using System.Collections.Generic;
using UnityEngine;

namespace Varneon.UdonPrefabs.Essentials.Editor
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

            public Playlist(string name, List<Song> songs = null)
            {
                Name = name;
                Songs = songs ?? new List<Song>();
            }
        }

        [Serializable]
        public struct Song
        {
            public string Name;
            public string Artist;
            public string URL;
            public string Description;

            public Song(string name, string artist, string url, string description = "")
            {
                Name = name;
                Artist = artist;
                URL = url;
                Description = description;
            }
        }
    }
}