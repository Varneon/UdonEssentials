
#pragma warning disable IDE0044 // readonly modifier hides the serialized field in Unity inspector

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Varneon.UdonPrefabs.Essentials
{
    /// <summary>
    /// More advanced but still extremely simple replacement for the original VRCSDK's "VRCWorldSettings" UdonBehaviour
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SimplePlayerSettings : UdonSharpBehaviour
    {
        [Header("Local Player Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float strafeSpeedMultiplier = 1f;
        [SerializeField] private float jumpImpulse = 3f;

        [Space]
        [Header("Remote Player Voice")]
        [SerializeField] private float voiceGain = 15f;
        [SerializeField] private float voiceDistanceNear = 0f;
        [SerializeField] private float voiceDistanceFar = 25f;
        [SerializeField] private float voiceVolumetricRadius = 0;
        [SerializeField] private bool voiceLowpass = true;

        [Space]
        [Header("Remote Player Avatar Audio")]
        [SerializeField] private float avatarAudioGain = 10f;
        [SerializeField] private float avatarAudioDistanceNear = 40f;
        [SerializeField] private float avatarAudioDistanceFar = 40f;
        [SerializeField] private float avatarAudioVolumetricRadius = 0;
        [SerializeField] private bool avatarAudioForceSpatial = true;
        [SerializeField] private bool avatarAudioCustomCurve = true;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                player.SetWalkSpeed(walkSpeed);
                player.SetRunSpeed(runSpeed);
                player.SetStrafeSpeed(walkSpeed * strafeSpeedMultiplier);
                player.SetJumpImpulse(jumpImpulse);
            }
            else
            {
                player.SetVoiceGain(voiceGain);
                player.SetVoiceDistanceNear(voiceDistanceNear);
                player.SetVoiceDistanceFar(voiceDistanceFar);
                player.SetVoiceVolumetricRadius(voiceVolumetricRadius);
                player.SetVoiceLowpass(voiceLowpass);

                player.SetAvatarAudioGain(avatarAudioGain);
                player.SetAvatarAudioNearRadius(avatarAudioDistanceNear);
                player.SetAvatarAudioFarRadius(avatarAudioDistanceFar);
                player.SetAvatarAudioVolumetricRadius(avatarAudioVolumetricRadius);
                player.SetAvatarAudioForceSpatial(avatarAudioForceSpatial);
                player.SetAvatarAudioCustomCurve(avatarAudioCustomCurve);
            }
        }
    }
}
