﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Moq;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Xunit;

namespace Reductech.EDR.Core.Tests
{

[AutoTheory.UseTestOutputHelper]
public partial class StateMonadTests
{
    public interface IDoubleDisposable : IStateDisposable, IDisposable { }

    [Fact]
    public async Task StateMonadShouldDisposeVariablesThatItRemoves()
    {
        var repo = new MockRepository(MockBehavior.Strict);

        var sd = repo.Create<IDoubleDisposable>();

        var monad = CreateMonad(repo);

        sd.Setup(x => x.DisposeAsync(monad)).Returns(Task.CompletedTask);
        sd.Setup(x => x.Dispose());

        await monad.SetVariableAsync(
            new VariableName("V"),
            sd.Object,
            false,
            null,
            CancellationToken.None
        );

        await monad.RemoveVariableAsync(new VariableName("V"), true, null);

        repo.VerifyAll();
    }

    [Fact]
    public async Task StateMonadShouldDisposeVariablesWhenItIsDisposed()
    {
        var repo = new MockRepository(MockBehavior.Strict);

        var sd = repo.Create<IDoubleDisposable>();

        var monad = CreateMonad(repo);

        // ReSharper disable once AccessToDisposedClosure
        sd.Setup(x => x.DisposeAsync(monad)).Returns(Task.CompletedTask);
        sd.Setup(x => x.Dispose());

        await monad.SetVariableAsync(
            new VariableName("V"),
            sd.Object,
            false,
            null,
            CancellationToken.None
        );

        await monad.DisposeAsync();

        repo.VerifyAll();
    }

    [Fact]
    public async Task ScopedStateMonadShouldDisposeVariablesThatItRemoves()
    {
        var repo = new MockRepository(MockBehavior.Strict);

        var sd = repo.Create<IDoubleDisposable>();

        var monad1 = CreateMonad(repo);

        var scopedMonad = new ScopedStateMonad(
            monad1,
            ImmutableDictionary<VariableName, object>.Empty,
            Maybe<VariableName>.None
        );

        sd.Setup(x => x.DisposeAsync(scopedMonad)).Returns(Task.CompletedTask);
        sd.Setup(x => x.Dispose());

        await scopedMonad.SetVariableAsync(
            new VariableName("V"),
            sd.Object,
            false,
            null,
            CancellationToken.None
        );

        await scopedMonad.RemoveVariableAsync(new VariableName("V"), true, null);

        repo.VerifyAll();
    }

    [Fact]
    public async Task ScopedStateMonadShouldDisposeVariablesWhenItIsDisposed()
    {
        var repo = new MockRepository(MockBehavior.Strict);

        var sd = repo.Create<IDoubleDisposable>();

        var monad1 = CreateMonad(repo);

        var scopedMonad = new ScopedStateMonad(
            monad1,
            ImmutableDictionary<VariableName, object>.Empty,
            Maybe<VariableName>.None
        );

        // ReSharper disable once AccessToDisposedClosure
        sd.Setup(x => x.DisposeAsync(scopedMonad)).Returns(Task.CompletedTask);
        sd.Setup(x => x.Dispose());

        await scopedMonad.SetVariableAsync(
            new VariableName("V"),
            sd.Object,
            false,
            null,
            CancellationToken.None
        );

        await scopedMonad.DisposeAsync();

        repo.VerifyAll();
    }

    private static IStateMonad CreateMonad(MockRepository repo)
    {
        return
            new StateMonad(
                repo.OneOf<ILogger>(),
                StepFactoryStore.Create(),
                repo.OneOf<IExternalContext>(),
                repo.OneOf<IReadOnlyDictionary<string, object>>()
            );
    }
}

}
