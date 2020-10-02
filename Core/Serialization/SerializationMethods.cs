﻿using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;

namespace Reductech.EDR.Core.Serialization
{
    /// <summary>
    /// Deserializes text to primitive types.
    /// </summary>
    public static class SerializationMethods
    {
        /// <summary>
        /// Serialize a constant freezable step.
        /// </summary>
        public static Result<string> TrySerializeConstant(ConstantFreezableStep cfp, bool quoteString, bool allowNewline)
        {
            if (cfp.Value.GetType().IsEnum)
                return cfp.Value.GetType().Name + "." + cfp.Value;


            if (cfp.Value is string s)
            {
                var newS = quoteString ? $"'{Escape(s)}'" : Escape(s);
                if (allowNewline || !newS.Contains('\n'))
                    return newS;
                return Result.Failure<string>("String constant contains newline");
            }
            return cfp.Value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Escape single quotes
        /// </summary>
        private static string Escape(string s) => s.Replace("'", "''");
    }
}