using UdonSharp;
using UnityEngine;
using Varneon.UdonPrefabs.Abstract;
using Varneon.UdonPrefabs.Essentials.TunifySyncEnums;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Essentials
{
    [DefaultExecutionOrder(1)] // Ensure that this behaviour runs after the main Tunify behaviour
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TunifySync : UdonSharpBehaviour
    {
        [SerializeField]
        private bool allowOwnershipClaimAtStart;

        [SerializeField]
        private UdonLogger logger;

        private VRCPlayerApi localPlayer;

        private Tunify tunify;

        private bool linked;

        private bool isLocalPlayerOwner;

        [UdonSynced]
        private bool allowOwnershipTransfer;

        private bool lastAllowOwnershipTransfer;

        /// <summary>
        /// Type of synced action that was executed
        /// </summary>
        [UdonSynced]
        private byte syncedActionType;

        /// <summary>
        /// Synced index of the current song
        /// </summary>
        [UdonSynced]
        private int syncedSongIndex;

        /// <summary>
        /// Synced start time of the current song
        /// </summary>
        [UdonSynced]
        private int syncedStartTime;

        /// <summary>
        /// Synced start time offset of the current song
        /// </summary>
        [UdonSynced]
        private float syncedStartTimeOffset;

        [UdonSynced]
        private int syncedActionTimestamp;

        private int lastSyncedActionTimestamp;

        private bool isSyncedSongPlaying;

        private TunifySyncedSongState syncedSongState;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            Log(string.Format("<color=silver>[{0}]</color> {1} requested ownership for <color=silver>[{2}]<color> {3}", requestingPlayer.playerId, requestingPlayer.displayName, requestedOwner.playerId, requestedOwner.displayName));

            return allowOwnershipTransfer;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            Log(string.Format("Ownership has been transferred to <color=silver>[{0}]</color> {1}", player.playerId, player.displayName));

            isLocalPlayerOwner = player.isLocal;

            tunify._SetLocalPlayerOwnerStatus(isLocalPlayerOwner);
        }

        internal void _OnSongSelected(int index)
        {
            if (!isLocalPlayerOwner) { return; }

            Log($"_OnSongSelected({index})");

            syncedActionType = (byte)TunifySyncActionType.SongSelected;

            syncedSongIndex = index;

            RequestSerializationWithTimestamp();
        }

        internal void _OnSongStarted(int index, float time = 0f)
        {
            Log($"_OnSongStarted({index}, {time})");

            if (isLocalPlayerOwner)
            {
                syncedActionType = (byte)TunifySyncActionType.SongStarted;

                syncedStartTime = Networking.GetServerTimeInMilliseconds();

                syncedStartTimeOffset = time;

                syncedSongIndex = index;

                RequestSerializationWithTimestamp();
            }
            else
            {
                switch (syncedSongState)
                {
                    case TunifySyncedSongState.Loading:
                        isSyncedSongPlaying = index == syncedSongIndex;

                        if (isSyncedSongPlaying)
                        {
                            Log("tunify._Pause()");

                            tunify._Pause();

                            syncedSongState = TunifySyncedSongState.WaitingForOwner;
                        }
                        break;
                    case TunifySyncedSongState.WaitingForLocal:
                        isSyncedSongPlaying = index == syncedSongIndex;

                        if (isSyncedSongPlaying)
                        {
                            Log("waitingForSyncedSongToLoad");

                            tunify._SetPlaybackTime(GetPlaybackOffset());

                            syncedSongState = TunifySyncedSongState.Playing;
                        }

                        break;
                    case TunifySyncedSongState.Paused:

                        tunify._Play();

                        syncedSongState = TunifySyncedSongState.Playing;

                        break;
                }
            }
        }

        internal void _OnSongStopped()
        {
            if (!isLocalPlayerOwner) { return; }

            Log("_OnSongStopped()");

            syncedActionType = (byte)TunifySyncActionType.SongStopped;

            RequestSerializationWithTimestamp();
        }

        public override void OnDeserialization()
        {
            Log($"OnDeserialization() {lastSyncedActionTimestamp}, {syncedActionTimestamp}");

            if (lastSyncedActionTimestamp == syncedActionTimestamp) { return; }

            lastSyncedActionTimestamp = syncedActionTimestamp;

            TunifySyncActionType type = (TunifySyncActionType)syncedActionType;

            if(lastAllowOwnershipTransfer != allowOwnershipTransfer)
            {
                lastAllowOwnershipTransfer = allowOwnershipTransfer;

                Log($"tunify._SetAllowOwnershipClaims({allowOwnershipTransfer})");

                tunify._SetAllowOwnershipClaims(allowOwnershipTransfer);
            }

            switch (type)
            {
                // If the synced action was song selected, select the song locally and set isSyncedSongPlaying flag to false
                case TunifySyncActionType.SongSelected:
                    Log("TunifySyncActionType.SongSelected");
                    tunify._SelectSong(syncedSongIndex);

                    syncedSongState = TunifySyncedSongState.Loading;

                    break;
                case TunifySyncActionType.SongStarted:
                    Log("TunifySyncActionType.SongStarted");

                    // If the local player is already playing the song, sync the time with the owner
                    if (syncedSongState == TunifySyncedSongState.WaitingForOwner || syncedSongState == TunifySyncedSongState.Paused)
                    {
                        Log("isSyncedSongPlaying");
                        tunify._SetPlaybackTime(GetPlaybackOffset());

                        tunify._Play();

                        syncedSongState = TunifySyncedSongState.Playing;
                    }
                    else // If local player haven't started the song yet, set the flag
                    {
                        Log("waitingForSyncedSongToLoad = true");

                        // If local player hasn't received the last instruction to load the song, load it
                        if (syncedSongState == TunifySyncedSongState.None)
                        {
                            Log("!isSyncedSongLoading && !isSyncedSongPlaying");
                            tunify._SelectSong(syncedSongIndex);

                            syncedSongState = TunifySyncedSongState.Loading;
                        }
                        else
                        {
                            syncedSongState = TunifySyncedSongState.WaitingForLocal;
                        }
                    }
                    break;
                case TunifySyncActionType.SongStopped:
                    Log("TunifySyncActionType.SongStopped");

                    tunify._Pause();

                    syncedSongState = TunifySyncedSongState.Paused;
                    break;
            }
        }

        internal void _ClaimOwnership()
        {
            Log("_ClaimOwnership()");

            if (!isLocalPlayerOwner && allowOwnershipTransfer)
            {
                Log("Networking.SetOwner(localPlayer, gameObject)");
                Networking.SetOwner(localPlayer, gameObject);
            }
        }

        internal void _SetAllowOwnershipClaims(bool allow)
        {
            if (isLocalPlayerOwner && allowOwnershipTransfer != allow)
            {
                allowOwnershipTransfer = allow;

                lastAllowOwnershipTransfer = allow;

                RequestSerializationWithTimestamp();
            }
        }

        internal void _Link(Tunify player)
        {
            if (linked) { return; }

            isLocalPlayerOwner = Networking.IsOwner(gameObject);

            tunify = player;

            tunify._SetLocalPlayerOwnerStatus(isLocalPlayerOwner);

            if(allowOwnershipClaimAtStart && isLocalPlayerOwner)
            {
                _SetAllowOwnershipClaims(true);

                tunify._SetAllowOwnershipClaims(true);
            }
        }

        private void Log(string message)
        {
            string logMessage = string.Format("[<color=cyan>TunifySync</color>]: {0}", message);

            Debug.Log(logMessage);

            if (logger) { logger._Log(logMessage); }
        }

        private float GetSecondsSinceSyncedAction()
        {
            int millisecondsSinceAction = Networking.GetServerTimeInMilliseconds() - syncedStartTime;

            return (float)millisecondsSinceAction / 1000f;
        }

        private float GetPlaybackOffset()
        {
            float time = GetSecondsSinceSyncedAction() + syncedStartTimeOffset;

            if(time < 0f) { time = 0f; }

            return time;
        }

        private void RequestSerializationWithTimestamp()
        {
            syncedActionTimestamp = Networking.GetServerTimeInMilliseconds();

            Log($"RequestSerializationWithTimestamp(), {syncedActionTimestamp}"); 

            RequestSerialization();
        }
    }
}
