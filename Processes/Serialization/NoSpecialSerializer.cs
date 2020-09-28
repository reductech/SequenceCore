﻿using CSharpFunctionalExtensions;
using Reductech.EDR.Processes.Internal;

namespace Reductech.EDR.Processes.Serialization
{
    /// <summary>
    /// Serializer for processes that should not use short form serialization.
    /// </summary>
    public sealed class NoSpecialSerializer : IStepSerializer
    {
        private NoSpecialSerializer() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static IStepSerializer Instance { get; } = new NoSpecialSerializer();

        /// <inheritdoc />
        public Result<string> TrySerialize(FreezableStepData data) => Result.Failure<string>("This step does not support special serialization");
    }
}