//-----------------------------------------------------------------------
// <copyright file="Parameter.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. If you cannot locate the  
//   Microsoft Public License, please send an email to dlr@microsoft.com. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------
namespace Microsoft.StyleCop.CSharp
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Describes a single method parameter.
    /// </summary>
    /// <subcategory>other</subcategory>
    public sealed partial class Parameter : CodeUnit
    {
        #region Internal Static Fields

        /// <summary>
        /// An empty array of parameters.
        /// </summary>
        internal static readonly Parameter[] EmptyParameterArray = new Parameter[] { };

        #endregion Internal Static Fields

        #region Private Fields

        /// <summary>
        /// The token representing the name of the parameter.
        /// </summary>
        private Token nameToken;

        /// <summary>
        /// The token representing the type of the parameter.
        /// </summary>
        private TypeToken parameterTypeToken;

        /// <summary>
        /// The optional modifiers on the parameter.
        /// </summary>
        private ParameterModifiers? modifiers;

        #endregion Private Fields

        #region Internal Constructors

        /// <summary>
        /// Initializes a new instance of the Parameter class.
        /// </summary>
        /// <param name="proxy">The proxy class.</param>
        internal Parameter(CodeUnitProxy proxy) : base(proxy, CodeUnitType.Parameter)
        {
            Param.Ignore(proxy);
        }

        #endregion Internal Constructors

        #region Public Properties

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name
        {
            get
            {
                Token nameToken = this.NameToken;
                return nameToken == null ? string.Empty : nameToken.Text;
            }
        }

        /// <summary>
        /// Gets the parameter name token.
        /// </summary>
        public Token NameToken
        {
            get 
            {
                this.ValidateEditVersion();
                if (this.nameToken == null)
                {
                    bool foundType = false;

                    for (Token token = this.FindFirstChild<Token>(); token != null; token = token.FindNextSibling<Token>())
                    {
                        if (!foundType)
                        {
                            if (token.TokenType != TokenType.Ref &&
                                token.TokenType != TokenType.Out &&
                                token.TokenType != TokenType.Params &&
                                token.TokenType != TokenType.This)
                            {
                                foundType = true;

                                // If the first thing we encounter is __arglist, then there is no type and only a name.
                                if (token.Text.Equals("__arglist", StringComparison.Ordinal))
                                {
                                    this.nameToken = token;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            this.nameToken = token;
                            break;
                        }
                    }
                }

                return this.nameToken;
            }
        }

        /// <summary>
        /// Gets the parameter type.
        /// </summary>
        public string ParameterType
        {
            get
            {
                Token typeToken = this.ParameterTypeToken;
                return typeToken == null ? string.Empty : typeToken.Text;
            }
        }

        /// <summary>
        /// Gets the parameter type token.
        /// </summary>
        public TypeToken ParameterTypeToken
        {
            get
            {
                this.ValidateEditVersion();
                if (this.parameterTypeToken == null)
                {
                    for (Token token = this.FindFirstChild<Token>(); token != null; token = token.FindNextSibling<Token>())
                    {
                        if (token.TokenType == TokenType.Type)
                        {
                            // If the first thing we encounter is __arglist, then there is no type and only a name.
                            if (!token.Text.Equals("__arglist", StringComparison.Ordinal))
                            {
                                this.parameterTypeToken = (TypeToken)token;
                            }

                            break;
                        }
                    }
                }

                return this.parameterTypeToken;
            }
        }

        /// <summary>
        /// Gets the modifiers applied to this parameter.
        /// </summary>
        public ParameterModifiers Modifiers
        {
            get
            {
                this.ValidateEditVersion();
                if (this.modifiers == null)
                {
                    this.modifiers = ParameterModifiers.None;

                    for (Token token = this.FindFirstChild<Token>(); token != null; token = token.FindNextSibling<Token>())
                    {
                        if (token.TokenType == TokenType.Ref)
                        {
                            this.modifiers |= ParameterModifiers.Ref;
                        }
                        else if (token.TokenType == TokenType.Out)
                        {
                            this.modifiers |= ParameterModifiers.Out;
                        }
                        else if (token.TokenType == TokenType.Params)
                        {
                            this.modifiers |= ParameterModifiers.Params;
                        }
                        else if (token.TokenType == TokenType.This)
                        {
                            this.modifiers |= ParameterModifiers.This;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return this.modifiers.Value; 
            }
        }

        #endregion Public Properties

        #region Protected Override Methods

        /// <summary>
        /// Resets the contents of the parameter.
        /// </summary>
        protected override void Reset()
        {
            this.nameToken = null;
            this.parameterTypeToken = null;
            this.modifiers = null;
        }

        #endregion Protected Override Methods
    }

    /// <content>
    /// Implements the IVariable interface.
    /// </content>
    public partial class Parameter : IVariable
    {
        /// <summary>
        /// Gets the variable name.
        /// </summary>
        public string VariableName
        {
            get
            {
                return this.Name;
            }
        }

        /// <summary>
        /// Gets the variable type.
        /// </summary>
        public TypeToken VariableType
        {
            get
            {
                return this.ParameterTypeToken;
            }
        }

        /// <summary>
        /// Gets the modifiers applied to this variable.
        /// </summary>
        public VariableModifiers VariableModifiers
        {
            get
            {
                return VariableModifiers.None;
            }
        }
    }
}
