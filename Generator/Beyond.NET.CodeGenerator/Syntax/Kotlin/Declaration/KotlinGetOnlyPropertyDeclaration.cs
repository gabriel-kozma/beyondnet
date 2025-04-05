using Beyond.NET.Core;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin.Declaration;

public struct KotlinGetOnlyPropertyDeclaration
{
    public string Name { get; }
    public KotlinVisibilities Visibility { get; }
    public string? ReturnTypeName { get; }
    public HashSet<string>? Attributes { get; }
    public string? Implementation { get; }
    
    public KotlinGetOnlyPropertyDeclaration(
        string name,
        KotlinVisibilities visibility,
        string? returnTypeName,
        HashSet<string>? attributes,
        string? implementation
    )
    {
        Name = !string.IsNullOrEmpty(name)
            ? name 
            : throw new ArgumentOutOfRangeException(nameof(name));
        
        Visibility = visibility;
        
        ReturnTypeName = !string.IsNullOrEmpty(returnTypeName)
            ? returnTypeName
            : null;

        Attributes = attributes;
        
        Implementation = !string.IsNullOrEmpty(implementation)
            ? implementation
            : null;
    }

    public override string ToString()
    {
        const string fun = "fun";
        
        string visibilityString = Visibility.ToKotlinSyntaxString();

        string returnString = !string.IsNullOrEmpty(ReturnTypeName)
            ? $": {ReturnTypeName}"
            : string.Empty;

        string attributesString;

        if (Attributes is not null) {
            attributesString = string.Join(" ", Attributes);
        } else {
            attributesString = string.Empty;
        }

        string[] signatureComponents = [
            attributesString,
            visibilityString,
            fun,
            $"{Name}()",
            returnString
        ];

        string signature = KotlinFunSignatureComponents.ComponentsToString(signatureComponents);

        string fullFunc;
        
        if (!string.IsNullOrEmpty(Implementation)) {
            string indentedImpl = Implementation.IndentAllLines(1);
            
            fullFunc = $"{signature} {{\n{indentedImpl}\n}}";
        } else {
            fullFunc = signature;
        }

        return fullFunc;
    }
}
