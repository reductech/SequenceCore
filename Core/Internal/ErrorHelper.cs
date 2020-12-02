﻿using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Core.Internal
{
    /// <summary>
    /// Easy access to common errors.
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// The error that should be returned when a step is requested which does not exist.
        /// </summary>
        public static IErrorBuilder MissingStepError(string stepName) => new ErrorBuilder($"The step '{stepName}' does not exist", ErrorCode.StepDoesNotExist);

        /// <summary>
        /// The error that should be returned when a parameter is missing.
        /// </summary>
        public static IErrorBuilder MissingParameterError(string parameterName, string stepType) => new ErrorBuilder($"Missing Parameter '{parameterName}' in '{stepType}'", ErrorCode.MissingParameter);


        public static IErrorBuilder WrongParameterTypeError(string propertyName, MemberType realType, MemberType expectedType) =>
            new ErrorBuilder($"{propertyName} was a {realType}, not a {expectedType}", ErrorCode.WrongArgumentType);

        /// <summary>
        /// The error that should be returned when a parameter is missing.
        /// </summary>
        public static IErrorBuilder UnexpectedParameterError(string parameterName, string stepType) => 
            new ErrorBuilder($"Unexpected Parameter '{parameterName}' in '{stepType}'", ErrorCode.UnexpectedParameter);

        /// <summary>
        /// The error that should be returned when there is a duplicate parameter.
        /// </summary>
        public static IErrorBuilder DuplicateParameterError(string parameterName) => new ErrorBuilder($"Duplicate Parameter '{parameterName}'", ErrorCode.DuplicateParameter);
    }
}