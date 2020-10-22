﻿using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.Steps
{
    /// <summary>
    /// Deletes a file or folder from the file system.
    /// </summary>
    public class DeleteItem : CompoundStep<Unit>
    {
        /// <inheritdoc />
        public override async Task<Result<Unit, IError>>  Run(StateMonad stateMonad, CancellationToken cancellationToken)
        {
            var pathResult = await Path.Run(stateMonad, cancellationToken);
            if (pathResult.IsFailure)
                return pathResult.ConvertFailure<Unit>();

            var path = pathResult.Value;


            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                stateMonad.Logger.LogInformation($"Directory '{path}' Deleted.");
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                stateMonad.Logger.LogInformation($"File '{path}' Deleted.");
            }
            else
                stateMonad.Logger.LogInformation($"Item '{path}' did not exist.");

            return Unit.Default;

        }

        /// <summary>
        /// The path to the file or folder to delete.
        /// </summary>
        [StepProperty]
        [Required]
        public IStep<string> Path { get; set; } = null!;


        /// <inheritdoc />
        public override IStepFactory StepFactory => DeleteItemStepFactory.Instance;
    }


    /// <summary>
    /// Deletes a file or folder from the file system.
    /// </summary>
    public class DeleteItemStepFactory : SimpleStepFactory<DeleteItem, Unit>
    {
        private DeleteItemStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static SimpleStepFactory<DeleteItem, Unit> Instance { get; } = new DeleteItemStepFactory();
    }
}
