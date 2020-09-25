﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CSharpFunctionalExtensions;

namespace Reductech.EDR.Processes.Internal
{
    /// <summary>
    /// A process factory that uses default values for most properties.
    /// </summary>
    public abstract class SimpleRunnableProcessFactory<TProcess, TOutput> : RunnableProcessFactory where TProcess : ICompoundRunnableProcess, new ()
    {
        /// <inheritdoc />
        public override Result<ITypeReference> TryGetOutputTypeReference(FreezableProcessData freezableProcessData) => Result.Success(ActualTypeReference.Create(typeof(TOutput)));

        /// <inheritdoc />
        public override IProcessNameBuilder ProcessNameBuilder => new DefaultProcessNameBuilder(TypeName);

        /// <inheritdoc />
        public override Type ProcessType => typeof(TProcess);


        /// <inheritdoc />
        public override IEnumerable<Type> EnumTypes => ImmutableArray<Type>.Empty;

        /// <inheritdoc />
        protected override Result<ICompoundRunnableProcess> TryCreateInstance(ProcessContext processContext, FreezableProcessData freezableProcessData) => new TProcess();

        /// <inheritdoc />
        public override string OutputTypeExplanation => typeof(TOutput).Name;
    }
}