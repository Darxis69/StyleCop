//-----------------------------------------------------------------------
// <copyright file="Destructor.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Describes a class destructor.
    /// </summary>
    /// <subcategory>element</subcategory>
    public sealed class Destructor : Element
    {
        #region Internal Constructors

        /// <summary>
        /// Initializes a new instance of the Destructor class.
        /// </summary>
        /// <param name="proxy">Proxy object for the destructor.</param>
        /// <param name="name">The name of the destructor.</param>
        /// <param name="attributes">The list of attributes attached to this element.</param>
        /// <param name="unsafeCode">Indicates whether the element resides within a block of unsafe code.</param>
        internal Destructor(CodeUnitProxy proxy, string name, ICollection<Attribute> attributes, bool unsafeCode)
            : base(proxy, ElementType.Destructor, name, attributes, unsafeCode)
        {
            Param.AssertNotNull(proxy, "proxy");
            Param.AssertValidString(name, "name");
            Param.Ignore(attributes);
            Param.Ignore(unsafeCode);
        }

        #endregion Internal Constructors

        #region Public Override Properties

        /// <summary>
        /// Gets the variables defined within this element.
        /// </summary>
        public override IList<IVariable> Variables
        {
            get
            {
                return Method.GatherVariablesForElementWithParametersAndChildStatements(this, null);
            }
        }

        #endregion Public Override Properties

        #region Protected Override Properties

        /// <summary>
        /// Gets the collection of modifiers allowed on this element.
        /// </summary>
        protected override IEnumerable<string> AllowedModifiers
        {
            get
            {
                return CodeParser.DestructorModifiers;
            }
        }

        /// <summary>
        /// Gets the default access modifier for this type.
        /// </summary>
        protected override AccessModifierType DefaultAccessModifierType
        {
            get
            {
                // Static destructors are always public.
                if (this.ContainsModifier(TokenType.Static))
                {
                    return AccessModifierType.Public;
                }

                return base.DefaultAccessModifierType;
            }
        }

        #endregion Protected Override Properties

        #region Protected Override Methods

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        /// <returns>The name of the element.</returns>
        protected override string GetElementName()
        {
            for (Token token = this.FindFirstChild<Token>(); token != null; token = token.FindNextSibling<Token>())
            {
                if (token.Is(TokenType.Literal) || token.Is(TokenType.Type))
                {
                    return "~" + token.Text;
                }
            }

            throw new SyntaxException(this.Document, this.LineNumber);
        }

        #endregion Protected Override Methods
    }
}