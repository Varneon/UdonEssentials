#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore

using System;
using UdonSharp;
using UnityEngine;
using Varneon.UdonPrefabs.Common.SeatEnums;
using VRC.SDKBase;
using VRCStation = VRC.SDK3.Components.VRCStation;

namespace Varneon.UdonPrefabs.Abstract
{
    [RequireComponent(typeof(VRCStation))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class Seat : UdonSharpBehaviour
    {
        #region Serialized Fields
        /// <summary>
        /// Method for calibrating the seat
        /// </summary>
        [Header("Settings")]
        [SerializeField]
        private SeatCalibrationMethod calibrationMethod;

        /// <summary>
        /// Reference transform for aligning the player's head
        /// </summary>
        [Space]
        [Header("References")]
        [SerializeField]
        private Transform headCalibrationPoint;

        /// <summary>
        /// Reference transform for aligning the player's hip bone
        /// </summary>
        [SerializeField]
        private Transform hipsCalibrationPoint;
        #endregion

        #region Synced Variables
        /// <summary>
        /// Synced ID of the player in the seat
        /// </summary>
        /// <remarks>
        /// Will be 0 if no one is sitting in the seat
        /// </remarks>
        [UdonSynced]
        private int playerId;

        /// <summary>
        /// Synced position data of the seat
        /// </summary>
        /// <remarks>
        /// [0]: Z-axis, [1]: Y-axis
        /// </remarks>
        [UdonSynced]
        private sbyte[] syncedSeatPosition = new sbyte[2];
        #endregion

        #region Public Properties
        /// <summary>
        /// Synced ID of the player in the seat
        /// </summary>
        /// <remarks>
        /// Will be 0 if no one is sitting in the seat
        /// </remarks>
        public int PlayerId { get => playerId; }

        /// <summary>
        /// Is the local player currently sitting in the seat
        /// </summary>
        public bool IsLocalPlayerSitting { get => isLocalPlayerSitting; private set { isLocalPlayerSitting = value; } }
        #endregion

        #region Private Variables
        /// <summary>
        /// Local player
        /// </summary>
        protected VRCPlayerApi localPlayer { get; private set; }

        /// <summary>
        /// ID of the local player
        /// </summary>
        protected int localPlayerId { get; private set; }

        /// <summary>
        /// Is the local player in VR
        /// </summary>
        protected bool vrEnabled { get; private set; }

        /// <summary>
        /// Is the local player currently sitting in the seat
        /// </summary>
        protected bool isLocalPlayerSitting { get; private set; }

        /// <summary>
        /// VRCStation component attached to the seat
        /// </summary>
        protected VRCStation station { get; private set; }

        /// <summary>
        /// PlayerEnterLocation of the VRCStation attached to the seat
        /// </summary>
        protected Transform seatEnterLocation { get; private set; }

        /// <summary>
        /// The original angle of the seat before player has attempted to sit on it
        /// </summary>
        private Quaternion originalAngle;

        /// <summary>
        /// Local position data of the seat
        /// </summary>
        /// <remarks>
        /// [0]: Z-axis, [1]: Y-axis
        /// </remarks>
        private sbyte[] localSeatPosition = new sbyte[2];
        #endregion

        /// <summary>
        /// Initializes the core seat
        /// </summary>
        protected internal void Initialize()
        {
            vrEnabled = (localPlayer = Networking.LocalPlayer).IsUserInVR();

            localPlayerId = localPlayer.playerId;

            station = GetComponent<VRCStation>();

            seatEnterLocation = station.stationEnterPlayerLocation;

            originalAngle = seatEnterLocation.localRotation;
        }

        public override void Interact()
        {
            SitInStation();
        }

        /// <summary>
        /// Starts the sitting procedure
        /// </summary>
        private protected virtual void SitInStation()
        {
            // Before sitting, ensure that we are the owner of the seat
            if (!Networking.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }

            // If VR is enabled, reset pitch and roll temporarily
            if (vrEnabled)
            {
                seatEnterLocation.eulerAngles = new Vector3(0f, seatEnterLocation.eulerAngles.y, 0f);
            }

            OnCalibrationStarted();

            station.UseStation(localPlayer);

            playerId = localPlayerId;

            RequestSerialization();

            isLocalPlayerSitting = true;

            if (vrEnabled)
            {
                _ResetStationRotation();
            }

            if (calibrationMethod == SeatCalibrationMethod.None) { return; }

            SendCustomEventDelayedSeconds(nameof(_CalibrateSeatPosition), 1f);
        }

        public void _ResetStationRotation()
        {
            seatEnterLocation.localRotation = originalAngle;
        }

        public void _CalibrateSeatPosition()
        {
            if (!IsLocalPlayerSitting) { return; }

            Vector3 offset = vrEnabled ? new Vector3(0f, -0.05f, 0.1f) : Vector3.zero;

            switch (calibrationMethod)
            {
                case SeatCalibrationMethod.Head:
                    Vector3 headPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

                    seatEnterLocation.Translate(Vector3.Scale(seatEnterLocation.InverseTransformVector(headCalibrationPoint.position - headPos) + offset, new Vector3(0f, 1f, 1f)), Space.Self);
                    break;
                case SeatCalibrationMethod.Hips:
                    Vector3 hipsPos = localPlayer.GetBonePosition(HumanBodyBones.Hips);

                    // If hip bone doesn't exist, we can't calibrate based on it
                    if (hipsPos == Vector3.zero) { break; }

                    seatEnterLocation.Translate(Vector3.Scale(seatEnterLocation.InverseTransformVector(hipsCalibrationPoint.position - hipsPos), new Vector3(0f, 1f, 1f)), Space.Self);
                    break;
            }

            OnCalibrationFinished();

            SyncPosition();
        }

        private void SyncPosition()
        {
            Vector3 localSeatPos = seatEnterLocation.localPosition;

            localSeatPosition = new sbyte[] { (sbyte)Mathf.Clamp(Mathf.RoundToInt(localSeatPos.z * 100f), sbyte.MinValue, sbyte.MaxValue), (sbyte)Mathf.Clamp(Mathf.RoundToInt(localSeatPos.y * 100f), sbyte.MinValue, sbyte.MaxValue) };

            RequestSerialization();
        }

        private void AdjustSeatOnRemote()
        {
            if (syncedSeatPosition.Length > 1)
            {
                Vector3 newPosition = new Vector3(0f, (float)syncedSeatPosition[1] / 100f, (float)syncedSeatPosition[0] / 100f);

                seatEnterLocation.localPosition = newPosition;
            }
        }

        #region Protected Virtual Methods

        protected virtual void OnCalibrationStarted() { }

        protected virtual void OnCalibrationFinished() { }

        [Obsolete("Use OnPlayerEnteredSeat(VRCPlayerApi player) instead")]
        protected virtual void OnPlayerEnteredSeat(int playerId) { }

        [Obsolete("Use OnPlayerExitedSeat(VRCPlayerApi player) instead")]
        protected virtual void OnPlayerExitedSeat(int playerId) { }

        protected virtual void OnPlayerEnteredSeat(VRCPlayerApi player) { }

        protected virtual void OnPlayerExitedSeat(VRCPlayerApi player) { }

        #endregion // Protected Virtual Methods

        #region VRC Method Overrides

        public override void OnPreSerialization()
        {
            syncedSeatPosition = localSeatPosition;
        }

        public override void OnDeserialization()
        {
            AdjustSeatOnRemote();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            OnPlayerEnteredSeat(player);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            OnPlayerExitedSeat(player);

            if (player.isLocal)
            {
                playerId = -1;

                RequestSerialization();

                isLocalPlayerSitting = false;
            }
        }

        #endregion // VRC Method Overrides

        #region Public Methods

        /// <summary>
        /// Ejects the local player out of the seat
        /// </summary>
        public virtual void _Eject()
        {
            station.ExitStation(localPlayer);
        }

        #endregion // Public Methods
    }
}