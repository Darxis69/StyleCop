//-----------------------------------------------------------------------
// <copyright file="Element.cs" company="Microsoft">
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
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Describes a single element in a C# code file.
    /// </summary>
    /// <subcategory>element</subcategory>
    [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", Justification = "Camel case better serves in this case.")]
    public abstract class Element : CodeUnit, ICodeElement
    {
        #region Internal Static Fields

        /// <summary>
        /// An empty array of elements.
        /// </summary>
        internal static readonly Element[] EmptyElementArray = new Element[] { };

        #endregion Internal Static Fields

        #region Private Static Fields

        /// <summary>
        /// Stores the friendly type names for all element types.
        /// </summary>
        private static ConcurrentDictionary<ElementType, string> friendlyTypeNames = new ConcurrentDictionary<ElementType, string>();

        /// <summary>
        /// Stores the pluralized friendly type names for all element types.
        /// </summary>
        private static ConcurrentDictionary<ElementType, string> friendlyPluralTypeNames = new ConcurrentDictionary<ElementType, string>();

        #endregion Private Static Fields

        #region Private Fields

        /// <summary>
        /// The list of attributes attached to the element.
        /// </summary>
        private ICollection<Attribute> attributes;

        /// <summary>
        /// The name of the element.
        /// </summary>
        private string name;

        /// <summary>
        /// The element's access modifier type.
        /// </summary>
        private AccessModifierType? accessModifier;

        /// <summary>
        /// The actual access level of the element.
        /// </summary>
        private AccessModifierType? actualAccessLevel;

        /// <summary>
        /// The list of modifiers in the declaration.
        /// </summary>
        private Dictionary<TokenType, Token> modifiers;

        /// <summary>
        /// Indicates whether this element is unsafe.
        /// </summary>
        private bool? unsafeCode;

        /// <summary>
        /// The fully qualified name of the element.
        /// </summary>
        private string fullyQualifiedName;

        /// <summary>
        /// The first token in the element's declaration.
        /// </summary>
        private Token firstDeclarationToken;

        /// <summary>
        /// The element's header.
        /// </summary>
        private Tuple<bool, XmlHeader> header;

        /// <summary>
        /// The list of violations in this element.
        /// </summary>
        private Dictionary<string, Violation> violations = new Dictionary<string, Violation>();

        /// <summary>
        /// A private tag which can be used by the current analyzer.
        /// </summary>
        private object analyzerTag;

        #endregion Private Fields

        #region Internal Constructors

        /// <summary>
        /// Initializes a new instance of the Element class.
        /// </summary>
        /// <param name="proxy">Proxy object for the element.</param>
        /// <param name="type">The element type.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="attributes">The list of attributes attached to this element.</param>
        /// <param name="unsafeCode">Indicates whether the element is unsafe.</param>
        internal Element(
            CodeUnitProxy proxy,
            ElementType type,
            string name,
            ICollection<Attribute> attributes,
            bool unsafeCode)
            : this(proxy, (int)type, name, attributes, unsafeCode)
        {
            Param.Ignore(proxy, type, name, attributes, unsafeCode);
        }

        /// <summary>
        /// Initializes a new instance of the Element class.
        /// </summary>
        /// <param name="proxy">Proxy object for the element.</param>
        /// <param name="type">The element type.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="attributes">The list of attributes attached to this element.</param>
        /// <param name="unsafeCode">Indicates whether the element is unsafe.</param>
        internal Element(
            CodeUnitProxy proxy,
            int type,
            string name,
            ICollection<Attribute> attributes,
            bool unsafeCode) 
            : base(proxy, (int)type)
        {
            Param.Ignore(proxy);
            Param.Ignore(type);
            Param.AssertNotNull(name, "name");
            Param.Ignore(attributes);
            Param.Ignore(unsafeCode);

            Debug.Assert(System.Enum.IsDefined(typeof(ElementType), this.ElementType), "The type is invalid.");

            this.name = name;
            this.attributes = attributes;
            this.unsafeCode = unsafeCode;

            if (!unsafeCode && this.ContainsModifier(TokenType.Unsafe))
            {
                this.unsafeCode = true;
            }

            // Fill in the element reference in the attributes list items.
            if (this.attributes != null)
            {
                Debug.Assert(this.attributes.IsReadOnly, "The attributes collection should be read-only.");
            }
        }

        #endregion Internal Constructors

        #region ICodeElement Properties

        /// <summary>
        /// Gets the collection of child elements beneath this element.
        /// </summary>
        IEnumerable<ICodeElement> ICodeElement.ChildCodeElements
        {
            get
            {
                return this.FindChildElements();
            }
        }

        /// <summary>
        /// Gets the containing document.
        /// </summary>
        ICodeDocument ICodeElement.Document
        {
            get
            {
                return this.Document;
            }
        }

        #endregion ICodeElement Properties

        #region Public Virtual Properties

        /// <summary>
        /// Gets the fully qualified name of the element.
        /// </summary>
        public virtual string FullyQualifiedName
        {
            get
            {
                this.ValidateEditVersion();
                if (this.fullyQualifiedName == null)
                {
                    string parentFullyQualifiedName = null;

                    Element parentElement = this.ParentCastedToElement;
                    if (parentElement != null && parentElement.ElementType != ElementType.Document)
                    {
                        parentFullyQualifiedName = parentElement.FullyQualifiedName;
                    }

                    if (string.IsNullOrEmpty(parentFullyQualifiedName))
                    {
                        this.fullyQualifiedName = this.Name;
                    }
                    else
                    {
                        var fullyQualifiedNameBuilder = new StringBuilder();
                        fullyQualifiedNameBuilder.Append(parentFullyQualifiedName);
                        fullyQualifiedNameBuilder.Append(".");

                        if (!string.IsNullOrEmpty(this.Name))
                        {
                            fullyQualifiedNameBuilder.Append(this.Name);
                        }

                        this.fullyQualifiedName = fullyQualifiedNameBuilder.ToString();
                    }
                }

                return this.fullyQualifiedName;
            }
        }

        /// <summary>
        /// Gets the element's access level, without taking into account the access level of the element's parent.
        /// </summary>
        public virtual AccessModifierType AccessModifierType
        {
            get
            {
                this.ValidateEditVersion();

                if (this.accessModifier == null)
                {
                    this.accessModifier = this.DefaultAccessModifierType;
                    this.GatherDeclarationModifiers(this.AllowedModifiers);
                }

                return this.accessModifier.Value;
            }
        }

        /// <summary>
        /// Gets the actual access level of this element, taking into account the
        /// access level of the element's parent.
        /// </summary>
        /// <returns>Returns the actual access level.</returns>
        public virtual AccessModifierType ActualAccessLevel
        {
            get
            {
                this.ValidateEditVersion();
                if (this.actualAccessLevel == null)
                {
                    this.actualAccessLevel = this.ComputeActualAccess();
                }

                return this.actualAccessLevel.Value;
            }
        }

        /// <summary>
        /// Gets the variables defined within this code unit.
        /// </summary>
        public virtual IList<IVariable> Variables
        {
            get
            {
                return CsParser.EmptyVariableArray;
            }
        }

        #endregion Public Virtual Properties

        #region Public Properties

        /// <summary>
        /// Gets the friendly name of the code unit type, which can be used in user output.
        /// </summary>
        public string FriendlyTypeText
        {
            get
            {
                ElementType type = this.ElementType;
                string value = null;
                if (!friendlyTypeNames.TryGetValue(type, out value))
                {
                    value = TypeNames.ResourceManager.GetString(this.GetType().Name, TypeNames.Culture);
                    friendlyTypeNames.AddOrUpdate(type, value, (elementType, originalValue) => originalValue);
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the pluralized friendly name of the code unit type, which can be used in user output.
        /// </summary>
        public string FriendlyPluralTypeText
        {
            get
            {
                ElementType type = this.ElementType;
                string value = null;
                if (!friendlyPluralTypeNames.TryGetValue(type, out value))
                {
                    value = TypeNames.ResourceManager.GetString(this.GetType().Name + "Plural", TypeNames.Culture);
                    friendlyPluralTypeNames.AddOrUpdate(type, value, (elementType, originalValue) => originalValue);
                }

                return value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the element declares an access modifier within its declaration.
        /// </summary>
        public bool DeclaresAccessModifier
        {
            get
            {
                return this.ContainsModifier(TokenType.Public, TokenType.Internal, TokenType.Protected, TokenType.Private);
            }
        }

        /// <summary>
        /// Gets the list of attributes attached to the element.
        /// </summary>
        public ICollection<Attribute> Attributes
        {
            get
            {
                this.ValidateEditVersion();
                if (this.attributes == null)
                {
                    List<Attribute> temp = new List<Attribute>();

                    for (CodeUnit item = this.FindFirstChild<CodeUnit>(); item != null; item = item.FindNextSibling<CodeUnit>())
                    {
                        if (item.Is(CodeUnitType.Attribute))
                        {
                            temp.Add((Attribute)item);
                        }
                        else if (!item.Is(CodeUnitType.LexicalElement) || item.Is(LexicalElementType.Token))
                        {
                            break;
                        }
                    }

                    this.attributes = temp.AsReadOnly();
                }

                return this.attributes;
            }
        }

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        public ElementType ElementType
        {
            get
            {
                return (ElementType)(this.FundamentalType & (int)FundamentalTypeMasks.Element);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the element resides within a block of unsafe code,
        /// or whether the element declares itself as unsafe.
        /// </summary>
        public bool Unsafe
        {
            get
            {
                this.ValidateEditVersion();
                if (this.unsafeCode == null)
                {
                    this.unsafeCode = false;

                    if (this.ContainsModifier(TokenType.Unsafe))
                    {
                        this.unsafeCode = true;
                    }
                    else
                    {
                        Element parent = this.ParentCastedToElement;
                        if (parent != null)
                        {
                            bool parentIsUnsafe = parent.Unsafe;
                            if (parentIsUnsafe)
                            {
                                this.unsafeCode = true;
                            }
                        }
                    }
                }

                return this.unsafeCode.Value;
            }
        }

        /// <summary>
        /// Gets the violations found in this element.
        /// </summary>
        public ICollection<Violation> Violations
        {
            get
            {
                return this.violations.Values;
            }
        }

        /// <summary>
        /// Gets or sets the analyzer tag.
        /// </summary>
        /// <remarks>A StyleCop rules analyzer can temporarily store and retrieve a value in this property. This value will be lost once 
        /// the rules analyzer has completed its analysis of the document containing this code element.</remarks>
        public object AnalyzerTag
        {
            get
            {
                return this.analyzerTag;
            }

            set
            {
                Param.Ignore(value);
                this.analyzerTag = value;
            }
        }

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string Name
        {
            get
            {
                this.ValidateEditVersion();
                if (this.name == null)
                {
                    this.name = this.GetElementName();
                    Debug.Assert(this.name != null, "GetElementName must never return null.");
                }

                return this.name;
            }
        }

        /// <summary>
        /// Gets the first token in the element's declaration.
        /// </summary>
        public Token FirstDeclarationToken
        {
            get
            {
                this.ValidateEditVersion();
                if (this.firstDeclarationToken == null)
                {
                    for (CodeUnit item = this.FindFirstDescendent<CodeUnit>(); item != null; item = item.FindNextDescendentOf<CodeUnit>(this))
                    {
                        if (item.Is(CodeUnitType.Attribute))
                        {
                            // Move to the end of the attribute.
                            item = item.FindLastDescendent<CodeUnit>();
                        }
                        else if (item.Is(LexicalElementType.Token))
                        {
                            this.firstDeclarationToken = (Token)item;
                        }
                    }
                }

                return this.firstDeclarationToken;
            }
        }

        #endregion Public Properties

        #region Protected Virtual Properties

        /// <summary>
        /// Gets the collection of modifiers allowed on this element.
        /// </summary>
        protected virtual IEnumerable<string> AllowedModifiers
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        /// Gets the default access modifier for this element.
        /// </summary>
        protected virtual AccessModifierType DefaultAccessModifierType
        {
            get
            {
                return AccessModifierType.Private;
            }
        }

        #endregion Protected Virtual Properties

        #region Private Properties

        /// <summary>
        /// Gets the parent of the element hard-casted to an element.
        /// </summary>
        /// <remarks>The parent of an element must always be an element.</remarks>
        private Element ParentCastedToElement
        {
            get
            {
                CodeUnit parent = this.Parent;
                if (parent == null)
                {
                    return null;
                }

                Debug.Assert(parent.Is(CodeUnitType.Element), "The parent of an Element must always be an Element.");
                return (Element)parent;
            }
        }

        #endregion Private Properties

        #region Public Virtual Methods

        /// <summary>
        /// Clears the analyzer tags for this element.
        /// </summary>
        /// <remarks>This method should only be called by the StyleCop framework.</remarks>
        public virtual void ClearAnalyzerTags()
        {
            this.analyzerTag = null;
        }

        #endregion Public Virtual Methods

        #region Public Methods

        /// <summary>
        /// Gets the contents of the Xml header, if any.
        /// </summary>
        /// <returns>Returns the header or null if there is none.</returns>
        public XmlHeader GetHeader()
        {
            this.ValidateEditVersion();

            if (this.header == null)
            {
                XmlHeader foundHeader = this.FindFirstChild<XmlHeader>();
                this.header = new Tuple<bool, XmlHeader>(foundHeader != null, foundHeader);
            }

            return this.header.Item2;
        }

        /// <summary>
        /// Indicates whether the element declaration contains one of the given modifiers.
        /// </summary>
        /// <param name="types">The modifier types to check for.</param>
        /// <returns>Returns true if the declaration contains at least one of the given modifiers.</returns>
        public bool ContainsModifier(params TokenType[] types)
        {
            Param.RequireNotNull(types, "types");

            this.ValidateEditVersion();

            if (this.modifiers == null)
            {
                this.GatherDeclarationModifiers(this.AllowedModifiers);
                Debug.Assert(this.modifiers != null, "Modifiers should be non-null now.");
            }

            for (int i = 0; i < types.Length; ++i)
            {
                if (this.modifiers.ContainsKey(types[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds one violation to this element.
        /// </summary>
        /// <param name="violation">The violation to add.</param>
        /// <returns>Returns false if there is already an identical violation in the element.</returns>
        public bool AddViolation(Violation violation)
        {
            Param.RequireNotNull(violation, "violation");
            string key = violation.Key;

            if (!this.violations.ContainsKey(key))
            {
                this.violations.Add(violation.Key, violation);
                return true;
            }

            return false;
        }

        #endregion Public Methods

        #region Internal Virtual Methods

        /// <summary>
        /// Initializes the element.
        /// </summary>
        /// <param name="document">The document that contains the element.</param>
        internal virtual void Initialize(CsDocument document)
        {
            Param.Ignore(document);
        }

        #endregion Internal Virtual Methods

        #region Protected Override Methods

        /// <summary>
        /// Resets the contents of the item.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            this.attributes = null;
            this.name = null;
            this.accessModifier = null;
            this.modifiers = null;
            this.unsafeCode = null;
            this.fullyQualifiedName = null;
            this.firstDeclarationToken = null;
            this.header = null;
        }

        #endregion Protected Override Methods

        #region Protected Abstract Methods

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        /// <returns>The name of the element.</returns>
        protected abstract string GetElementName();

        #endregion Protected Abstract Methods

        #region Private Static Methods

        /// <summary>
        /// Gets another type of modifier for an element declaration.
        /// </summary>
        /// <param name="allowedModifiers">The types of allowed modifiers for the element.</param>
        /// <param name="elementModifiers">The collection of modifiers on the element.</param>
        /// <param name="token">The modifier token.</param>
        /// <returns>true to continue collecting modifiers; false to quit.</returns>
        private static bool GetOtherElementModifier(IEnumerable<string> allowedModifiers, Dictionary<TokenType, Token> elementModifiers, Token token)
        {
            Param.Ignore(allowedModifiers);
            Param.AssertNotNull(elementModifiers, "elementModifiers");
            Param.AssertNotNull(token, "token");

            bool stop = true;

            // If the modifier is one of the allowed modifiers, store it. Otherwise, we are done.
            if (allowedModifiers != null)
            {
                foreach (string allowedModifier in allowedModifiers)
                {
                    if (string.Equals(token.Text, allowedModifier, StringComparison.Ordinal))
                    {
                        elementModifiers.Add(token.TokenType, token);
                        stop = false;
                        break;
                    }
                }
            }

            return !stop;
        }

        /////// <summary>
        /////// Determines whether the given text string contains an xml header summary tag.
        /////// </summary>
        /////// <param name="text">The text to check.</param>
        /////// <returns>Returns true if the text is a summary; false otherwise.</returns>
        ////private static bool IsXmlHeaderSummaryLine(string text)
        ////{
        ////    Param.AssertNotNull(text, "text");

        ////    const string Summary = "summary";

        ////    for (int i = 0; i < text.Length; ++i)
        ////    {
        ////        if (text[i] == '<')
        ////        {
        ////            for (int j = 0; j < Summary.Length; ++j)
        ////            {
        ////                int index = i + j + 1;
        ////                if (text.Length <= index)
        ////                {
        ////                    return false;
        ////                }

        ////                if (Summary[j] != text[index])
        ////                {
        ////                    return false;
        ////                }
        ////            }

        ////            return true;
        ////        }
        ////        else if (!char.IsWhiteSpace(text[i]))
        ////        {
        ////            break;
        ////        }
        ////    }

        ////    return false;
        ////}

        #endregion Private Static Methods

        #region Private Methods

        /// <summary>
        /// Merges the access of this element with the access of its parent to determine
        /// the actual visibility of this item outside of the class.
        /// </summary>
        /// <returns>Returns the actual access level.</returns>
        private AccessModifierType ComputeActualAccess()
        {
            AccessModifierType localAccess = this.AccessModifierType;

            if (localAccess == AccessModifierType.Private)
            {
                return localAccess;
            }

            Element parentElement = this.ParentCastedToElement;
            if (parentElement == null)
            {
                return localAccess;
            }

            AccessModifierType parentActualAccess = parentElement.ActualAccessLevel;
            AccessModifierType actualAccess = localAccess;

            if (parentActualAccess == AccessModifierType.Public)
            {
                return actualAccess;
            }
            else if (parentActualAccess == AccessModifierType.ProtectedInternal)
            {
                if (actualAccess == AccessModifierType.Public)
                {
                    return AccessModifierType.ProtectedInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.Protected)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal)
                {
                    return AccessModifierType.Protected;
                }
                else if (actualAccess == AccessModifierType.Internal)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else 
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.Internal)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal)
                {
                    return AccessModifierType.Internal;
                }
                else if (actualAccess == AccessModifierType.Protected)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.ProtectedAndInternal)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal ||
                    actualAccess == AccessModifierType.Protected ||
                    actualAccess == AccessModifierType.Internal)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else
            {
                return AccessModifierType.Private;
            }
        }

        /// <summary>
        /// Walks the element declaration and gathers the element modifiers.
        /// </summary>
        /// <param name="allowedModifiers">The modifiers which are allowed for the current element type.</param>
        private void GatherDeclarationModifiers(IEnumerable<string> allowedModifiers)
        {
            Param.Ignore(allowedModifiers);

            this.modifiers = new Dictionary<TokenType, Token>();

            Token accessModifierSeen = null;

            for (Token token = this.FirstDeclarationToken; token != null; token = token.FindNextSibling<Token>())
            {
                if (token.TokenType == TokenType.Public)
                {
                    // A public access modifier can only be specified if there have been no other access modifiers.
                    if (accessModifierSeen != null)
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    this.accessModifier = AccessModifierType.Public;
                    accessModifierSeen = token;
                    this.modifiers.Add(TokenType.Public, token);
                }
                else if (token.TokenType == TokenType.Private)
                {
                    // A private access modifier can only be specified if there have been no other access modifiers.
                    if (accessModifierSeen != null)
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    this.accessModifier = AccessModifierType.Private;
                    accessModifierSeen = token;
                    this.modifiers.Add(TokenType.Private, token);
                }
                else if (token.TokenType == TokenType.Internal)
                {
                    // The access is internal unless we have already seen a protected access
                    // modifier, in which case it is protected internal.
                    if (accessModifierSeen == null)
                    {
                        this.accessModifier = AccessModifierType.Internal;
                    }
                    else if (accessModifierSeen.TokenType == TokenType.Protected)
                    {
                        this.accessModifier = AccessModifierType.ProtectedInternal;
                    }
                    else
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    accessModifierSeen = token;
                    this.modifiers.Add(TokenType.Internal, token);
                }
                else if (token.TokenType == TokenType.Protected)
                {
                    // The access is protected unless we have already seen an internal access
                    // modifier, in which case it is protected internal.
                    if (accessModifierSeen == null)
                    {
                        this.accessModifier = AccessModifierType.Protected;
                    }
                    else if (accessModifierSeen.TokenType == TokenType.Internal)
                    {
                        this.accessModifier = AccessModifierType.ProtectedInternal;
                    }
                    else
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    accessModifierSeen = token;
                    this.modifiers.Add(TokenType.Protected, token);
                }
                else
                {
                    if (!GetOtherElementModifier(allowedModifiers, this.modifiers, token))
                    {
                        break;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}