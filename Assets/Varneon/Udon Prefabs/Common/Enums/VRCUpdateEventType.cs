namespace Varneon.UdonPrefabs.Common.VRCEnums
{
    /// <summary>
    /// The types of update events that can be used in VRChat
    /// </summary>
    public enum VRCUpdateEventType
    {
        /// <summary>
        /// Unity Fixed Update
        /// </summary>
        FixedUpdate,
        /// <summary>
        /// Unity Update
        /// </summary>
        Update,
        /// <summary>
        /// Unity Late Update
        /// </summary>
        LateUpdate,
        /// <summary>
        /// VRChat's Post-Late Update
        /// </summary>
        PostLateUpdate
    }
}
