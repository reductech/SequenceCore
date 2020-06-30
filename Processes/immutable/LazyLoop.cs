﻿using System.Collections.Generic;
using Reductech.EDR.Processes.Mutable;
using Reductech.EDR.Processes.Mutable.Enumerations;
using Reductech.EDR.Processes.Output;

namespace Reductech.EDR.Processes.Immutable
{
    internal class LazyLoop : ImmutableProcess<Unit>
    {
        public LazyLoop(ILazyEnumerationElements lazyEnumerationElements, Process @do, IProcessSettings processSettings)
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
            IEagerEnumerationElements? elements = null;
            var anyErrors = false;

            await foreach (var r in LazyEnumerationElements.Execute())
            {
                if (r.OutputType == OutputType.Success)
                    elements = r.Value;
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
                    yield return ProcessOutput<Unit>.Error("Could not evaluate elements");
                else
                {
                    var freezeResult = Loop.GetFreezeResultFromEagerElements(ProcessSettings, elements, Do);

                    if (freezeResult.IsSuccess)
                    {
                        await foreach (var r in freezeResult.Value.Execute())
                            yield return r;
                    }
                    else
                        yield return ProcessOutput<Unit>.Error(freezeResult.Error);
                }
            }
        }

        public IProcessSettings ProcessSettings { get; }

        public Process Do { get; }

        public ILazyEnumerationElements LazyEnumerationElements { get; }
    }
}
