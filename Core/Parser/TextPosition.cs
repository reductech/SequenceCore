﻿using System;
using Antlr4.Runtime;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Parser
{
    /// <summary>
    /// The position in a sequence text.
    /// </summary>
    public sealed class TextPosition : IErrorLocation
    {
        public TextPosition(IToken token) : this(token.Text, token.StartIndex, token.StopIndex) { }

        public TextPosition(ParserRuleContext parserRuleContext) : this(
            parserRuleContext.GetText(),
            parserRuleContext.Start.StartIndex,
            parserRuleContext.Stop.StopIndex)
        { }

        public TextPosition(string text, int startIndex, int stopIndex)
        {
            Text = text;
            StartIndex = startIndex;
            StopIndex = stopIndex;
        }

        /// <summary>
        /// The text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The start index
        /// </summary>
        public int StartIndex { get; }
        /// <summary>
        /// The stop index
        /// </summary>
        public int StopIndex { get; }

        /// <inheritdoc />
        public override string ToString() => (Text, StartIndex, StopIndex).ToString();


        /// <inheritdoc />
        public bool Equals(IErrorLocation? other)
        {
            return other is TextPosition tp &&
                   Text == tp.Text && StartIndex == tp.StartIndex && StopIndex == tp.StopIndex;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is TextPosition other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Text, StartIndex, StopIndex);

        /// <inheritdoc />
        public string AsString { get; }
    }
}