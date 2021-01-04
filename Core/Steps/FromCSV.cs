﻿using System.ComponentModel.DataAnnotations;
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
    /// Extracts entities from a CSV file.
    /// The same as FromConcordance but with different default values.
    /// </summary>
    [Alias("ConvertCSVToEntity")]
    public sealed class FromCSV : CompoundStep<Core.Sequence<Entity>>
    {
        /// <inheritdoc />
        public override async Task<Result<Core.Sequence<Entity>, IError>> Run(IStateMonad stateMonad,
            CancellationToken cancellationToken)
        {
            var result = await CSVReader.ReadCSV(
                stateMonad,
                Stream,
                Delimiter,
                CommentCharacter,
                QuoteCharacter,
                MultiValueDelimiter,
                new StepErrorLocation(this),
                cancellationToken);

            return result;
        }

        /// <summary>
        /// Stream containing the CSV data.
        /// </summary>
        [StepProperty(1)]
        [Required]
        public IStep<StringStream> Stream { get; set; } = null!;

        /// <summary>
        /// The delimiter to use to separate fields.
        /// </summary>
        [StepProperty(2)]
        [DefaultValueExplanation(",")]
        public IStep<StringStream> Delimiter { get; set; } = new StringConstant(new StringStream(","));

        /// <summary>
        /// The token to use to indicate comments.
        /// Must be a single character, or an empty string.
        /// If it is empty, then comments cannot be indicated
        /// </summary>
        [StepProperty(3)]
        [DefaultValueExplanation("#")]
        [SingleCharacter]
        public IStep<StringStream> CommentCharacter { get; set; } = new StringConstant(new StringStream("#"));

        /// <summary>
        /// The quote character to use.
        /// Should be a single character or an empty string.
        /// If it is empty then strings cannot be quoted.
        /// </summary>
        [StepProperty(4)]
        [DefaultValueExplanation("\"")]
        [SingleCharacter]
        public IStep<StringStream> QuoteCharacter { get; set; } = new StringConstant(new StringStream( "\""));


        /// <summary>
        /// The multi value delimiter character to use.
        /// Should be a single character or an empty string.
        /// If it is empty then fields cannot have multiple fields.
        /// </summary>
        [StepProperty(5)]
        [DefaultValueExplanation("")]
        [SingleCharacter]
        public IStep<StringStream> MultiValueDelimiter { get; set; } = new StringConstant(new StringStream(""));

        /// <inheritdoc />
        public override IStepFactory StepFactory => FromCSVStepFactory.Instance;
    }


    /// <summary>
    /// Extracts entities from a CSV Stream
    /// The same as FromConcordance but with different default values.
    /// </summary>
    public sealed class FromCSVStepFactory : SimpleStepFactory<FromCSV, Core.Sequence<Entity>>
    {
        private FromCSVStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static SimpleStepFactory<FromCSV, Core.Sequence<Entity>> Instance { get; } = new FromCSVStepFactory();
    }
}