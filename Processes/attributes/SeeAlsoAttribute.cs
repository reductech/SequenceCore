﻿using System;

namespace Reductech.EDR.Processes.Attributes
{
    /// <summary>
    /// Indicates a related topic.
    /// </summary>
    public sealed class SeeAlsoAttribute : Attribute
    {
        /// <summary>
        /// Creates a new SeeAlsoAttribute
        /// </summary>
        /// <param name="seeAlso"></param>
        public SeeAlsoAttribute(string seeAlso)
        {
            SeeAlso = seeAlso;
        }

        /// <summary>
        /// Where to go to see something else.
        /// </summary>
        public string SeeAlso { get; }
    }
}