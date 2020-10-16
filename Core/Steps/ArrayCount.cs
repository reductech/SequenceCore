﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;

namespace Reductech.EDR.Core.Steps
{
    /// <summary>
    /// Counts the elements in an array.
    /// </summary>
    public sealed class ArrayCount<T> : CompoundStep<int>
    {
        /// <summary>
        /// The array to count.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<List<T>> Array { get; set; } = null!;

        /// <inheritdoc />
        public override Result<int, IRunErrors> Run(StateMonad stateMonad) => Array.Run(stateMonad).Map(x => x.Count);

        /// <inheritdoc />
        public override IStepFactory StepFactory => ArrayCountStepFactory.Instance;
    }

    /// <summary>
    /// Counts the elements in an array.
    /// </summary>
    public sealed class ArrayCountStepFactory : GenericStepFactory
    {
        private ArrayCountStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static GenericStepFactory Instance { get; } = new ArrayCountStepFactory();

        /// <inheritdoc />
        public override Type StepType => typeof(ArrayCount<>);

        /// <inheritdoc />
        public override string OutputTypeExplanation => nameof(Int32);

        /// <inheritdoc />
        protected override ITypeReference GetOutputTypeReference(ITypeReference memberTypeReference) => new ActualTypeReference(typeof(int));

        /// <inheritdoc />
        protected override Result<ITypeReference> GetMemberType(FreezableStepData freezableStepData,
            TypeResolver typeResolver)
        {

            var r1 = freezableStepData.GetArgument(nameof(ArrayCount<object>.Array))
                .Bind(x => x.TryGetOutputTypeReference(typeResolver))
                .Bind(x => x.TryGetGenericTypeReference(typeResolver, 0))
                .Map(x => x as ITypeReference);

            return r1;
        }
    }
}