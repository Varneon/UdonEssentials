using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using Varneon.UdonPrefabs.Core.NoclipEnums;
using Varneon.VInspector;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Varneon.UdonPrefabs.Core
{
    /// <summary>
    /// Simple noclip
    /// </summary>
    [SelectionBase]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Noclip : UdonSharpBehaviour
    {
        #region Serialized Fields
        /// <summary>
        /// Methdod for triggering noclip
        /// </summary>
        [SerializeField]
        [Tooltip("Method for triggering the noclip mode")]
        [FieldParentElement("Foldout_Settings")]
        private NoclipTriggerMethod noclipTriggerMethod = NoclipTriggerMethod.DoubleJump;

        /// <summary>
        /// Time in which jump has to be double tapped in order to toggle noclip
        /// </summary>
        [SerializeField]
        [Tooltip("Time in which jump has to be double tapped in order to toggle noclip")]
        [Range(0.1f, 1f)]
        [FieldParentElement("Foldout_Settings")]
        private float toggleThreshold = 0.25f;

        /// <summary>
        /// Maximum speed in m/s
        /// </summary>
        [SerializeField]
        [Tooltip("Maximum speed in m/s")]
        [Range(1f, 50f)]
        [FieldParentElement("Foldout_Settings")]
        private float speed = 15f;

        /// <summary>
        /// Exponent of the linear input for raising the speed to the power of
        /// </summary>
        [SerializeField]
        [Tooltip("[1 = Linear input curve | 1< = Exponential input curve] Helps with 'crawling' slowly when just slightly moving joystick even with higher standard speed")]
        [Range(1f, 5f)]
        [FieldParentElement("Foldout_VR")]
        [FieldLabel("Input Exponent")]
        private float vrSpeedExponent = 2f;

        /// <summary>
        /// Speed multiplier when Shift is not pressed
        /// </summary>
        [SerializeField]
        [Tooltip("Speed multiplier when Shift is not pressed")]
        [Range(0.1f, 1f)]
        [FieldParentElement("Foldout_Desktop")]
        [FieldLabel("Speed Fraction")]
        private float desktopSpeedFraction = 0.25f;

        /// <summary>
        /// Allow vertical movement on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Allow vertical movement on desktop")]
        [FieldParentElement("Foldout_Desktop")]
        [FieldLabel("Allow Vertical Input")]
        private bool desktopVerticalInput = true;

        /// <summary>
        /// Key for ascending on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Key for ascending on desktop")]
        [FieldParentElement("Foldout_Desktop")]
        private KeyCode upKey = KeyCode.E;

        /// <summary>
        /// Key for descending on desktop
        /// </summary>
        [SerializeField]
        [Tooltip("Key for descending on desktop")]
        [FieldParentElement("Foldout_Desktop")]
        private KeyCode downKey = KeyCode.Q;
        #endregion // Serialized Fields

        #region Private Variables
        /// <summary>
        /// Can double jump be used to toggle noclip
        /// </summary>
        private bool toggleByDoubleJump;

        /// <summary>
        /// Is the noclip currently enabled
        /// </summary>
        private bool noclipEnabled;

        /// <summary>
        /// Is the switching currently primed, waiting for second jump press within the threshold
        /// </summary>
        private bool switchPrimed;

        /// <summary>
        /// Current position of the player
        /// </summary>
        private Vector3 position;
        
        /// <summary>
        /// Last position of the player
        /// </summary>
        /// <remarks>
        /// Used for applying remaining velocity to the player after switching noclip off
        /// </remarks>
        private Vector3 lastPosition;

        /// <summary>
        /// Local player
        /// </summary>
        private VRCPlayerApi localPlayer;

        /// <summary>
        /// Is local player currently in VR
        /// </summary>
        private bool vrEnabled;

        /// <summary>
        /// Collider buffer for detecting local player's collider
        /// </summary>
        private Collider[] playerCollider = new Collider[1];

        private const string
            AXIS_HORIZONTAL = "Horizontal",
            AXIS_VERTICAL = "Vertical",
            AXIS_RIGHT_THUMBSTICK_VERTICAL = "Oculus_CrossPlatform_SecondaryThumbstickVertical";
        #endregion // Private Variables

        #region Unity Methods
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            vrEnabled = localPlayer.IsUserInVR();

            toggleByDoubleJump = noclipTriggerMethod == NoclipTriggerMethod.DoubleJump;
        }

        private void LateUpdate()
        {
            if (noclipEnabled)
            {
                Vector3 localPlayerPos = localPlayer.GetPosition();

                // Cache the last position
                lastPosition = position;

                // If the player's collider isn't enabled, the player has entered a seat so disable noclip
                // Radius is supposed to be 0f but the check sometimes fails when the player is inside a collider
                // Larger radius might return false positives if LocalPlayer layer is being used for something else in the world
                if (Physics.OverlapSphereNonAlloc(localPlayerPos, 100000f, playerCollider, 1024) < 1)
                {
                    SetNoclipEnabled(false);

                    return;
                }

                // Get the head rotation for movement direction
                Quaternion headRot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

                float deltaTime = Time.deltaTime;

                float vertical = Input.GetAxisRaw(AXIS_VERTICAL);

                float horizontal = Input.GetAxisRaw(AXIS_HORIZONTAL);

                if (vrEnabled)
                {
                    // Get magnitude of the movement input to apply shared exponential velocity
                    float exponentialInputMagnitude = Mathf.Pow(new Vector2(horizontal, vertical).magnitude, vrSpeedExponent);

                    // Get the vertical input from the right joystick
                    float worldVertical = Input.GetAxisRaw(AXIS_RIGHT_THUMBSTICK_VERTICAL);

                    // Get the maximum delta magnitude
                    float deltaTimeSpeed = deltaTime * speed;

                    // Create a delta vector for local X and Z axes
                    Vector3 xzDelta = deltaTimeSpeed * exponentialInputMagnitude * new Vector3(horizontal, 0f, vertical);

                    // Create a delta vector for world Y axis
                    Vector3 yWorldDelta = new Vector3(0f, Mathf.Pow(Mathf.Abs(worldVertical), vrSpeedExponent) * Mathf.Sign(worldVertical) * deltaTimeSpeed, 0f);

                    // Apply the position changes
                    position += headRot * xzDelta + yWorldDelta;
                }
                else
                {
                    float worldVertical = (Input.GetKey(downKey) ? 0f : 1f) - (Input.GetKey(upKey) ? 0f : 1f);

                    // Get the maximum delta magnitude
                    float deltaTimeMaxSpeed = deltaTime * (Input.GetKey(KeyCode.LeftShift) ? speed : speed * desktopSpeedFraction);

                    // Apply the position changes from vertical and horizontal inputs
                    position += headRot * (new Vector3(horizontal, 0f, vertical) * deltaTimeMaxSpeed);

                    // If allowed, apply vertical motion
                    if (desktopVerticalInput)
                    {
                        position += new Vector3(0f, deltaTimeMaxSpeed * worldVertical, 0f);
                    }
                }

                if (vrEnabled)
                {
                    VRCPlayerApi.TrackingData originData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);

                    // Get the playspace delta for applying to the final position
                    Vector3 playspaceDelta = originData.position - localPlayerPos;

                    // If the player is in VR, use the origin's rotation instead of player's rotation and align the room with spawn point
                    localPlayer.TeleportTo(position + playspaceDelta, originData.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
                }
                else
                {
                    localPlayer.TeleportTo(position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.Default, true);
                }

                // Force the player's velocity to zero to prevent the falling animation from triggering
                localPlayer.SetVelocity(Vector3.zero);
            }
        }
        #endregion

        #region Public Delayed Custom Event Methods
        /// <summary>
        /// Disables the switch priming if jump hasn't been pressed again within the threshold
        /// </summary>
        /// <remarks>
        /// Targeted by SendCustomEventDelayedSeconds
        /// </remarks>
        public void _DisablePriming()
        {
            switchPrimed = false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets the noclip mode enabled
        /// </summary>
        /// <param name="enabled"></param>
        private void SetNoclipEnabled(bool enabled)
        {
            noclipEnabled = enabled;

            localPlayer.Immobilize(enabled);

            if (enabled)
            {
                // Get the initial position of the player
                position = localPlayer.GetPosition();
            }
            else
            {
                // Apply the remainder velocity from last lerp to the player's velocity to allow them to fly after turning off noclip
                localPlayer.SetVelocity((position - lastPosition) / Time.deltaTime);
            }
        }
        #endregion

        #region VRChat Override Methods
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            // Only register inputs when noclip can be toggled with double jump and jump was just pressed
            if (toggleByDoubleJump && value)
            {
                if (noclipEnabled)
                {
                    // If noclip is enabled and switch primed, disable noclip
                    if (switchPrimed)
                    {
                        switchPrimed = false;

                        SetNoclipEnabled(false);
                    }
                    else // Prime the switch if it's not already
                    {
                        switchPrimed = true;

                        SendCustomEventDelayedSeconds(nameof(_DisablePriming), toggleThreshold);
                    }
                }
                else
                {
                    // If noclip is not enabled and switch is primed, enable noclip
                    if (switchPrimed)
                    {
                        switchPrimed = false;

                        SetNoclipEnabled(true);
                    }
                    else // Prime the switch if it's not already
                    {
                        switchPrimed = true;

                        SendCustomEventDelayedSeconds(nameof(_DisablePriming), toggleThreshold);
                    }
                }
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                // If the local player respawns, disable noclip to prevent confusion
                SetNoclipEnabled(false);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // If the local player just left and noclip is enabled, disable it to prevent any unnecessary errors when leaving the world
            if (noclipEnabled && !Utilities.IsValid(player))
            {
                noclipEnabled = false;
            }
        }
        #endregion

        #region Public API Methods
        /// <summary>
        /// Enables noclip
        /// </summary>
        [PublicAPI("Enables noclip")]
        public void _EnableNoclip()
        {
            SetNoclipEnabled(true);
        }

        /// <summary>
        /// Disables noclip
        /// </summary>
        [PublicAPI("Disables noclip")]
        public void _DisableNoclip()
        {
            SetNoclipEnabled(false);
        }

        /// <summary>
        /// Sets noclip enabled
        /// </summary>
        [PublicAPI("Sets noclip enabled")]
        public void _SetNoclipEnabled(bool enabled)
        {
            SetNoclipEnabled(enabled);
        }

        /// <summary>
        /// Sets the noclip max speed
        /// </summary>
        /// <param name="maxSpeed"></param>
        [PublicAPI("Sets the noclip max speed")]
        public void _SetMaxSpeed(float maxSpeed)
        {
            speed = maxSpeed;
        }
        #endregion
    }
}
