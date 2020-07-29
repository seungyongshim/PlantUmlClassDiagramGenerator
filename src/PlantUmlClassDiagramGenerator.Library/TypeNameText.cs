using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace PlantUmlClassDiagramGenerator.Library
{
    public class TypeNameText
    {
        public string Identifier { get; set; }

        public string TypeArguments { get; set; }
        
        public static IEnumerable<TypeNameText> From(SimpleNameSyntax syntax)
        {
            var identifier = syntax.Identifier.Text;
            var typeArgs = string.Empty;

            yield return new TypeNameText
            {
                Identifier = identifier,
                TypeArguments = typeArgs
            };

            if (syntax is GenericNameSyntax genericName)
            {
                foreach (var item in genericName.TypeArgumentList.Arguments)
                {
                    switch (item)
                    {
                        case SimpleNameSyntax x:
                            yield return new TypeNameText
                            {
                                Identifier = x.Identifier.Text,
                                TypeArguments = typeArgs
                            };
                            break;
                        default:
                            break;
                    }

                    
                }
            }
        }

        public static TypeNameText From(GenericNameSyntax syntax)
        {
            int paramCount = syntax.TypeArgumentList.Arguments.Count;
            string[] parameters = new string[paramCount];
            if (paramCount > 1)
            {
                for (int i = 0; i < paramCount; i++)
                {
                    parameters[i] = $"T{i + 1}";
                }

            }
            else
            {
                parameters[0] = "T";
            }
            return new TypeNameText
            {
                Identifier = $"\"{syntax.Identifier.Text}`{paramCount}\"",
                TypeArguments = "<" + string.Join(",", parameters) + ">",
            };
        }

        public static TypeNameText From(BaseTypeDeclarationSyntax syntax)
        {
            var identifier = syntax.Identifier.Text;
            var typeArgs = string.Empty;
            var typeDeclaration = syntax as TypeDeclarationSyntax;
            if (typeDeclaration != null && typeDeclaration.TypeParameterList != null)
            {
                var count = typeDeclaration.TypeParameterList.Parameters.Count;
                identifier = $"\"{identifier}`{count}\"";
                typeArgs = "<" + string.Join(",", typeDeclaration.TypeParameterList.Parameters) + ">";
            }
            return new TypeNameText
            {
                Identifier = identifier,
                TypeArguments = typeArgs
            };
        }
    }
}