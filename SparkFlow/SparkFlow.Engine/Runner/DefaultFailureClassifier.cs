using AdbLib.Exceptions;
using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public sealed class DefaultFailureClassifier : IFailureClassifier
{
    public RunnerFailure Classify(Exception ex)
    {
        if (ex is OperationCanceledException)
            throw ex;

        if (ex is AdbDeviceNotFoundException)
            return new RunnerFailure(
                RunnerFailureType.DeviceUnreachable,
                ex.Message,
                IsTransient: true,
                Code: "ADB:device_not_found");

        if (ex is AdbCommandException cmd)
        {
            if (cmd.Message.Contains("unauthorized"))
                return new RunnerFailure(
                    RunnerFailureType.AdbFailure,
                    "ADB unauthorized",
                    IsTransient: false,
                    Code: "ADB:unauthorized");

            return new RunnerFailure(
                RunnerFailureType.AdbFailure,
                cmd.Message,
                IsTransient: true,
                Code: "ADB:command_error");
        }

        if (ex is TimeoutException)
            return new RunnerFailure(
                RunnerFailureType.DeviceReadyTimeout,
                ex.Message,
                IsTransient: true,
                Code: "TIMEOUT");

        return new RunnerFailure(
            RunnerFailureType.Unknown,
            ex.Message,
            IsTransient: true,
            Code: "UNKNOWN");
    }
}