using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Engine.Runner
{
    /// <summary>
    /// Default implementation of IRunContext.
    /// Created by the platform runner and shared across
    /// all execution layers for the lifetime of a single run.
    /// </summary>
    internal sealed class RunContext : IRunContext
    {
        public RunContext(
            Guid runId,
            AccountProfile profile,
            string gameId,
            IDeviceSession device,
            CancellationToken cancellationToken)
        {
            if (runId == Guid.Empty)
                throw new ArgumentException("RunId cannot be empty.", nameof(runId));

            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Device = device ?? throw new ArgumentNullException(nameof(device));

            GameId = string.IsNullOrWhiteSpace(gameId)
                ? throw new ArgumentException(nameof(gameId))
                : gameId;

            RunId = runId;
            CancellationToken = cancellationToken;
        }

        public Guid RunId { get; }

        public AccountProfile Profile { get; }

        public string GameId { get; }

        public IDeviceSession Device { get; }

        public CancellationToken CancellationToken { get; }
    }
}
