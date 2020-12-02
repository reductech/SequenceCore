﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Enums;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Serialization;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Steps
{
    /// <summary>
    /// Compares two items.
    /// </summary>
    public sealed class Compare<T> : CompoundStep<bool> where T : IComparable
    {
        /// <summary>
        /// The item to the left of the operator.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<T> Left { get; set; } = null!;

        /// <summary>
        /// The operator to use for comparison.
        /// </summary>
        [StepProperty]
        [Required]

        public IStep<CompareOperator> Operator { get; set; } = null!;

        /// <summary>
        /// The item to the right of the operator.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<T> Right { get; set; } = null!;


        /// <inheritdoc />
        public override async Task<Result<bool, IError>> Run(IStateMonad stateMonad,
            CancellationToken cancellationToken)
        {
            var result = await Left.Run(stateMonad, cancellationToken)
                .Compose(() => Operator.Run(stateMonad, cancellationToken), () => Right.Run(stateMonad, cancellationToken))
                .Bind(x => CompareItems(x.Item1, x.Item2, x.Item3).MapError(e=>e.WithLocation(this)));


            return result;
        }

        /// <inheritdoc />
        public override IStepFactory StepFactory => CompareStepFactory.Instance;

        private static Result<bool, IErrorBuilder> CompareItems(T item1, CompareOperator compareOperator, T item2)
        {
            return compareOperator switch
            {
                CompareOperator.Equals => item1.Equals(item2),
                CompareOperator.NotEquals => !item1.Equals(item2),
                CompareOperator.LessThan => item1.CompareTo(item2) < 0,
                CompareOperator.LessThanOrEqual => item1.CompareTo(item2) <= 0,
                CompareOperator.GreaterThan => item1.CompareTo(item2) > 0,
                CompareOperator.GreaterThanOrEqual => item1.CompareTo(item2) >= 0,
                _ => new ErrorBuilder($"Could not apply '{compareOperator}'", ErrorCode.UnexpectedEnumValue)
            };
        }

    }

    /// <summary>
    /// Compares two items.
    /// </summary>
    public sealed class CompareStepFactory : GenericStepFactory
    {
        private CompareStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static StepFactory Instance { get; } = new CompareStepFactory();

        /// <inheritdoc />
        public override Type StepType => typeof(Compare<>);

        /// <inheritdoc />
        public override IEnumerable<Type> EnumTypes => new[] { typeof(CompareOperator) };

        /// <inheritdoc />
        public override string OutputTypeExplanation => nameof(Boolean);

        /// <inheritdoc />
        protected override ITypeReference GetOutputTypeReference(ITypeReference memberTypeReference) => new ActualTypeReference(typeof(bool));

        /// <inheritdoc />
        protected override Result<ITypeReference, IError> GetMemberType(FreezableStepData freezableStepData,
            TypeResolver typeResolver)
        {
            var result = freezableStepData.GetStep(nameof(Compare<int>.Left), TypeName)
                .MapError(e=>e)
                .Bind(x => x.TryGetOutputTypeReference(typeResolver))
                .Compose(() => freezableStepData.GetStep(nameof(Compare<int>.Right), TypeName)
                    .Bind(x => x.TryGetOutputTypeReference(typeResolver))
                )
                .Map(x => new[] { x.Item1, x.Item2 })
                .Bind((x) => MultipleTypeReference.TryCreate(x, TypeName)
                .MapError(e=>e.WithLocation(this, freezableStepData)));

            return result;
        }

        /// <inheritdoc />
        public override IStepNameBuilder StepNameBuilder => new StepNameBuilderFromTemplate($"[{nameof(Compare<int>.Left)}] [{nameof(Compare<int>.Operator)}] [{nameof(Compare<int>.Right)}]");


        /// <inheritdoc />
        public override IStepSerializer Serializer => new StepSerializer(TypeName, new FixedStringComponent("("),
            new StepComponent(nameof(Compare<int>.Left)),
            SpaceComponent.Instance,
            new EnumDisplayComponent<CompareOperator>(nameof(Compare<int>.Operator)),
            SpaceComponent.Instance,
            new StepComponent(nameof(Compare<int>.Right)),
            new FixedStringComponent(")"));
    }
}