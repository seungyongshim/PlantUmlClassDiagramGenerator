using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlantUmlClassDiagramGenerator.Library
{
    public class ClassDiagramGenerator : CSharpSyntaxWalker
    {
        private readonly string indent;
        private IList<SyntaxNode> _additionalTypeDeclarationNodes;
        private bool _createAssociation;
        private Accessibilities _ignoreMemberAccessibilities;
        private int nestingDepth = 0;
        private HashSet<string> types = new HashSet<string>();
        private TextWriter writer;

        public ClassDiagramGenerator(TextWriter writer, string indent, Accessibilities ignoreMemberAccessibilities = Accessibilities.None, bool createAssociation = true)
        {
            this.writer = writer;
            this.indent = indent;
            _additionalTypeDeclarationNodes = new List<SyntaxNode>();
            _ignoreMemberAccessibilities = ignoreMemberAccessibilities;
            _createAssociation = createAssociation;
        }

        private RelationshipCollection _relationships { get; set; }
                            = new RelationshipCollection();

        private IDictionary<string, string> LutUsingType { get; set; } = new Dictionary<string, string>
        {
            ["C1dStrings"] = "List<string, string>",
            ["C1dValues"] = "List<CValue>",
            ["C1dColumnValues"] = "Dictionary<int, CValue>",
            ["C1dValuePtrs"] = "List<CValue>",
        };

        public void Generate(SyntaxNode root)
        {
            WriteLine("@startuml");
            GenerateInternal(root);
            WriteLine("@enduml");
        }

        public void GenerateInternal(SyntaxNode root)
        {
            Visit(root);
            //GenerateAdditionalTypeDeclarations();
            GenerateRelationships();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitTypeDeclaration(node, () => base.VisitClassDeclaration(node));
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (IsIgnoreMember(node.Modifiers)) { return; }

            var modifiers = GetMemberModifiersText(node.Modifiers);
            var name = node.Identifier.ToString();
            var args = node.ParameterList.Parameters.Select(p => $"{p.Identifier}:{p.Type}");

            WriteLine($"{modifiers}{name}({string.Join(", ", args)})");
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (SkipInnerTypeDeclaration(node)) { return; }

            _relationships.AddInnerclassRelationFrom(node);

            var type = $"{node.Identifier}";

            types.Add(type);

            WriteLine($"{node.EnumKeyword} {type} {{");

            nestingDepth++;
            base.VisitEnumDeclaration(node);
            nestingDepth--;

            WriteLine("}");
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            WriteLine($"{node.Identifier}{node.EqualsValue},");
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (IsIgnoreMember(node.Modifiers)) { return; }

            var modifiers = GetMemberModifiersText(node.Modifiers);
            var name = string.Join(",", node.Declaration.Variables.Select(v => v.Identifier));
            var typeName = node.Declaration.Type.ToString();

            WriteLine($"{modifiers} <<{node.EventKeyword}>> {name} : {typeName} ");
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (IsIgnoreMember(node.Modifiers)) { return; }

            var modifiers = GetMemberModifiersText(node.Modifiers);
            var type = node.Declaration.Type;
            var variables = node.Declaration.Variables;
            var parentClass = (node.Parent as TypeDeclarationSyntax);
            var isTypeParameterField = parentClass?.TypeParameterList?.Parameters
                .Any(t => t.Identifier.Text == type.ToString()) ?? false;

            foreach (var field in variables)
            {
                Type fieldType = type.GetType();
                if (true || fieldType == typeof(PredefinedTypeSyntax) || fieldType == typeof(NullableTypeSyntax) || isTypeParameterField)
                {
                    var useLiteralInit = field.Initializer?.Value?.Kind().ToString().EndsWith("LiteralExpression") ?? false;
                    var initValue = useLiteralInit ? (" = " + field.Initializer.Value.ToString()) : "";
                    WriteLine($"{modifiers}{field.Identifier} : {type.ToString()}{initValue}");

                    if (fieldType == typeof(GenericNameSyntax))
                    {
                        _additionalTypeDeclarationNodes.Add(type);
                        //_relationships.AddAssociationFrom(node, field);
                    }
                    else
                    {
                        _relationships.AddAssociationFrom(node, field);
                    }
                }
            }
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            if (_createAssociation)
            {
                _additionalTypeDeclarationNodes.Add(node);
            }
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            VisitTypeDeclaration(node, () => base.VisitInterfaceDeclaration(node));
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (IsIgnoreMember(node.Modifiers)) { return; }

            var modifiers = GetMemberModifiersText(node.Modifiers);
            var name = node.Identifier.ToString();
            var returnType = node.ReturnType.ToString();
            var args = node.ParameterList.Parameters.Select(p => $"{p.Identifier}:{p.Type}");

            WriteLine($"{modifiers}{name}({string.Join(", ", args)}) : {returnType}");
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (IsIgnoreMember(node.Modifiers)) { return; }

            var type = node.Type;

            var parentClass = (node.Parent as TypeDeclarationSyntax);
            var isTypeParameterProp = parentClass?.TypeParameterList?.Parameters
                .Any(t => t.Identifier.Text == type.ToString()) ?? false;

            var modifiers = GetMemberModifiersText(node.Modifiers);
            var name = node.Identifier.ToString();

            var result = LutUsingType.Keys.Contains(type.ToString()) ? LutUsingType[type.ToString()]
                                                                     : type.ToString();

            WriteLine($"{modifiers}{name} : {result}");

            if (type.GetType() == typeof(GenericNameSyntax))
            {
                _additionalTypeDeclarationNodes.Add(type);
                _relationships.AddAssociationFrom(node);
            }
            else
            {
                _relationships.AddAssociationFrom(node);
            }
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (SkipInnerTypeDeclaration(node)) { return; }

            _relationships.AddInnerclassRelationFrom(node);
            _relationships.AddInheritanceFrom(node);

            var typeName = TypeNameText.From(node);
            var name = typeName.Identifier;
            var typeParam = typeName.TypeArguments;
            var type = $"{name}{typeParam}";

            types.Add(name);

            WriteLine($"class {type} <<struct>> {{");

            nestingDepth++;
            base.VisitStructDeclaration(node);
            nestingDepth--;

            WriteLine("}");
        }

        private void GenerateAdditionalGenericTypeDeclaration(GenericNameSyntax genericNode)
        {
            var typename = TypeNameText.From(genericNode);
            if (!types.Contains(typename.Identifier))
            {
                WriteLine($"class {typename.Identifier}{typename.TypeArguments} {{");
                WriteLine("}");
                types.Add(typename.Identifier);
            }
        }

        private void GenerateAdditionalTypeDeclarations()
        {
            for (int i = 0; i < _additionalTypeDeclarationNodes.Count; i++)
            {
                SyntaxNode node = _additionalTypeDeclarationNodes[i];
                if (node is GenericNameSyntax genericNode)
                {
                    continue;
                }
                Visit(node);
            }
        }

        private void GenerateRelationships()
        {
            foreach (var relationship in _relationships.Distinct())
            {
                WriteLine(relationship.ToString());
            }
        }

        private string GetMemberModifiersText(SyntaxTokenList modifiers)
        {
            var tokens = modifiers.Select(token =>
            {
                switch (token.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                        return "+";

                    case SyntaxKind.PrivateKeyword:
                        return "-";

                    case SyntaxKind.ProtectedKeyword:
                        return "#";

                    case SyntaxKind.AbstractKeyword:
                    case SyntaxKind.StaticKeyword:
                        return $"{{{token.ValueText}}}";

                    case SyntaxKind.InternalKeyword:
                    default:
                        return $"<<{token.ValueText}>>";
                }
            });
            var result = string.Join(" ", tokens);
            if (result != string.Empty)
            {
                result += " ";
            };
            return result;
        }

        private string GetTypeModifiersText(SyntaxTokenList modifiers)
        {
            var tokens = modifiers.Select(token =>
            {
                switch (token.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.PrivateKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.AbstractKeyword:
                        return "";

                    default:
                        return $"<<{token.ValueText}>>";
                }
            }).Where(token => token != "");

            var result = string.Join(" ", tokens);
            if (result != string.Empty)
            {
                result += " ";
            };
            return result;
        }

        private bool IsIgnoreMember(SyntaxTokenList modifiers)
        {
            if (_ignoreMemberAccessibilities == Accessibilities.None) { return false; }

            var tokenKinds = modifiers.Select(x => x.Kind()).ToArray();

            if (_ignoreMemberAccessibilities.HasFlag(Accessibilities.ProtectedInternal)
                && tokenKinds.Contains(SyntaxKind.ProtectedKeyword)
                && tokenKinds.Contains(SyntaxKind.InternalKeyword))
            {
                return true;
            }

            if (_ignoreMemberAccessibilities.HasFlag(Accessibilities.Public)
                && tokenKinds.Contains(SyntaxKind.PublicKeyword))
            {
                return true;
            }

            if (_ignoreMemberAccessibilities.HasFlag(Accessibilities.Protected)
                && tokenKinds.Contains(SyntaxKind.ProtectedKeyword))
            {
                return true;
            }

            if (_ignoreMemberAccessibilities.HasFlag(Accessibilities.Internal)
                && tokenKinds.Contains(SyntaxKind.InternalKeyword))
            {
                return true;
            }

            if (_ignoreMemberAccessibilities.HasFlag(Accessibilities.Private)
                && tokenKinds.Contains(SyntaxKind.PrivateKeyword))
            {
                return true;
            }
            return false;
        }

        private bool SkipInnerTypeDeclaration(SyntaxNode node)
        {
            if (nestingDepth <= 0) return false;

            _additionalTypeDeclarationNodes.Add(node);
            return true;
        }

        private void VisitTypeDeclaration(TypeDeclarationSyntax node, Action visitBase)
        {
            if (SkipInnerTypeDeclaration(node)) { return; }

            _relationships.AddInnerclassRelationFrom(node);
            _relationships.AddInheritanceFrom(node);

            var modifiers = GetTypeModifiersText(node.Modifiers);
            var keyword = (node.Modifiers.Any(SyntaxKind.AbstractKeyword) ? "abstract " : "")
                + node.Keyword.ToString();

            var typeName = TypeNameText.From(node);
            var name = typeName.Identifier;
            var typeParam = typeName.TypeArguments;
            var type = $"{name}{typeParam}";

            types.Add(name);

            WriteLine($"{keyword} {type} {modifiers}{{");

            nestingDepth++;
            visitBase();
            nestingDepth--;

            WriteLine("}");
        }

        private void WriteLine(string line)
        {
            var space = string.Concat(Enumerable.Repeat(indent, nestingDepth));
            writer.WriteLine(space + line);
        }
    }
}