﻿using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Reductech.EDR.Utilities.Processes.immutable;
using Reductech.EDR.Utilities.Processes.mutable.chain;

namespace Reductech.EDR.Utilities.Processes.mutable
{
    /// <summary>
    /// Base class of all processes.
    /// </summary>
    public abstract class Process
    {
        /// <summary>
        /// The type of this process, or a description of how the type is calculated.
        /// </summary>
        public abstract string GetReturnTypeInfo();

        /// <summary>
        /// The name of this process.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Executes this process. Should only be called if all conditions are met.
        /// </summary>
        /// <returns></returns>
        public abstract Result<IImmutableProcess<TOutput>> TryFreeze<TOutput>(IProcessSettings processSettings);

        /// <summary>
        /// Gets special requirements for the process.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<string> GetRequirements();

        /// <summary>
        /// Creates a immutableChain link builder.
        /// </summary>
        public abstract Result<ChainLinkBuilder<TInput, TFinal>> TryCreateChainLinkBuilder<TInput, TFinal>();

        /// <inheritdoc />
        public override string ToString()
        {
            return GetName();
        }

        /// <summary>
        /// Converts the result of freezing to the appropriate type.
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <typeparam name="TActual"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        protected Result<IImmutableProcess<TOutput>> TryConvertFreezeResult<TOutput, TActual>(Result<IImmutableProcess<TActual>> result)
        {
            if (result.IsFailure) return result.ConvertFailure<IImmutableProcess<TOutput>>();

            if (result.Value is IImmutableProcess<TOutput> process) return Result.Success(process);

            return Result.Failure<IImmutableProcess<TOutput>>($"{GetName()} has output type: '{typeof(TActual).Name}', not '{typeof(TOutput).Name}'.");
        }
    }


    internal class ProcessObjectTypeWrapper<T> : IImmutableProcess<object>
    {
        public ProcessObjectTypeWrapper(IImmutableProcess<T> process)
        {
            Process = process;
        }

        public IImmutableProcess<T> Process { get; }

        /// <inheritdoc />
        public async IAsyncEnumerable<IProcessOutput<object>> Execute()
        {
            await foreach (var line in Process.Execute())
            {
                if (line is IProcessOutput<object> l) yield return l;

#pragma warning disable CS8604 // Possible null reference argument. Value must have a value because the output type is success
                else if (line.OutputType == OutputType.Success) yield return ProcessOutput<object>.Success(line.Value);
#pragma warning restore CS8604 // Possible null reference argument.
                else yield return line.ConvertTo<object>();
            }
        }

    }
}