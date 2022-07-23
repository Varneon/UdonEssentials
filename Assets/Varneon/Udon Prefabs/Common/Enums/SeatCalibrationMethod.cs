namespace Varneon.UdonPrefabs.Common.SeatEnums
{
    /// <summary>
    /// Method for calibrating a seat
    /// </summary>
    public enum SeatCalibrationMethod
    {
        /// <summary>
        /// No calibration
        /// </summary>
        None,

        /// <summary>
        /// Align player's head's tracking data position with a transform's position
        /// </summary>
        Head,

        /// <summary>
        /// Align player's hip bone's position with a transforms's position
        /// </summary>
        Hips
    }
}
