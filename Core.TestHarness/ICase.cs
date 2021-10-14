﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using AutoTheory;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Moq;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.ExternalProcesses;
using Reductech.EDR.Core.Internal;
using RestSharp;

namespace Reductech.EDR.Core.TestHarness
{

/// <summary>
/// A case that executes a step.
/// </summary>
public interface ICaseThatExecutes : IAsyncTestInstance, ICaseWithSetup
{
    Dictionary<VariableName, object> ExpectedFinalState { get; }

    public bool IgnoreFinalState { get; set; }

    public List<Action<IExternalContext>> FinalContextChecks { get; }

    /// <summary>
    /// The step factory store to use for running the step.
    /// The default step factory store will still be used for deserialization.
    /// </summary>
    Maybe<StepFactoryStore> StepFactoryStoreToUse { get; set; }

    LogLevel CheckLogLevel { get; set; }
}

public interface ICaseWithSetup
{
    ExternalContextSetupHelper ExternalContextSetupHelper { get; }
    RESTClientSetupHelper RESTClientSetupHelper { get; }
    public List<Action> FinalChecks { get; }
}

public sealed class RESTSetup
{
    public RESTSetup(
        string? baseUri,
        Expression<Func<IRestRequest, bool>> checkRequest,
        IRestResponse response)
    {
        BaseUri      = baseUri;
        CheckRequest = checkRequest;
        Response     = response;
    }

    public string? BaseUri { get; }
    public Expression<Func<IRestRequest, bool>> CheckRequest { get; }

    public IRestResponse Response { get; }

    public void SetupClient(Mock<IRestClient> client)
    {
        if (!string.IsNullOrWhiteSpace(BaseUri))
        {
            var uri = new Uri(BaseUri);
            client.SetupSet<Uri>(x => x.BaseUrl = uri);
        }

        client
            .Setup(
                s => s.ExecuteAsync(
                    It.Is(CheckRequest),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IRestRequest, CancellationToken>(
                (request, _) =>
                {
                    Response.Request = request;
                }
            )
            .ReturnsAsync(Response);
    }
}

public sealed class RESTClientSetupHelper
{
    private List<RESTSetup> Setups { get; } = new();

    public void AddHttpTestAction(RESTSetup restSetup) => Setups.Add(restSetup);

    public IRestClient GetRESTClient(MockRepository mockRepository, List<Action> finalChecks)
    {
        var client = mockRepository.Create<IRestClient>();

        foreach (var setup in Setups)
            setup.SetupClient(client);

        return client.Object;
    }
}

public sealed class ExternalContextSetupHelper
{
    public void AddSetupAction<T>(Action<Mock<T>, MockRepository> action)
        where T : class => _setupActions.Add(action);

    private readonly List<object> _setupActions = new();

    public T GetAndSetupMock<T>(MockRepository mockRepository)
        where T : class
    {
        var mock = mockRepository.Create<T>();

        foreach (var setupAction in _setupActions.OfType<Action<Mock<T>, MockRepository>>())
        {
            setupAction(mock, mockRepository);
        }

        return mock.Object;
    }

    public void AddContextObject(string name, object o) => _contextObjects.Add((name, o));

    public void AddContextMock(string name, Func<MockRepository, Mock> createFunc) =>
        _contextMocks.Add((name, createFunc));

    private readonly List<(string name, object context)> _contextObjects = new();

    private readonly List<(string name, Func<MockRepository, Mock> contextFunc)> _contextMocks =
        new();

    public IExternalContext GetExternalContext(MockRepository mockRepository)
    {
        var externalProcessRunner = GetAndSetupMock<IExternalProcessRunner>(mockRepository);
        var console               = GetAndSetupMock<IConsole>(mockRepository);

        var objects = new List<(string, object)>();

        objects.AddRange(_contextObjects);

        foreach (var (name, contextFunc) in _contextMocks)
        {
            var mock = contextFunc(mockRepository);
            objects.Add((name, mock.Object));
        }

        var externalContext = new ExternalContext(
            externalProcessRunner,
            console,
            objects.ToArray()
        );

        return externalContext;
    }
}

}
