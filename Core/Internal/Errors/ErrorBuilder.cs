﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Reductech.EDR.Core.Internal.Errors
{

/// <summary>
/// A single error builder
/// </summary>
public record ErrorBuilder(ErrorCodeBase ErrorCode, ErrorData Data) : IErrorBuilder
{
    /// <summary>
    /// Create a new SingleErrorBuilder
    /// </summary>
    public ErrorBuilder(ErrorCodeBase errorCode, params object?[] data) : this(
        errorCode,
        new ErrorData.ObjectData(data)
    )
    {
        Data      = new ErrorData.ObjectData(data);
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Create a new SingleErrorBuilder
    /// </summary>
    public ErrorBuilder(Exception exception, ErrorCodeBase errorCode) : this(
        errorCode,
        new ErrorData.ExceptionData(exception)
    )
    {
        Data      = new ErrorData.ExceptionData(exception);
        Timestamp = DateTime.Now;
    }

    /// <inheritdoc />
    public IError WithLocation(ErrorLocation location) => WithLocationSingle(location);

    /// <inheritdoc />
    public IEnumerable<ErrorBuilder> GetErrorBuilders()
    {
        yield return this;
    }

    /// <summary>
    /// The time the error was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Returns a SingleError with the given location.
    /// </summary>
    public SingleError WithLocationSingle(ErrorLocation location) => new(location, this);

    /// <inheritdoc />
    public string AsString => Data.AsString(ErrorCode);

    /// <summary>
    /// Equals method
    /// </summary>
    public bool Equals(IErrorBuilder? errorBuilder)
    {
        if (errorBuilder is ErrorBuilder seb)
            return Equals(seb);

        if (errorBuilder is ErrorBuilderList ebl && ebl.ErrorBuilders.Count == 1)
            return Equals(ebl.ErrorBuilders.Single());

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(ErrorCode, Data);
    }

    /// <inheritdoc />
    public virtual bool Equals(ErrorBuilder? other)
    {
        if (other is null)
            return false;

        return ErrorCode.Equals(other.ErrorCode) && Data.Equals(other.Data);
    }
}

/// <summary>
/// Error builder data
/// </summary>
public abstract record ErrorData
{
    /// <summary>
    /// An exception
    /// </summary>
    public record ExceptionData(Exception Exception) : ErrorData, IEquatable<ErrorData>
    {
        /// <inheritdoc />
        public override string AsString(ErrorCodeBase errorCode)
        {
            return Exception.Message;
        }

        /// <inheritdoc />
        public virtual bool Equals(ExceptionData? other)
        {
            if (other is null)
                return false;

            return Exception.Message.Equals(other.Exception.Message);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Exception.Message.GetHashCode();
        }
    }

    /// <summary>
    /// Data based on objects
    /// </summary>
    public record ObjectData(object?[] Arguments) : ErrorData, IEquatable<ErrorData>
    {
        /// <inheritdoc />
        public override string AsString(ErrorCodeBase errorCode)
        {
            return errorCode.GetFormattedMessage(Arguments);
        }

        /// <inheritdoc />
        public virtual bool Equals(ObjectData? other)
        {
            if (other is null)
                return false;

            return Arguments.Select(x => x?.ToString())
                .SequenceEqual(other.Arguments.Select(x => x?.ToString()));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Arguments.Length;
        }
    }

    /// <summary>
    /// This ErrorData as a string
    /// </summary>
    public abstract string AsString(ErrorCodeBase errorCode);
}

}
