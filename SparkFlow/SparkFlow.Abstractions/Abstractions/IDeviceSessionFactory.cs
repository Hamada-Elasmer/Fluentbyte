namespace SparkFlow.Abstractions.Abstractions
{
    /// <summary>
    /// Factory responsible for creating exclusive device sessions
    /// bound to a specific ADB serial.
    /// 
    /// The factory guarantees:
    /// - One active session per ADB serial at a time.
    /// - Proper acquisition and release semantics.
    /// </summary>
    public interface IDeviceSessionFactory
    {
        /// <summary>
        /// Opens an exclusive device session for the given ADB serial.
        /// The call must block or fail if the device is already in use.
        /// </summary>
        Task<IDeviceSession> OpenSessionAsync(
            string adbSerial,
            CancellationToken cancellationToken = default);
    }
}
