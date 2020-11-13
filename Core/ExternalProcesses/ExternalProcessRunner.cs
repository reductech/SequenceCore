﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Core.ExternalProcesses
{
    /// <summary>
    /// Basic external step runner.
    /// </summary>
    public class ExternalProcessRunner : IExternalProcessRunner
    {
        private ExternalProcessRunner() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static IExternalProcessRunner Instance { get; } = new ExternalProcessRunner();

        /// <inheritdoc />
        public Result<IExternalProcessReference, IErrorBuilder> StartExternalProcess(string processPath, IEnumerable<string> arguments, Encoding encoding)
        {
            if (!File.Exists(processPath))
                return new ErrorBuilder($"Could not find '{processPath}'", ErrorCode.ExternalProcessNotFound);

            var argumentString = string.Join(' ', arguments.Select(EncodeParameterArgument));
            var pProcess = new Process
            {
                StartInfo =
                {
                    FileName = processPath,
                    Arguments = argumentString,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden, //don't display a window
                    CreateNoWindow = true,
                    StandardErrorEncoding = encoding,
                    StandardOutputEncoding = encoding,
                    RedirectStandardInput = true
                }
            };

            var started = pProcess.Start();
            if(!started)
                return new ErrorBuilder($"Could not start '{processPath}'", ErrorCode.ExternalProcessError);

            AppDomain.CurrentDomain.ProcessExit += KillProcess;
            AppDomain.CurrentDomain.DomainUnload += KillProcess;

            var reference = new ExternalProcessReference(pProcess);

            return reference;

            void KillProcess(object? sender, EventArgs e)
            {
                if (!pProcess.HasExited)
                    pProcess.Kill(true);
            }
        }

        /// <inheritdoc />
        public async Task<Result<Unit, IErrorBuilder>> RunExternalProcess(string processPath, ILogger logger, IErrorHandler errorHandler, IEnumerable<string> arguments, Encoding encoding)
        {
            if (!File.Exists(processPath))
                return new ErrorBuilder($"Could not find '{processPath}'", ErrorCode.ExternalProcessNotFound);

            var argumentString = string.Join(' ', arguments.Select(EncodeParameterArgument));
            using var pProcess = new Process
            {
                StartInfo =
                {
                    FileName = processPath,
                    Arguments = argumentString,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden, //don't display a window
                    CreateNoWindow = true,
                    StandardErrorEncoding = encoding,
                    StandardOutputEncoding = encoding,
                }
            };

            var errors = new List<IErrorBuilder>();

            try
            {
                pProcess.Start();

                var multiStreamReader = new MultiStreamReader<(string line, StreamSource source)>(new IStreamReader<(string, StreamSource)>[]
                {
                new StreamReaderWithSource<StreamSource>(pProcess.StandardOutput, StreamSource.Output),
                new StreamReaderWithSource<StreamSource>(pProcess.StandardError, StreamSource.Error),
                });

                //Read the output one line at a time
                while (true)
                {
                    var line = await multiStreamReader.ReadLineAsync();
                    if (line == null) //We've reached the end of the file
                        break;
                    if (line.Value.source == StreamSource.Error)
                    {
                        var errorText = string.IsNullOrWhiteSpace(line.Value.line) ? "Unknown Error" : line.Value.line;

                        if (errorHandler.ShouldIgnoreError(errorText))
                            logger.LogWarning(line.Value.line);
                        else
                            errors.Add(new ErrorBuilder(errorText, ErrorCode.ExternalProcessError));

                    }
                    else
                        logger.LogInformation(line.Value.line);
                }

                pProcess.WaitForExit();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                errors.Add(new ErrorBuilder(e, ErrorCode.ExternalProcessError));
            }
#pragma warning restore CA1031 // Do not catch general exception types


            if (errors.Any())
            {
                var e = ErrorBuilderList.Combine(errors);
                return Result.Failure<Unit, IErrorBuilder>(e);
            }

            return Unit.Default;
        }

        private static readonly Regex BackslashRegex = new Regex(@"(\\*)" + "\"", RegexOptions.Compiled);
        private static readonly Regex TermWithSpaceRegex = new Regex(@"^(.*\s.*?)(\\*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static string EncodeParameterArgument(string original)
        {
            if (string.IsNullOrEmpty(original))
                return $"\"{original}\"";
            var value = BackslashRegex.Replace(original, @"$1\$0");

            value = TermWithSpaceRegex.Replace(value, "\"$1$2$2\"");
            return value;
        }
    }
}