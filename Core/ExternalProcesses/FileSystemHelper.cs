﻿using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.ExternalProcesses
{
    /// <summary>
    /// Interacts with the file system in the normal way.
    /// </summary>
    public class FileSystemHelper : IFileSystemHelper
    {
        private FileSystemHelper() {}

        /// <summary>
        /// The instance.
        /// </summary>
        public static IFileSystemHelper Instance { get; } = new FileSystemHelper();

        /// <inheritdoc />
        public Result<Unit, IErrorBuilder> CreateDirectory(string path)
        {
            Result<Unit, IErrorBuilder> r;

            try
            {
                Directory.CreateDirectory(path);
                r = Unit.Default;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                r = new ErrorBuilder(e.Message, ErrorCode.ExternalProcessError);
            }

            return r;
        }

        /// <summary>
        /// Creates a new file and writes in the text.
        /// </summary>
        public async Task<Result<Unit, IErrorBuilder>> CreateFileAsync(string path, string text, CancellationToken ca)
        {
            Result<Unit, IErrorBuilder> r1;

            try
            {
                await using var sw = File.CreateText(path);
                await sw.WriteAsync(text.AsMemory(), ca);
                r1 = Unit.Default;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                r1 = new ErrorBuilder(e.Message, ErrorCode.ExternalProcessError);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return r1;
        }

        /// <inheritdoc />
        public bool DoesFileExist(string path) => File.Exists(path);

        /// <inheritdoc />
        public bool DoesDirectoryExist(string path) => Directory.Exists(path);

        /// <inheritdoc />
        public Result<Unit, IErrorBuilder> DeleteDirectory(string path, bool recursive)
        {
            try
            {
                Directory.Delete(path, recursive);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                return new ErrorBuilder(e, ErrorCode.ExternalProcessError);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return Unit.Default;
        }

        /// <inheritdoc />
        public Result<Unit, IErrorBuilder> DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                return new ErrorBuilder(e, ErrorCode.ExternalProcessError);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return Unit.Default;
        }

        /// <inheritdoc />
        public Result<Stream, IErrorBuilder> ReadFile(string path)
        {
            Result<Stream, IErrorBuilder> result;
            try
            {
                Stream fs = File.OpenRead(path);
                result = fs;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                result = new ErrorBuilder(e.Message, ErrorCode.ExternalProcessError);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return result;
        }

        /// <inheritdoc />
        public Result<Unit, IErrorBuilder> ExtractToDirectory(string sourceArchivePath, string destinationDirectoryPath, bool overwrite)
        {
            Maybe<IErrorBuilder> error;
            try
            {
                ZipFile.ExtractToDirectory(sourceArchivePath, destinationDirectoryPath, overwrite);
                error = Maybe<IErrorBuilder>.None;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                error = Maybe<IErrorBuilder>.From(new ErrorBuilder(e.Message, ErrorCode.ExternalProcessError));
            }
#pragma warning restore CA1031 // Do not catch general exception types

            if (error.HasValue)
                return Result.Failure<Unit, IErrorBuilder>(error.Value);

            return Unit.Default;
        }

        /// <inheritdoc />
        public async Task<Result<Unit, IErrorBuilder>> WriteFileAsync(string path, Stream stream, CancellationToken cancellationToken)
        {
            Maybe<IErrorBuilder> error;
            try
            {
                var fileStream = File.Create(path);
                await stream.CopyToAsync(fileStream, cancellationToken);
                fileStream.Close();
                error = Maybe<IErrorBuilder>.None;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                error = Maybe<IErrorBuilder>.From(new ErrorBuilder(e.Message, ErrorCode.ExternalProcessError));
            }
#pragma warning restore CA1031 // Do not catch general exception types

            if (error.HasValue)
                return Result.Failure<Unit, IErrorBuilder>(error.Value);

            return Unit.Default;
        }
    }
}