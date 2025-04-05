using System.Reflection;

using Beyond.NET.CodeGenerator.Extensions;
using Beyond.NET.CodeGenerator.Generator.Kotlin;
using Beyond.NET.CodeGenerator.Types;
using Beyond.NET.Core;

namespace Beyond.NET.CodeGenerator.Syntax.Kotlin;

public partial class KotlinTypeSyntaxWriter
{

    internal class DelegateTypeInfo
    {
        internal Type Type { get; }
        internal TypeDescriptor TypeDescriptor { get; }
        internal string TypeName { get; }
        internal string FullTypeName { get; }
        internal string CTypeName { get; }
        internal string KotlinTypeName { get; }

        internal Type? BaseType { get; }
        internal TypeDescriptor? BaseTypeDescriptor { get; }
        internal string KotlinBaseTypeName { get; }

        internal Type ReturnType { get; }
        internal TypeDescriptor ReturnTypeDescriptor { get; }
        internal bool IsReturning { get; }
        internal bool ReturnTypeIsPrimitive { get; }
        internal bool ReturnTypeIsOptional { get; }
        internal bool ReturnTypeIsReadOnlySpanOfByte { get; }
        internal string KotlinReturnTypeName { get; }

        internal ParameterInfo[] ParameterInfos { get; }

        internal MethodInfo? DelegateInvokeMethod { get; }

        internal DelegateTypeInfo(
            Type type,
            MethodInfo? delegateInvokeMethod,
            TypeDescriptorRegistry typeDescriptorRegistry
        ) : this(
            type,
            delegateInvokeMethod,
            delegateInvokeMethod?.GetParameters() ?? Array.Empty<ParameterInfo>(),
            delegateInvokeMethod?.ReturnType ?? typeof(void),
            typeDescriptorRegistry
        )
        {
        }

        internal DelegateTypeInfo(
            Type type,
            MethodInfo? delegateInvokeMethod,
            ParameterInfo[] parameterInfos,
            Type returnType,
            TypeDescriptorRegistry typeDescriptorRegistry
        )
        {
            DelegateInvokeMethod = delegateInvokeMethod;

            Type = type;
            TypeDescriptor = type.GetTypeDescriptor(typeDescriptorRegistry);

            string? fullTypeName = type.FullName;

            if (string.IsNullOrEmpty(fullTypeName))
            {
                throw new Exception($"// Type \"{type.Name}\" was skipped. Reason: It has no full name.");
            }

            FullTypeName = fullTypeName;
            TypeName = type.GetFullNameOrName();
            CTypeName = type.CTypeName();
            KotlinTypeName = TypeDescriptor.GetTypeName(CodeLanguage.Kotlin, false);

            ReturnType = returnType;

            if (ReturnType.IsByRef)
            {
                throw new Exception($"// TODO: ({KotlinTypeName}) Unsupported delegate type. Reason: Has by ref return type");
            }

            IsReturning = !ReturnType.IsVoid();

            ReturnTypeDescriptor = ReturnType.GetTypeDescriptor(typeDescriptorRegistry);

            ReturnTypeIsPrimitive = ReturnType.IsPrimitive;
            ReturnTypeIsReadOnlySpanOfByte = ReturnType.IsReadOnlySpanOfByte();
            ReturnTypeIsOptional = ReturnTypeDescriptor.Nullability == Nullability.Nullable;

            // TODO: This generates inout TypeName if the return type is by ref
            KotlinReturnTypeName = ReturnTypeDescriptor.GetTypeName(
                CodeLanguage.Kotlin,
                true,
                ReturnTypeIsOptional ? Nullability.Nullable : Nullability.NonNullable,
                Nullability.NotSpecified, // TODO
                false,
                false,
                false
            );

            foreach (var parameter in parameterInfos)
            {
                if (parameter.IsOut)
                {
                    throw new Exception($"// TODO: ({KotlinTypeName}) Unsupported delegate type. Reason: Has out parameters");
                }

                if (parameter.IsIn)
                {
                    throw new Exception($"// TODO: ({KotlinTypeName}) Unsupported delegate type. Reason: Has in parameters");
                }

                if (!ExperimentalFeatureFlags.EnableByRefParametersInDelegates)
                {
                    Type parameterType = parameter.ParameterType;

                    if (parameterType.IsByRef)
                    {
                        throw new Exception($"// TODO: ({KotlinTypeName}) Unsupported delegate type. Reason: Has by ref parameters");
                    }
                }
            }

            ParameterInfos = parameterInfos;

            BaseType = type.BaseType;
            BaseTypeDescriptor = BaseType?.GetTypeDescriptor(typeDescriptorRegistry);

            KotlinBaseTypeName = BaseTypeDescriptor?.GetTypeName(CodeLanguage.Kotlin, false)
                                ?? "DNObject";
        }

        public bool DelegateInvokeMethodMatches(MethodInfo? otherDelegateInvokeMethod)
        {
            MethodInfo? delegateInvokeMethod = DelegateInvokeMethod;

            if (otherDelegateInvokeMethod == delegateInvokeMethod)
            {
                return true;
            }

            if (delegateInvokeMethod == null ||
                otherDelegateInvokeMethod == null)
            {
                return false;
            }

            var returnType = delegateInvokeMethod.ReturnType;
            var otherReturnType = otherDelegateInvokeMethod.ReturnType;

            if (returnType != otherReturnType)
            {
                return false;
            }

            var parameterInfos = delegateInvokeMethod.GetParameters();
            var otherParameterInfos = otherDelegateInvokeMethod.GetParameters();

            if (parameterInfos != otherParameterInfos)
            {
                return false;
            }

            return true;
        }
    }

    private string WriteDelegateTypeDefs(
            ISyntaxWriterConfiguration? configuration,
            Type type,
            State state
        )
    {
        var delegateInvokeMethod = type.GetDelegateInvokeMethod();
        var kotlinConfiguration = (configuration as KotlinSyntaxWriterConfiguration)!;
        TypeDescriptorRegistry typeDescriptorRegistry = TypeDescriptorRegistry.Shared;

        if (state.CSharpUnmanagedResult is null)
        {
            throw new Exception("No C# unmanaged result");
        }

        if (state.CResult is null)
        {
            throw new Exception("No C result");
        }

        DelegateTypeInfo typeInfo;

        try
        {
            typeInfo = new(
                type,
                delegateInvokeMethod,
                typeDescriptorRegistry
            );
        }
        catch (Exception ex)
        {
            state.AddSkippedType(type);

            return ex.Message;
        }

        Type? baseType = typeInfo.BaseType;
        MethodInfo? baseTypeDelegateInvokeMethod;

        if (baseType is not null &&
            baseType.IsDelegate())
        {
            baseTypeDelegateInvokeMethod = baseType.GetDelegateInvokeMethod();
        }
        else
        {
            baseTypeDelegateInvokeMethod = null;
        }

        List<string> memberParts = new();

        #region Type Names
        string typeNamesCode = WriteTypeNames(
            typeInfo.TypeName,
            typeInfo.FullTypeName
        );

        memberParts.Add(typeNamesCode);
        #endregion Type Names

        // #region Closure Type Alias
        // string closureTypeAliasCode = WriteClosureTypeAlias(
        //     type,
        //     typeInfo.ParameterInfos,
        //     typeInfo.KotlinReturnTypeName,
        //     typeDescriptorRegistry,
        //     out string closureTypeTypeAliasName
        // );

        // memberParts.Add(closureTypeAliasCode);
        // #endregion Closure Type Alias
        string closureTypeTypeAliasName = "";
        #region Create C Function
        string createCFunctionCode = WriteCreateCFunction(
            type,
            typeInfo.ParameterInfos,
            typeInfo.CTypeName,
            closureTypeTypeAliasName,
            typeInfo.IsReturning,
            typeInfo.ReturnTypeDescriptor,
            typeInfo.ReturnTypeIsOptional,
            typeInfo.ReturnTypeIsPrimitive || typeInfo.ReturnType.IsEnum,
            typeInfo.ReturnTypeIsReadOnlySpanOfByte,
            typeDescriptorRegistry,
            kotlinConfiguration,
            out string createCFunctionFuncName
        );

        memberParts.Add(createCFunctionCode);
        #endregion Create C Function

        #region Create C Destructor Function
        string createCDestructorFunctionCode = WriteCreateCDestructorFunction(
            typeInfo.CTypeName,
            closureTypeTypeAliasName,
            out string createCDestructorFunctionFuncName
        );

        memberParts.Add(createCDestructorFunctionCode);
        #endregion Create C Destructor Function

        #region Invoke
        if (typeInfo.DelegateInvokeMethod is not null)
        {
            string invokeCode = WriteInvoke(
                typeInfo,
                baseTypeDelegateInvokeMethod,
                typeDescriptorRegistry,
                kotlinConfiguration
            );

            memberParts.Add(invokeCode);
        }
        #endregion Invoke

        string memberPartsCode = string.Join("\n\n", memberParts);

        KotlinCodeBuilder kb = new();

        kb.AppendLine(memberPartsCode);
        
        string typeDecl = Builder.Class($"{typeInfo.KotlinTypeName} /* {typeInfo.FullTypeName} */")
            .BaseTypeName(typeInfo.KotlinBaseTypeName)
            .Public()
            .Implementation(kb.ToString())
            .ToString();
        
        var typeDocumentationComment = type.GetDocumentation()
            ?.GetFormattedDocumentationComment();

        KotlinCodeBuilder kbFinal;

        if (!string.IsNullOrEmpty(typeDocumentationComment))
        {
            kbFinal = new(typeDocumentationComment + "\n");
            kbFinal.AppendLine(typeDecl);
        }
        else
        {
            kbFinal = new(typeDecl);
        }

        var final = kbFinal.ToString();

        return final;
    }

    private string WriteTypeNames(
        string typeName,
        string fullTypeName
    )
    {
        string typeNameDecl = Builder.GetOnlyProperty("typeName", "String")
            .Public()
            .Override()
            .Implementation($"\"{typeName}\"")
            .ToString();

        string fullTypeNameDecl = Builder.GetOnlyProperty("fullTypeName", "String")
            .Public()
            .Override()
            .Implementation($"\"{fullTypeName}\"")
            .ToString();

        return "companion object {\n" + $"{typeNameDecl}\n\n{fullTypeNameDecl}".IndentAllLines(1) + "\n}";;
    }

    private string WriteCreateCFunction(
        Type type,
        ParameterInfo[] parameterInfos,
        string cTypeName,
        string closureTypeTypeAliasName,
        bool isReturning,
        TypeDescriptor returnTypeDescriptor,
        bool returnTypeIsOptional,
        bool returnTypeIsPrimitiveOrEnum,
        bool returnValueIsReadOnlySpanOfByte,
        TypeDescriptorRegistry typeDescriptorRegistry,
        KotlinSyntaxWriterConfiguration? configuration,
        out string createCFunctionFuncName
    )
    {
        string cFunctionParameters = KotlinMethodSyntaxWriter.WriteParameters(
            MemberKind.Method,
            null,
            Nullability.NotSpecified,
            Nullability.NotSpecified,
            false,
            type,
            parameterInfos,
            false,
            Array.Empty<Type>(),
            true,
            false,
            configuration,
            typeDescriptorRegistry
        );

        string innerContextParameterName = "__innerContext";

        if (string.IsNullOrEmpty(cFunctionParameters))
        {
            cFunctionParameters = innerContextParameterName;
        }
        else
        {
            cFunctionParameters = $"{innerContextParameterName}, {cFunctionParameters}";
        }

        string fatalErrorMessageIfNoContext = "Context is nil";

        string innerSwiftContextVarName = "__innerSwiftContext";
        createCFunctionFuncName = "__createCFunction";
        string innerClosureVarName = "__innerClosure";

        KotlinCodeBuilder kb = new();

        kb.AppendLine($"return {{ {cFunctionParameters} in");
        kb.AppendLine($"\tguard let {innerContextParameterName} else {{ fatalError(\"{fatalErrorMessageIfNoContext}\") }}");
        kb.AppendLine();

        kb.AppendLine(Builder.Val(innerSwiftContextVarName)
            .Value($"NativeBox<{closureTypeTypeAliasName}>.fromPointer({innerContextParameterName})")
            .ToIndentedString(1));

        kb.AppendLine(Builder.Val(innerClosureVarName)
            .Value($"{innerSwiftContextVarName}.value")
            .ToIndentedString(1));

        kb.AppendLine();

        string parameterConversionsToKotlin = KotlinMethodSyntaxWriter.WriteParameterConversions(
            CodeLanguage.Kotlin,
            CodeLanguage.KotlinJNA,
            MemberKind.Method,
            null,
            Nullability.NotSpecified,
            Nullability.NotSpecified,
            parameterInfos,
            false,
            Array.Empty<Type>(),
            Array.Empty<Type>(),
            configuration,
            typeDescriptorRegistry,
            out List<string> convertedParameterNamesToSwift,
            out _,
            out _,
            out List<string> parameterBackConversionCodes
        );

        kb.AppendLine(parameterConversionsToKotlin
            .IndentAllLines(1));

        string returnValueName = "__returnValueSwift";

        string returnValueStorage = isReturning
            ? $"let {returnValueName} = "
            : string.Empty;

        string allParameterNamesString = string.Join(", ", convertedParameterNamesToSwift);

        string invocation = $"{returnValueStorage}{innerClosureVarName}({allParameterNamesString})";

        kb.AppendLine($"\t{invocation}");
        kb.AppendLine();

        if (parameterBackConversionCodes.Count > 0)
        {
            foreach (var backConversionCode in parameterBackConversionCodes)
            {
                var indentedBackConversionCode = backConversionCode
                    .IndentAllLines(1);

                kb.AppendLine(indentedBackConversionCode);
            }

            kb.AppendLine();
        }

        string returnCode = string.Empty;

        if (isReturning)
        {
            string? returnTypeConversion = returnTypeDescriptor.GetTypeConversion(
                CodeLanguage.Kotlin,
                CodeLanguage.KotlinJNA,
                Nullability.NotSpecified // TODO
            );

            if (!string.IsNullOrEmpty(returnTypeConversion))
            {
                string newReturnValueName = "__returnValue";

                string returnTypeOptionalString = returnTypeIsOptional
                    ? "?"
                    : string.Empty;

                string fullReturnTypeConversion = Builder.Val(newReturnValueName)
                    .Value(string.Format(returnTypeConversion, $"{returnValueName}{returnTypeOptionalString}"))
                    .ToIndentedString(1);

                kb.AppendLine(fullReturnTypeConversion);

                if (!returnTypeIsPrimitiveOrEnum &&
                    !returnValueIsReadOnlySpanOfByte)
                {
                    string nullabilitySpecifier = returnTypeIsOptional
                        ? "?"
                        : string.Empty;

                    kb.AppendLine($"\t{returnValueName}{nullabilitySpecifier}.__destroyMode = .skip // Will be destroyed by .NET");
                }

                kb.AppendLine();

                returnValueName = newReturnValueName;
            }

            returnCode = $"return {returnValueName}";
        }

        if (isReturning)
        {
            kb.AppendLine($"\t{returnCode}");
        }

        kb.AppendLine("}");

        string code = Builder.Fun(createCFunctionFuncName)
            .Private()
            .ReturnTypeName($"{cTypeName}_CFunction_t")
            .Implementation(kb.ToString())
            .ToString();

        return code;
    }

    private string WriteCreateCDestructorFunction(
        string cTypeName,
        string closureTypeTypeAliasName,
        out string createCDestructorFunctionFuncName
    )
    {
        KotlinCodeBuilder kb = new();

        string innerContextParameterName = "__innerContext";
        string fatalErrorMessageIfNoContext = "Context is nil";

        createCDestructorFunctionFuncName = "__createCDestructorFunction";

        kb.AppendLine($"return {{ {innerContextParameterName} in");
        kb.AppendLine($"\tguard let {innerContextParameterName} else {{ fatalError(\"{fatalErrorMessageIfNoContext}\") }}");
        kb.AppendLine();
        kb.AppendLine($"\tNativeBox<{closureTypeTypeAliasName}>.release({innerContextParameterName})");
        kb.AppendLine("}");

        string code = Builder.Fun(createCDestructorFunctionFuncName)
            .Private()
            .ReturnTypeName($"{cTypeName}_CDestructorFunction_t")
            .Implementation(kb.ToString())
            .ToString();

        return code;
    }

    private string WriteInvoke(
        DelegateTypeInfo typeInfo,
        MethodInfo? baseTypeDelegateInvokeMethod,
        TypeDescriptorRegistry typeDescriptorRegistry,
        KotlinSyntaxWriterConfiguration? configuration
    )
    {
        KotlinCodeBuilder kb = new();

        string kotlinFunParameters = KotlinMethodSyntaxWriter.WriteParameters(
            MemberKind.Method,
            null,
            Nullability.NotSpecified,
            Nullability.NotSpecified,
            false,
            typeInfo.Type,
            typeInfo.ParameterInfos,
            false,
            Array.Empty<Type>(),
            false,
            false,
            configuration,
            typeDescriptorRegistry
        );

        bool isOverride = typeInfo.DelegateInvokeMethodMatches(baseTypeDelegateInvokeMethod);

        string swiftFuncDecl = Builder.Fun("invoke")
            .Public()
            .Override(isOverride)
            .Parameters(kotlinFunParameters)
            .ReturnTypeName(typeInfo.KotlinReturnTypeName)
            .ToString();

        kb.AppendLine($"{swiftFuncDecl} {{");

        string exceptionCVarName = "__exceptionC";

        kb.AppendLine(Builder.Var(exceptionCVarName)
            .TypeName("System_Exception_t?")
            .ToIndentedString(1));

        kb.AppendLine();

        string selfConvertedVarName = "__selfC";

        kb.AppendLine(Builder.Val(selfConvertedVarName)
            .Value("self.__handle")
            .ToIndentedString(1));

        kb.AppendLine();

        string parameterConversions = KotlinMethodSyntaxWriter.WriteParameterConversions(
            CodeLanguage.Kotlin,
            CodeLanguage.KotlinJNA,
            MemberKind.Method,
            null,
            Nullability.NotSpecified,
            Nullability.NotSpecified,
            typeInfo.ParameterInfos,
            false,
            Array.Empty<Type>(),
            Array.Empty<Type>(),
            configuration,
            typeDescriptorRegistry,
            out List<string> convertedParameterNamesToC,
            out _,
            out _,
            out _
        );

        convertedParameterNamesToC.Insert(0, selfConvertedVarName);
        convertedParameterNamesToC.Add($"&{exceptionCVarName}");

        string allParameterNamesString = string.Join(", ", convertedParameterNamesToC);

        kb.AppendLine(parameterConversions
            .IndentAllLines(1));

        string returnValueName = "__returnValueC";

        string returnValueStorage = typeInfo.IsReturning
            ? $"let {returnValueName} = "
            : string.Empty;


        string cInvokeMethodName = $"{typeInfo.CTypeName}_Invoke";

        string invocation = $"{returnValueStorage}{cInvokeMethodName}({allParameterNamesString})";

        kb.AppendLine($"\t{invocation}");
        kb.AppendLine();

        string returnCode = string.Empty;

        if (typeInfo.IsReturning)
        {
            string? returnTypeConversion = typeInfo.ReturnTypeDescriptor.GetTypeConversion(
                CodeLanguage.KotlinJNA,
                CodeLanguage.Kotlin,
                Nullability.NotSpecified // TODO
            );

            if (!string.IsNullOrEmpty(returnTypeConversion))
            {
                string newReturnValueName = "__returnValue";

                string fullReturnTypeConversion = Builder.Val(newReturnValueName)
                    .Value(string.Format(returnTypeConversion, $"{returnValueName}"))
                    .ToIndentedString(1);

                kb.AppendLine(fullReturnTypeConversion);
                kb.AppendLine();

                returnValueName = newReturnValueName;
            }

            returnCode = $"return {returnValueName}";
        }

        kb.AppendLine("""
    if let __exceptionC {
        let __exception = System_Exception(handle: __exceptionC)
        let __error = __exception.swiftError
        
        throw __error
    }
""");

        kb.AppendLine();

        if (typeInfo.IsReturning)
        {
            kb.AppendLine($"\t{returnCode}");
        }

        kb.AppendLine("}");

        string code = kb.ToString();

        return code;
    }
}