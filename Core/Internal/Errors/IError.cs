﻿using System.Collections.Generic;

namespace Reductech.EDR.Core.Internal.Errors
{
    /// <summary>
    /// One or more errors caused
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// The errors.
        /// </summary>
        IEnumerable<SingleError> GetAllErrors();

        /// <summary>
        /// The error as a string.
        /// </summary>
        string AsString { get; }
    }
}