using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace PlantUmlClassDiagramGenerator.Library
{
    public class RelationshipCollection : IEnumerable<Relationship>
    {
        private IList<Relationship> _items { get; set; } = new List<Relationship>();

        private IList<string> InvisibleClassOrInterface { get; set; } = new List<string>
        {
            "ICloneable",
            "IDisposable",
            "IComparable",
            "DateTime",
            "C1dStrings",
            "Dictionary",
            "List",

        };

        public void AddInheritanceFrom(TypeDeclarationSyntax syntax)
        {
            if (syntax.BaseList == null) return;

            var subTypeName = TypeNameText.From(syntax);

            foreach (var typeStntax in syntax.BaseList.Types)
            {
                var typeNameSyntax = typeStntax.Type as SimpleNameSyntax;
                if (typeNameSyntax == null) continue;
                var baseTypeNames = TypeNameText.From(typeNameSyntax);

                foreach (var baseTypeName in baseTypeNames)
                {
                    // 특정 클래스는 스킵
                    if (InvisibleClassOrInterface.Contains(baseTypeName.Identifier))
                        continue;

                    _items.Add(new Relationship(baseTypeName, subTypeName, "<|--", baseTypeName.TypeArguments));
                }
            }
        }

        public void AddInnerclassRelationFrom(SyntaxNode node)
        {
            var outerTypeNode = node.Parent as BaseTypeDeclarationSyntax;
            var innerTypeNode = node as BaseTypeDeclarationSyntax;

            if (outerTypeNode == null || innerTypeNode == null) return;

            var outerTypeName = TypeNameText.From(outerTypeNode);
            var innerTypeName = TypeNameText.From(innerTypeNode);
            _items.Add(new Relationship(outerTypeName, innerTypeName, "+--"));
        }

        public void AddAssociationFrom(FieldDeclarationSyntax node, VariableDeclaratorSyntax field)
        {
            var baseNode = node.Declaration.Type as SimpleNameSyntax;
            var subNode = node.Parent as BaseTypeDeclarationSyntax;

            if (baseNode == null || subNode == null) return;

            var symbol = field.Initializer == null ? "-->" : "o->";

            var baseNames = TypeNameText.From(baseNode);
            var subName = TypeNameText.From(subNode);

            foreach (var baseName in baseNames)
            {
                if (InvisibleClassOrInterface.Contains(baseName.Identifier))
                    continue;

                _items.Add(new Relationship(subName, baseName, symbol, "", field.Identifier.ToString() + baseName.TypeArguments));
            }
        }

        public void AddAssociationFrom(PropertyDeclarationSyntax node)
        {
            var baseNode = node.Type as SimpleNameSyntax;
            var subNode = node.Parent as BaseTypeDeclarationSyntax;

            if (baseNode == null || subNode == null) return;

            var symbol = node.Initializer == null ? "-->" : "o->";

            var baseNames = TypeNameText.From(baseNode);
            var subName = TypeNameText.From(subNode);

            foreach (var baseName in baseNames)
            {
                if (InvisibleClassOrInterface.Contains(baseName.Identifier))
                    continue;

                _items.Add(new Relationship(subName, baseName, symbol, "", node.Identifier.ToString() + baseName.TypeArguments));
            }
        }

        public IEnumerator<Relationship> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
