using Beyond.NET.Core;
using Beyond.NET.CodeGenerator.Syntax.Kotlin.Declaration;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin.Builders;

public struct GetOnlyProperty
{
    private readonly string m_name;
    private readonly string m_typeName;
    
    private KotlinVisibilities m_visibility = KotlinVisibilities.None;
    private bool m_override = false;
    private bool m_throws = false;
    private string? m_implementation = null;
    
    public GetOnlyProperty(
        string name,
        string typeName
    )
    {
        m_name = name;
        m_typeName = typeName;
    }

    #region Visibility
    public GetOnlyProperty Visibility(KotlinVisibilities visibility)
    {
        m_visibility = visibility;
        
        return this;
    }
    
    public GetOnlyProperty Open()
    {
        return Visibility(KotlinVisibilities.Open);
    }

    public GetOnlyProperty Public()
    {
        return Visibility(KotlinVisibilities.Public);
    }
    
    public GetOnlyProperty Private()
    {
        return Visibility(KotlinVisibilities.Private);
    }
    
    #endregion Visibility

    #region Override
    public GetOnlyProperty Override(bool isOverride = true)
    {
        m_override = isOverride;

        return this;
    }
    #endregion Override

    #region Throws
    public GetOnlyProperty Throws(bool throws = true)
    {
        m_throws = throws;

        return this;
    }
    #endregion Throws

    #region Implementation
    public GetOnlyProperty Implementation(string? implementation = null)
    {
        m_implementation = implementation;

        return this;
    }
    #endregion Implementation

    #region Build
    public KotlinGetOnlyPropertyDeclaration Build()
    {
        return new(
            m_name,
            m_visibility,
            m_override,
            m_throws,
            m_typeName,
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