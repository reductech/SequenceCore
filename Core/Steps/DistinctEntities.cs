﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Entity = Reductech.EDR.Core.Entities.Entity;

namespace Reductech.EDR.Core.Steps
{
    /// <summary>
    /// Removes duplicate entities.
    /// </summary>
    public sealed class DistinctEntities : CompoundStep<EntityStream>
    {
        /// <inheritdoc />
        public override async Task<Result<EntityStream, IError>> Run(StateMonad stateMonad, CancellationToken cancellationToken)
        {
            var entityStreamResult = await EntityStream.Run(stateMonad, cancellationToken);
            if (entityStreamResult.IsFailure) return entityStreamResult.ConvertFailure<EntityStream>();

            var caseSensitiveResult = await CaseSensitive.Run(stateMonad, cancellationToken);
            if (caseSensitiveResult.IsFailure) return caseSensitiveResult.ConvertFailure<EntityStream>();




            //var entities = await entityStreamResult.Value.TryGetResultsAsync(cancellationToken);

            //thropw
            throw new NotImplementedException();
        }

        /// <summary>
        /// The entities to sort
        /// </summary>
        [StepProperty(Order = 1)]
        [Required]
        public IStep<EntityStream> EntityStream { get; set; } = null!;

        /// <summary>
        /// A function that gets the key to distinct by from the variable &lt;Entity&gt;
        /// To distinct by multiple properties, concatenate several keys
        /// </summary>
        [StepProperty(Order = 2)]
        [Required]
        public IStep<string> DistinctBy { get; set; } = null!;

        /// <summary>
        /// Whether comparisons should be case sensitive.
        /// </summary>
        [StepProperty(Order = 2)]
        [DefaultValueExplanation("true")]
        public IStep<bool> CaseSensitive { get; set; } = new Constant<bool>(true);
        /// <inheritdoc />
        public override IStepFactory StepFactory => DirectoryExistsStepFactory.Instance;
    }

    /// <summary>
    /// Removes duplicate entities.
    /// </summary>
    public sealed class DistinctEntitiesStepFactory : SimpleStepFactory<DistinctEntities, EntityStream>
    {
        private DistinctEntitiesStepFactory() {}

        /// <summary>
        /// The instance.
        /// </summary>
        public static SimpleStepFactory<DistinctEntities, EntityStream> Instance { get; } = new DistinctEntitiesStepFactory();

        /// <inheritdoc />
        public override IEnumerable<(VariableName VariableName, ITypeReference typeReference)> FixedVariablesSet
        {
            get
            {
                yield return (VariableName.Entity, new ActualTypeReference(typeof(Entity)));
            }
        }
    }
}