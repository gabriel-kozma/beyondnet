using Beyond.NET.Core;
using Beyond.NET.CodeGenerator.Syntax.Kotlin.Declaration;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin.Builders;

public struct GetOnlyProperty
{
    private readonly string m_name;

    private KotlinVisibilities m_visibility = KotlinVisibilities.None;
    private bool m_external = false;
    private bool m_override = false;
    private bool m_operator = false;
    private string? m_extendedTypeName = null;
    private string? m_parameters = null;
    private string? m_returnTypeName = null;
    private string? m_implementation = null;
    private HashSet<string> m_attributes = new();

    public GetOnlyProperty
    (
        string name,
        string returnType
    )
    {
        m_name = name;
        m_returnTypeName = returnType;
        m_attributes.Add("@JvmStatic");
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

    #region ReturnTypeName
    public GetOnlyProperty ReturnTypeName(string? returnTypeName = null)
    {
        m_returnTypeName = returnTypeName;

        return this;
    }
    #endregion ReturnTypeName
    
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
            m_returnTypeName,
            m_attributes,
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