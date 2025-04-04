using Beyond.NET.Core;
using Beyond.NET.CodeGenerator.Syntax.Kotlin.Declaration;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin.Builders;

public struct Class
{
    private readonly string m_name;

    private string? m_baseTypeName = null;
    private string? m_interfaceConformance = null;
    private KotlinVisibilities m_visibility = KotlinVisibilities.None;
    private string? m_implementation = null;
    private KotlinFunSignatureParameters? m_primaryConstructorParameters = null;
    private IEnumerable<string> m_baseTypePrimaryConstructorParameterNames = [];
    
    public Class(
        string name
    )
    {
        m_name = name;
    }
    
    #region Visibility
    public Class Visibility(KotlinVisibilities visibility)
    {
        m_visibility = visibility;
        
        return this;
    }
    
    public Class Open()
    {
        return Visibility(KotlinVisibilities.Open);
    }

    public Class Public()
    {
        return Visibility(KotlinVisibilities.Public);
    }
    
    public Class Private()
    {
        return Visibility(KotlinVisibilities.Private);
    }
    
    #endregion Visibility

    #region BaseTypeName
    public Class BaseTypeName(string? baseTypeName = null)
    {
        m_baseTypeName = baseTypeName;

        return this;
    }
    #endregion BaseTypeName
    
    #region InterfaceConformance
    public Class InterfaceConformance(string? interfaceConformance = null)
    {
        m_interfaceConformance = interfaceConformance;

        return this;
    }
    #endregion InterfaceConformance
    
    #region Implementation
    public Class Implementation(string? implementation = null)
    {
        m_implementation = implementation;

        return this;
    }
    #endregion Implementation
    
    #region primaryConstructor
    
    public Class PrimaryConstructorParameters(KotlinFunSignatureParameters primaryConstructorParameters) 
    {
        m_primaryConstructorParameters = primaryConstructorParameters;

        return this;
    }

    public Class BaseTypePrimaryConstructorParameterNames(IEnumerable<string> baseTypePrimaryConstructorParameterNames)
    {
        m_baseTypePrimaryConstructorParameterNames = baseTypePrimaryConstructorParameterNames;

        return this;
    }

    #endregion

    #region Build
    
    public KotlinClassDeclaration Build()
    {
        return new(
            m_name,
            m_baseTypeName,
            m_interfaceConformance,
            m_visibility,
            m_primaryConstructorParameters,
            m_baseTypePrimaryConstructorParameterNames,
            m_implementation
        );
    }

    public override string ToString()
    {
        return Build()
            .ToString();
    }
    
    public string ToIndentedString(int indentationLevel)
    {
        return Build()
            .ToString()
            .IndentAllLines(indentationLevel);
    }
    #endregion Build
}