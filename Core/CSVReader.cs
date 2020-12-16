﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using CsvHelper.Configuration;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Parser;

namespace Reductech.EDR.Core
{
    /// <summary>
    /// Helps read blocks
    /// </summary>
    public static class CSVReader
    {
        /// <summary>
        /// Reads a CSV stream to an entity stream based on all the input steps.
        /// </summary>
        /// <returns></returns>
        public static async Task<Result<IAsyncEnumerable<Entity>, IError>> ReadCSV(
            IStateMonad stateMonad,
            IStep<StringStream> stream,
            IStep<StringStream> delimiter,
            IStep<StringStream> commentCharacter,
            IStep<StringStream> quoteCharacter,
            IStep<StringStream> multiValueDelimiter,
            IErrorLocation errorLocation,
            CancellationToken cancellationToken)
        {
            var testStreamResult = await stream.Run(stateMonad, cancellationToken);
            if (testStreamResult.IsFailure) return testStreamResult.ConvertFailure<IAsyncEnumerable<Entity>>();

            var delimiterResult = await delimiter.Run(stateMonad, cancellationToken).Map(async x=> await x.GetStringAsync());
            if (delimiterResult.IsFailure) return delimiterResult.ConvertFailure<IAsyncEnumerable<Entity>>();

            var quoteResult = await TryConvertToChar(quoteCharacter, "Quote Character", stateMonad, errorLocation, cancellationToken);
            if (quoteResult.IsFailure) return quoteResult.ConvertFailure<IAsyncEnumerable<Entity>>();

            var commentResult = await TryConvertToChar(commentCharacter, "Comment Character", stateMonad, errorLocation, cancellationToken);
            if (commentResult.IsFailure) return commentResult.ConvertFailure<IAsyncEnumerable<Entity>>();

            var multiValueResult = await TryConvertToChar(multiValueDelimiter, "MultiValue Delimiter", stateMonad, errorLocation, cancellationToken);
            if (multiValueResult.IsFailure) return multiValueResult.ConvertFailure<IAsyncEnumerable<Entity>>();


            var asyncEnumerable = ReadCSV(testStreamResult.Value,
                delimiterResult.Value,
                quoteResult.Value,
                commentResult.Value,
                multiValueResult.Value,
                errorLocation);


            return Result.Success<IAsyncEnumerable<Entity>, IError>(asyncEnumerable);
        }

        /// <summary>
        /// Tries to convert a string step to a single nullable character.
        /// </summary>
        public static async Task<Result<char?, IError>> TryConvertToChar(IStep<StringStream> step,
            string propertyName,
            IStateMonad stateMonad,
            IErrorLocation errorLocation,
            CancellationToken cancellationToken)
        {
            var charResult = await step.Run(stateMonad, cancellationToken)
                .Map(async x=> await x.GetStringAsync());

            if (charResult.IsFailure) return charResult.ConvertFailure<char?>();

            char? resultChar;

            if (charResult.Value.Length == 0)
                resultChar = null;
            else if (charResult.Value.Length == 1)
                resultChar = charResult.Value.Single();
            else return new SingleError($"{propertyName} must be a single character.", ErrorCode.CSVError, errorLocation);

            return resultChar;
        }

        /// <summary>
        /// Creates a block that will produce records from the CSV file.
        /// </summary>
        public static async IAsyncEnumerable<Entity> ReadCSV(StringStream stringStream,
            string delimiter,
            char? quoteCharacter,
            char? commentCharacter,
            char? multiValueDelimiter,
            IErrorLocation location)
        {
            var (stream, encodingEnum) = stringStream.GetStream();

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                Encoding = encodingEnum.Convert(),
                SanitizeForInjection = false,
                DetectColumnCountChanges = false,
                ReadingExceptionOccurred = HandleException,
            };

            if (quoteCharacter.HasValue)
            {
                configuration.Quote = quoteCharacter.Value;
                configuration.IgnoreQuotes = false;
            }
            else configuration.IgnoreQuotes = true;

            if (commentCharacter.HasValue)
            {
                configuration.Comment = commentCharacter.Value;
                configuration.AllowComments = true;
            }
            else configuration.AllowComments = false;


            var textReader = new StreamReader(stream, encodingEnum.Convert());

            var reader = new CsvReader(textReader, configuration);


            await foreach (var row in reader.GetRecordsAsync<dynamic>())
            {
                var dict = row as IDictionary<string, object>;

                var entity = Entity.Create(dict!.Select(x=>(x.Key, x.Value)), multiValueDelimiter);
                yield return entity;
            }

            reader.Dispose();

            bool HandleException(CsvHelperException exception)
            {
                throw new ErrorException(new SingleError(exception, ErrorCode.CSVError, location));
            }
        }
    }
}
