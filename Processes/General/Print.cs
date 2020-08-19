﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Reductech.EDR.Processes.Attributes;

namespace Reductech.EDR.Processes.General
{

    /// <summary>
    /// Prints a value to the log.
    /// </summary>
    public sealed class Print<T> : CompoundRunnableProcess<Unit>
    {
        /// <inheritdoc />
        public override Result<Unit> Run(ProcessState processState)
        {
            var r = Value.Run(processState);
            if (r.IsFailure) return r.ConvertFailure<Unit>();

            processState.Logger.LogInformation(r.Value.ToString());

            return Result.Success(Unit.Default);
        }

        /// <summary>
        /// The Value to Print.
        /// </summary>
        [RunnableProcessProperty]
        [Required]
        public IRunnableProcess<T> Value { get; set; } = null!;

        /// <inheritdoc />
        public override RunnableProcessFactory RunnableProcessFactory => PrintProcessFactory.Instance;
    }

    /// <summary>
    /// Prints a value to the log.
    /// </summary>
    public sealed class PrintProcessFactory : GenericProcessFactory
    {
        private PrintProcessFactory() { }

        public static GenericProcessFactory Instance { get; } = new PrintProcessFactory();

        /// <inheritdoc />
        public override Type ProcessType => typeof(Print<>);

        /// <inheritdoc />
        protected override ITypeReference GetOutputTypeReference(ITypeReference memberTypeReference) => new ActualTypeReference(typeof(Unit));

        /// <inheritdoc />
        public override IProcessNameBuilder ProcessNameBuilder { get; } = new ProcessNameBuilderFromTemplate($"Print '[{nameof(Print<object>.Value)}]'");

        /// <inheritdoc />
        protected override Result<ITypeReference> GetMemberType(FreezableProcessData freezableProcessData) =>
            freezableProcessData.GetArgument(nameof(Print<object>.Value))
                .Bind(x => x.TryGetOutputTypeReference());


        /// <inheritdoc />
        public override IEnumerable<ICustomSerializer> CustomSerializers { get; } = new[]
        {
            new CustomSerializer($"Print [{nameof(Print<object>.Value)}]",
                new Regex(@"\A\s*Print\s+(?:(?<Value>(?:[\w\d\._]+))|'(?<Value>.+?)'|(?<Value><[\w\d\._]+>))\s*\Z",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new AnyDeserializerMapping("Value", nameof(Print<object>.Value))
            ),
        };
    }
}
