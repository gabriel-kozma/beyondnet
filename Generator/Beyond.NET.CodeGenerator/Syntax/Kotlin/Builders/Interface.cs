using Beyond.NET.Core;
using Beyond.NET.CodeGenerator.Syntax.Kotlin.Declaration;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin.Builders;

public struct Interface
{
    private readonly string m_name;

    private string? m_baseTypeName = null;
    private string? m_interfaceConformance = null;
    private KotlinVisibilities m_visibility = KotlinVisibilities.None;
    private string? m_implementation = null;
    
    public Interface(
        string name
    )
    {
        m_name = name;
    }
    
    #region Visibility
    public Interface Visibility(KotlinVisibilities visibility)
    {
        m_visibility = visibility;
        
        return this;
    }
    
    public Interface Open()
    {
        return Visibility(KotlinVisibilities.Open);
    }

    public Interface Public()
    {
        return Visibility(KotlinVisibilities.Public);
    }
    
    public Interface Private()
    {
        return Visibility(KotlinVisibilities.Private);
    }
    
    #endregion Visibility

    #region BaseTypeName
    public Interface BaseTypeName(string? baseTypeName = null)
    {
        m_baseTypeName = baseTypeName;

        return this;
    }
    #endregion BaseTypeName
    
    #region InterfaceConformance
    public Interface InterfaceConformance(string? interfaceConformance = null)
    {
        m_interfaceConformance = interfaceConformance;

        return this;
    }
    #endregion InterfaceConformance
    
    #region Implementation
    public Interface Implementation(string? implementation = null)
    {
        m_implementation = implementation;

        return this;
    }
    #endregion Implementation

    #region Build
    
    public KotlinInterfaceDeclaration Build()
    {
        return new (
            m_name,
            m_baseTypeName,
            m_interfaceConformance,
            m_visibility,
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