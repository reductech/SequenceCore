﻿using System.Collections.Generic;
using Reductech.EDR.Utilities.Processes.mutable;
using Reductech.EDR.Utilities.Processes.mutable.enumerations;
using Reductech.EDR.Utilities.Processes.output;

namespace Reductech.EDR.Utilities.Processes.immutable
{
    internal class LazyLoop : ImmutableProcess<Unit>
    {
        public LazyLoop(LazyEnumerationElements lazyEnumerationElements, Process @do, IProcessSettings processSettings)
        {
            LazyEnumerationElements = lazyEnumerationElements;
            Do = @do;
            ProcessSettings = processSettings;
        }

        /// <inheritdoc />
        public override string Name => ProcessNameHelper.GetLoopName(LazyEnumerationElements.Name, Do.GetName());

        /// <inheritdoc />
        public override IProcessConverter? ProcessConverter => null;

        /// <inheritdoc />
        public override async IAsyncEnumerable<IProcessOutput<Unit>> Execute()
        {
            
            EagerEnumerationElements? elements = null;
            var anyErrors = false;

            await foreach (var r in LazyEnumerationElements.Execute())
            {
                if (r.OutputType == OutputType.Success)
                {
                    elements = r.Value;
                }
                else
                {
                    if (r.OutputType == OutputType.Error)
                        anyErrors = true;

                    yield return r.ConvertTo<Unit>(); //These methods failing is expected so it should not produce an error
                }
            }

            if (!anyErrors)
            {
                if (elements == null)
                {
                    yield return ProcessOutput<Unit>.Error("Could not evaluate elements");
                }
                else
                {
                    var freezeResult = Loop.GetFreezeResultFromEagerElements(ProcessSettings, elements, Do);

                    if (freezeResult.IsSuccess)
                    {
                        if (freezeResult.Value is ImmutableProcess<Unit> unitProcess)
                        {
                            await foreach (var r in unitProcess.Execute())
                            {
                                yield return r;
                            }
                        }
                        else
                        {
                            yield return ProcessOutput<Unit>.Error(
                                $"Process '{freezeResult.Value.Name}' has result type {freezeResult.Value.ResultType.Name} but members of a loop should have result type void.");
                        }
                    }
                    else
                        foreach (var freezeError in freezeResult.Error)
                            yield return ProcessOutput<Unit>.Error(freezeError);
                }
            }
        }

        public IProcessSettings ProcessSettings { get; }

        public Process Do { get; }

        public LazyEnumerationElements LazyEnumerationElements { get; }
    }
}