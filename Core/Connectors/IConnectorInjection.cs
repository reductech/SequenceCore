﻿namespace Reductech.EDR.Core.Connectors;

/// <summary>
/// Injects necessary contexts for this connector
/// </summary>
public interface IConnectorInjection
{
    /// <summary>
    /// Tries to get external contexts to inject
    /// </summary>
    Result<IReadOnlyCollection<(string Name, object Context)>, IErrorBuilder>
        TryGetInjectedContexts();
}
