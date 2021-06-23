﻿using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.ConnectorManagement.Base;

namespace Reductech.EDR.Core.Connectors
{

/// <summary>
/// Provides Core-specific extension methods for ConnectorData
/// </summary>
public static class ConnectorDataExtensions
{
    /// <summary>
    /// Get injections for this connector
    /// </summary>
    public static IReadOnlyCollection<IConnectorInjection> GetConnectorInjections(
        this ConnectorData connectorData) => connectorData.Assembly?.GetTypes()
        .Where(x => !x.IsAbstract)
        .Where(x => typeof(IConnectorInjection).IsAssignableFrom(x))
        .Select(Activator.CreateInstance)
        .Cast<IConnectorInjection>()
        .ToArray() ?? ArraySegment<IConnectorInjection>.Empty;

    /// <summary>
    /// Tries to get contexts injected by connectors
    /// </summary>
    public static Result<(string Name, object Context)[], IErrorBuilder> TryGetInjectedContexts(
        this ConnectorData connectorData,
        SCLSettings settings)
    {
        var contexts = connectorData.GetConnectorInjections()
            .Select(x => x.TryGetInjectedContexts(settings))
            .Combine(x => x.SelectMany(y => y).ToArray(), ErrorBuilderList.Combine);

        return contexts;
    }
}

}
