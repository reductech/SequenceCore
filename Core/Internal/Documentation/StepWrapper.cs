﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Namotion.Reflection;
using Reductech.EDR.Core.Attributes;

namespace Reductech.EDR.Core.Internal.Documentation
{

/// <summary>
/// A wrapper for this documented object.
/// </summary>
public class StepWrapper : IDocumentedStep
{
    /// <summary>
    /// Creates a new StepWrapper.
    /// </summary>
    public StepWrapper(IGrouping<IStepFactory, string> grouping)
    {
        Factory               = grouping.Key;
        DocumentationCategory = grouping.Key.Category;

        RelevantProperties = grouping.Key.ParameterDictionary.Values.Distinct()
            .Select(
                property => (
                    property, attribute: property.GetCustomAttribute<StepPropertyBaseAttribute>())
            )
            .Where(x => x.attribute != null)
            // ReSharper disable once ConstantConditionalAccessQualifier
            .OrderBy(x => x.attribute!.Order)
            .ThenBy(x => x.property.Name)
            .Select(x => x.property)
            .ToList();

        Parameters =
            RelevantProperties.Select(GetPropertyWrapper).ToList();

        Requirements = grouping.Key.Requirements.Select(x => $"Requires {x}").ToList();

        TypeDetails = grouping.Key.OutputTypeExplanation;

        AllNames = grouping.ToList();

        Examples = Factory.Examples.ToList();
    }

    private static PropertyWrapper GetPropertyWrapper(PropertyInfo propertyInfo) =>
        new(propertyInfo, null);

    private IStepFactory Factory { get; }

    /// <inheritdoc />
    public string DocumentationCategory { get; }

    /// <inheritdoc />
    public string Name =>
        Factory.TypeName; // TypeNameHelper.GetHumanReadableTypeName(Factory.StepType);

    /// <inheritdoc />
    public string FileName => Factory.TypeName + ".md";

    /// <inheritdoc />
    public string Summary => Factory.Summary;

    /// <inheritdoc />
    public string? TypeDetails { get; }

    /// <inheritdoc />
    public IEnumerable<string> Requirements { get; }

    /// <summary>
    /// Properties of this step.
    /// </summary>
    protected IEnumerable<PropertyInfo> RelevantProperties { get; }

    /// <inheritdoc />
    public IEnumerable<IParameter> Parameters { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> AllNames { get; }

    /// <inheritdoc />
    public IReadOnlyList<SCLExampleAttribute> Examples { get; }

    /// <summary>
    /// The wrapper for a property.
    /// </summary>
    protected class PropertyWrapper : IParameter
    {
        private readonly PropertyInfo _propertyInfo;

        /// <summary>
        /// Creates a new PropertyWrapper.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="defaultValueString"></param>
        public PropertyWrapper(PropertyInfo propertyInfo, string? defaultValueString)
        {
            _propertyInfo = propertyInfo;

            Required = _propertyInfo.GetCustomAttributes<RequiredAttribute>().Any()
                    && defaultValueString == null;

            var explanation = Required
                ? null
                : //Required properties should not have a default value
                propertyInfo.GetCustomAttributes<DefaultValueExplanationAttribute>()
                    .FirstOrDefault()
                    ?.Explanation;

            var extraFields = new Dictionary<string, string>();

            var dvs = explanation == null
                ? defaultValueString
                : $"{explanation}";

            if (!string.IsNullOrWhiteSpace(dvs))
                extraFields.Add("Default Value", dvs);

            AddFieldFromAttribute<ExampleAttribute>(
                "Example",
                extraFields,
                propertyInfo,
                x => x.Example
            );

            AddFieldFromAttribute<RequiredVersionAttribute>(
                "Requirements",
                extraFields,
                propertyInfo,
                x => x.Text
            );

            AddFieldFromAttribute<DocumentationURLAttribute>(
                "URL",
                extraFields,
                propertyInfo,
                x => $"[{propertyInfo.Name}]({x.DocumentationURL})"
            );

            AddFieldFromAttribute<RecommendedRangeAttribute>(
                "Recommended Range",
                extraFields,
                propertyInfo,
                x => x.RecommendedRange
            );

            AddFieldFromAttribute<RecommendedValueAttribute>(
                "Recommended Value",
                extraFields,
                propertyInfo,
                x => x.RecommendedValue
            );

            AddFieldFromAttribute<AllowedRangeAttribute>(
                "Allowed Range",
                extraFields,
                propertyInfo,
                x => x.AllowedRangeValue
            );

            AddFieldFromAttribute<SeeAlsoAttribute>(
                "See Also",
                extraFields,
                propertyInfo,
                x => x.SeeAlso
            );

            AddFieldFromAttribute<ValueDelimiterAttribute>(
                "Value Delimiter",
                extraFields,
                propertyInfo,
                x => x.Delimiter
            );

            ExtraFields = extraFields;

            static Type TryGetNested(Type t) => t.IsGenericType ? t.GetGenericArguments()[0] : t;

            if (propertyInfo.GetCustomAttribute<StepPropertyAttribute>() is not null)
            {
                Type = TryGetNested(_propertyInfo.PropertyType);
            }
            else if (propertyInfo.GetCustomAttribute<StepListPropertyAttribute>() is not null)
            {
                Type = Type.MakeGenericSignatureType(
                    typeof(List<>),
                    TryGetNested(TryGetNested(_propertyInfo.PropertyType))
                );
            }
            else if (propertyInfo.GetCustomAttribute<VariableNameAttribute>() is not null)
            {
                Type = typeof(VariableName);
            }
            else
                Type = TryGetNested(_propertyInfo.PropertyType);

            Position = propertyInfo.GetCustomAttribute<StepPropertyBaseAttribute>()?.Order;
        }

        private static void AddFieldFromAttribute<T>(
            string name,
            IDictionary<string, string> dictionary,
            MemberInfo propertyInfo,
            Func<T, string> getAttributeText) where T : Attribute
        {
            var t = propertyInfo.GetCustomAttributes<T>().FirstOrDefault();

            if (t == null)
                return;

            var attributeText = getAttributeText(t);

            if (!string.IsNullOrWhiteSpace(attributeText))
                dictionary.Add(name, attributeText);
        }

        /// <inheritdoc />
        public string Name => _propertyInfo.Name;

        /// <inheritdoc />
        public IReadOnlyCollection<string> Aliases => _propertyInfo
            .GetCustomAttributes<AliasAttribute>()
            .Select(x => x.Name)
            .ToList();

        /// <inheritdoc />
        public string Summary
        {
            get
            {
                try
                {
                    var summary = _propertyInfo.GetXmlDocsSummary();
                    return summary;
                }
                catch (NullReferenceException) //This annoyingly happens for some reason
                {
                    return "";
                }
            }
        }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public bool Required { get; }

        /// <inheritdoc />
        public int? Position { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> ExtraFields { get; }
    }
}

}
