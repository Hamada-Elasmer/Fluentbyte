using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Abstractions.Abstractions
{
    /// <summary>
    /// Represents a single execution run context.
    /// This context is created by the platform runner and passed down
    /// to all game lifecycle steps, health checks, and tasks.
    /// </summary>
    public interface IRunContext
    {
        /// <summary>
        /// Unique identifier for the current run cycle.
        /// Changes on every global run or auto-restart iteration.
        /// </summary>
        Guid RunId { get; }

        /// <summary>
        /// The account profile being executed.
        /// </summary>
        AccountProfile Profile { get; }

        /// <summary>
        /// The game identifier selected for this profile.
        /// </summary>
        string GameId { get; }

        /// <summary>
        /// Active device session bound to the profile's ADB serial.
        /// </summary>
        IDeviceSession Device { get; }

        /// <summary>
        /// Cancellation token that represents the lifetime of this run.
        /// All long-running operations must observe this token.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}
