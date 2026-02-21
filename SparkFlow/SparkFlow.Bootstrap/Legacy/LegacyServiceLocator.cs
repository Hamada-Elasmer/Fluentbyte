/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/LegacyServiceLocator.cs
 * Purpose: Core component: LegacyServiceLocator.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Concurrent;

namespace SparkFlow.Core.Services
{
    /// <summary>
    /// Simple service locator.
    /// NOTE: Exists for compatibility and legacy access.
    /// DI should be preferred where possible.
    /// </summary>
    public sealed class LegacyServiceLocator
    {
        private static readonly Lazy<LegacyServiceLocator> _instance =
            new(() => new LegacyServiceLocator());

        public static LegacyServiceLocator Instance => _instance.Value;

        private readonly ConcurrentDictionary<Type, object> _services = new();

        private LegacyServiceLocator()
        {
        }

        public void Register<TService>(TService service) where TService : class
        {
            var type = typeof(TService);
            _services[type] = service;
        }

        public TService Get<TService>() where TService : class
        {
            var type = typeof(TService);

            if (_services.TryGetValue(type, out var service))
            {
                if (service is TService typed)
                    return typed;

                throw new InvalidCastException(
                    $"Service {type.Name} is registered but cannot be cast.");
            }

            throw new KeyNotFoundException(
                $"Service {type.Name} is not registered.");
        }
    }
}