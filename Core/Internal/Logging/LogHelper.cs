﻿using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Reductech.EDR.Core.Internal.Logging
{

/// <summary>
/// A log message.
/// These will be passed to the ILogger instance.
/// </summary>
public record LogMessage(
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        string Message,
        object? MessageParams,
        string? StepName,
        TextLocation? Location,
        IReadOnlyDictionary<string, object> SequenceInfo) : IEnumerable<KeyValuePair<string, object>
    >
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        yield return new KeyValuePair<string, object>(nameof(Message), Message);

        if (MessageParams is not null)
            yield return new KeyValuePair<string, object>(nameof(MessageParams), MessageParams);

        if (StepName is not null)
            yield return new KeyValuePair<string, object>(nameof(StepName), StepName);

        if (Location is not null)
            yield return new KeyValuePair<string, object>(nameof(Location), Location);

        yield return new KeyValuePair<string, object>(nameof(SequenceInfo), SequenceInfo);
    }

    /// <inheritdoc />
    public override string ToString() => Message;

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Contains methods for interacting with logging.
/// </summary>
public static class LogHelper
{
    /// <summary>
    /// Logs a message for the particular situation.
    /// Will use the resource to localize the message
    /// </summary>
    public static void LogSituation<T>(
        this ILogger logger,
        T situation,
        IStep? step,
        IStateMonad monad,
        params object?[] args) where T : LogSituationBase => LogSituation(
        logger,
        situation,
        step,
        monad.SequenceMetadata,
        args
    );

    /// <summary>
    /// Logs a message for the particular situation.
    /// Will use the resource to localize the message
    /// </summary>
    public static void LogSituation<T>(
        this ILogger logger,
        T situation,
        IStep? step,
        IReadOnlyDictionary<string, object> sequenceMetadata,
        object?[] args) where T : LogSituationBase
    {
        var logLevel = situation.LogLevel;

        if (logger.IsEnabled(logLevel))
        {
            var q = situation.GetLocalizedString(args);

            var logMessage = new LogMessage(
                q.message,
                q.properties,
                step?.Name,
                step?.TextLocation,
                sequenceMetadata
            );

            logger.Log(logLevel, default, logMessage, null, (x, _) => x.ToString());
        }
    }

    /// <summary>
    /// Logs a message that is not associated with a particular situation.
    /// Usually from an external process
    /// </summary>
    public static void LogMessage(
        this ILogger logger,
        LogLevel level,
        string message,
        IStep? step,
        IStateMonad monad)
    {
        var logMessage = new LogMessage(
            message,
            null,
            step?.Name,
            step?.TextLocation,
            monad.SequenceMetadata
        );

        logger.Log(level, default, logMessage, null, (x, _) => x.Message);
    }
}

}
