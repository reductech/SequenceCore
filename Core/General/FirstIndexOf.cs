﻿using System;
using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.General
{
    /// <summary>
    /// Gets the first instance of substring in a string.
    /// </summary>
    public sealed class FirstIndexOf : CompoundStep<int>
    {
        /// <summary>
        /// The string to check.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<string> String { get; set; } = null!;

        /// <summary>
        /// The substring to find.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<string> SubString { get; set; } = null!;

        /// <inheritdoc />
        public override Result<int, IRunErrors> Run(StateMonad stateMonad)
        {
            var str = String.Run(stateMonad);
            if (str.IsFailure) return str.ConvertFailure<int>();

            var subString = SubString.Run(stateMonad);
            if (subString.IsFailure) return subString.ConvertFailure<int>();


            return str.Value.IndexOf(subString.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override IStepFactory StepFactory => FirstIndexOfStepFactory.Instance;
    }


    /// <summary>
    /// Gets the first instance of substring in a string.
    /// </summary>
    public sealed class FirstIndexOfStepFactory : SimpleStepFactory<FirstIndexOf, int>
    {
        private FirstIndexOfStepFactory() { }

        /// <summary>
        /// The instance
        /// </summary>
        public static SimpleStepFactory<FirstIndexOf, int> Instance { get; } = new FirstIndexOfStepFactory();

        /// <inheritdoc />
        public override IStepNameBuilder StepNameBuilder => new StepNameBuilderFromTemplate($"First index of '[{nameof(FirstIndexOf.SubString)}]' in '[{nameof(FirstIndexOf.String)}]'");
    }


}