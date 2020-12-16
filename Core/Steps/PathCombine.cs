﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Parser;

namespace Reductech.EDR.Core.Steps
{
    /// <summary>
    /// Combine Paths
    /// </summary>
    public sealed class PathCombine : CompoundStep<StringStream>
    {
        /// <inheritdoc />
        public override async Task<Result<StringStream, IError>> Run(IStateMonad stateMonad, CancellationToken cancellationToken)
        {
            var pathsResult = await Paths.Run(stateMonad, cancellationToken);
            if (pathsResult.IsFailure) return pathsResult.ConvertFailure<StringStream>();

            var paths = new List<string>();

            await foreach (var stringStream in pathsResult.Value.WithCancellation(cancellationToken))
                paths.Add(await stringStream.GetStringAsync());

            if(!paths.Any())
                return new StringStream(stateMonad.FileSystemHelper.GetCurrentDirectory());

            if (!Path.IsPathFullyQualified(paths[0]))
                paths = paths.Prepend(stateMonad.FileSystemHelper.GetCurrentDirectory()).ToList();

            var result = Path.Combine(paths.ToArray());

            return new StringStream(result);
        }

        /// <summary>
        /// The paths to combine.
        /// </summary>
        [StepProperty(1)]
        [Required]
        public IStep<IAsyncEnumerable<StringStream>> Paths { get; set; } = null!;


        /// <inheritdoc />
        public override IStepFactory StepFactory => PathCombineStepFactory.Instance;
    }

    /// <summary>
    /// Combine Paths
    /// </summary>
    public class PathCombineStepFactory : SimpleStepFactory<PathCombine, StringStream>
    {
        private PathCombineStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static SimpleStepFactory<PathCombine, StringStream> Instance { get; } = new PathCombineStepFactory();
    }
}