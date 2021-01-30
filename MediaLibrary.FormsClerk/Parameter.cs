// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Describes an <see cref="IInitializer"/> parameter.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        /// <param name="parameterInfo">The <see cref="ParameterInfo"/> to use as the soruce for this parameter.</param>
        public Parameter(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
            {
                throw new ArgumentNullException(nameof(parameterInfo));
            }

            this.Name = parameterInfo.Name;
            this.ParameterType = parameterInfo.ParameterType;

            var display = parameterInfo.GetCustomAttribute<DisplayAttribute>(inherit: true);
            string name = null;
            if (display != null && !string.IsNullOrEmpty(display.Name))
            {
                name = display.GetName();
            }

            if (string.IsNullOrEmpty(name))
            {
                name = parameterInfo.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = parameterInfo.Name;
            }

            var parenthesizePropertyName = parameterInfo.GetCustomAttribute<ParenthesizePropertyNameAttribute>(inherit: true);
            if (parenthesizePropertyName?.NeedParenthesis ?? false)
            {
                name = $"({name})";
            }

            this.DisplayName = name;

            string description = null;
            if (display != null && !string.IsNullOrEmpty(display.Description))
            {
                description = display.GetDescription();
            }

            if (string.IsNullOrEmpty(description))
            {
                description = parameterInfo.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
            }

            this.Description = description;

            if (parameterInfo.HasDefaultValue)
            {
                this.Default = new Maybe<object>(parameterInfo.RawDefaultValue);
            }

            string placeholder = null;
            if (display != null && !string.IsNullOrEmpty(display.Prompt))
            {
                placeholder = display.GetPrompt();
            }

            this.Placeholder = placeholder;

            var passwordPropertyText = parameterInfo.GetCustomAttribute<PasswordPropertyTextAttribute>(inherit: true)?.Password;
            var dataTypes = parameterInfo.GetCustomAttributes<DataTypeAttribute>();

            this.IsPassword = passwordPropertyText ?? false || dataTypes.Any(d => d.DataType == DataType.Password);

            var validations = parameterInfo.GetCustomAttributes<ValidationAttribute>(inherit: true);
            this.Validations = validations.ToList().AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="displayName">The display name of the parameter.</param>
        /// <param name="default">The optional default value.</param>
        /// <param name="description">The description of the parameter.</param>
        /// <param name="placeholder">An optional placeholder value.</param>
        /// <param name="isPassword">A value indicating whether the value is sensitive, such as a password.</param>
        /// <param name="validations">A collection of validation attributes.</param>
        public Parameter(string name, Type parameterType, string displayName = null, Maybe<object> @default = default, string description = null, string placeholder = null, bool isPassword = false, IEnumerable<ValidationAttribute> validations = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
            this.DisplayName = displayName ?? this.Name;
            this.Default = @default;
            this.Description = description;
            this.IsPassword = isPassword;
            this.Validations = (validations ?? Array.Empty<ValidationAttribute>()).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the optional default value.
        /// </summary>
        public Maybe<object> Default { get; }

        /// <summary>
        /// Gets the description of the parameter.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the display name of the parameter.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets a value indicating whether the value is sensitive, such as a password.
        /// </summary>
        public bool IsPassword { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets an optional placeholder value.
        /// </summary>
        public string Placeholder { get; }

        /// <summary>
        /// Gets a collection of validation attributes.
        /// </summary>
        public IReadOnlyList<ValidationAttribute> Validations { get; }
    }
}
