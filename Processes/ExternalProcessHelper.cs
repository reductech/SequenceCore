﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Reductech.EDR.Utilities.Processes.output;

namespace Reductech.EDR.Utilities.Processes
{
    /// <summary>
    /// Runs external processes.
    /// </summary>
    public static class ExternalProcessHelper
    {
        private enum Source
        {
            Output,
            Error
        }

        /// <summary>
        /// Runs an external process and returns the output and errors
        /// </summary>
        /// <param name="processPath">The path to the process</param>
        /// <param name="arguments">The arguments to provide to the process. These will all be escaped</param>
        /// <returns>The output of the process</returns>
        public static async IAsyncEnumerable<IProcessOutput<Unit>> RunExternalProcess(string processPath, IEnumerable<string> arguments)
        {
            if (!File.Exists(processPath))
            {
                yield return ProcessOutput<Unit>.Error($"Could not find '{processPath}'");
                yield break;
            }

            var argumentString = string.Join(' ', arguments.Select(EncodeParameterArgument));
            using var pProcess = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = processPath,
                    Arguments = argumentString,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, //don't display a window
                    CreateNoWindow = true,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            pProcess.Start();

            var multiStreamReader = new MultiStreamReader<(string line, Source source)>(new IStreamReader<(string, Source)>[]
            {
                new StreamReaderWithSource<Source>(pProcess.StandardOutput, Source.Output),
                new StreamReaderWithSource<Source>(pProcess.StandardError, Source.Error),
            });

            //Read the output one line at a time
            while (true)
            {
                var line = await multiStreamReader.ReadLineAsync();
                if (line == null) //We've reached the end of the file
                    break;
                if (line.Value.source == Source.Error)
                {
                    var errorText = string.IsNullOrWhiteSpace(line.Value.line) ? "Unknown Error" : line.Value.line;
                    yield return ProcessOutput<Unit>.Error(errorText);
                }
                yield return ProcessOutput<Unit>.Message(line.Value.line);
            }

            pProcess.WaitForExit();
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