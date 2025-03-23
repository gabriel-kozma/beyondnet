using System.Reflection;

using Beyond.NET.CodeGenerator.Extensions;
using Beyond.NET.CodeGenerator.Types;

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
        internal string SwiftTypeName { get; }

        internal Type? BaseType { get; }
        internal TypeDescriptor? BaseTypeDescriptor { get; }
        internal string SwiftBaseTypeName { get; }

        internal Type ReturnType { get; }
        internal TypeDescriptor ReturnTypeDescriptor { get; }
        internal bool IsReturning { get; }
        internal bool ReturnTypeIsPrimitive { get; }
        internal bool ReturnTypeIsOptional { get; }
        internal bool ReturnTypeIsReadOnlySpanOfByte { get; }
        internal string SwiftReturnTypeName { get; }

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
            SwiftTypeName = TypeDescriptor.GetTypeName(CodeLanguage.Swift, false);

            ReturnType = returnType;

            if (ReturnType.IsByRef)
            {
                throw new Exception($"// TODO: ({SwiftTypeName}) Unsupported delegate type. Reason: Has by ref return type");
            }

            IsReturning = !ReturnType.IsVoid();

            ReturnTypeDescriptor = ReturnType.GetTypeDescriptor(typeDescriptorRegistry);

            ReturnTypeIsPrimitive = ReturnType.IsPrimitive;
            ReturnTypeIsReadOnlySpanOfByte = ReturnType.IsReadOnlySpanOfByte();
            ReturnTypeIsOptional = ReturnTypeDescriptor.Nullability == Nullability.Nullable;

            // TODO: This generates inout TypeName if the return type is by ref
            SwiftReturnTypeName = ReturnTypeDescriptor.GetTypeName(
                CodeLanguage.Swift,
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
                    throw new Exception($"// TODO: ({SwiftTypeName}) Unsupported delegate type. Reason: Has out parameters");
                }

                if (parameter.IsIn)
                {
                    throw new Exception($"// TODO: ({SwiftTypeName}) Unsupported delegate type. Reason: Has in parameters");
                }

                if (!ExperimentalFeatureFlags.EnableByRefParametersInDelegates)
                {
                    Type parameterType = parameter.ParameterType;

                    if (parameterType.IsByRef)
                    {
                        throw new Exception($"// TODO: ({SwiftTypeName}) Unsupported delegate type. Reason: Has by ref parameters");
                    }
                }
            }

            ParameterInfos = parameterInfos;

            BaseType = type.BaseType;
            BaseTypeDescriptor = BaseType?.GetTypeDescriptor(typeDescriptorRegistry);

            SwiftBaseTypeName = BaseTypeDescriptor?.GetTypeName(CodeLanguage.Swift, false)
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
        Type type,
        State state,
        KotlinSyntaxWriterConfiguration configuration
    )
    {
        var delegateInvokeMethod = type.GetDelegateInvokeMethod();

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
        return "";
    }
}