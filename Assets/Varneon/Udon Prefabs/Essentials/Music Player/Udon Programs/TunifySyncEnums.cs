namespace Varneon.UdonPrefabs.Essentials.TunifySyncEnums
{
    public enum TunifySyncActionType
    {
        None,
        SongSelected,
        SongStarted,
        SongStopped
    }

    public enum TunifySyncedSongState
    {
        None,
        Loading,
        Playing,
        Paused,
        WaitingForOwner,
        WaitingForLocal
    }
}
