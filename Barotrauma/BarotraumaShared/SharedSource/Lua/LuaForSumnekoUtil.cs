﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Barotrauma.Networking;
using System.Threading.Tasks;
using Barotrauma.Items.Components;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Barotrauma
{
    public static partial class LuaForSumneko
    {
        private static void ExplanAnnotationPrefix(StringBuilder builder) { builder.Append("---"); }
        private static void ExplanAnnotationField(StringBuilder builder, string fieldName, string scriptName) { ExplanAnnotationPrefix(builder); builder.Append($"@field {fieldName} {scriptName}"); }
        private static void ExplanAnnotationProperty(StringBuilder builder, string propertyName, string scriptName) { ExplanAnnotationPrefix(builder); builder.Append($"@field {propertyName} {scriptName}"); }
        private static void ExplanAnnotationReturn(StringBuilder builder, string name) { ExplanAnnotationPrefix(builder); builder.Append($"@return {name}"); }
        private static void ExplanAnnotationParam(StringBuilder builder, string paramName, string scriptType) { ExplanAnnotationPrefix(builder); builder.Append($"@param {paramName} {scriptType}"); }
        private static void ExplanAnnotationUnOperator(StringBuilder builder, string op, string outType) { ExplanAnnotationPrefix(builder); builder.Append($"@operator {op}:{outType}"); }
        private static void ExplanAnnotationBinOperator(StringBuilder builder, string op, string inpType, string outType) { ExplanAnnotationPrefix(builder); builder.Append($"@operator {op}({inpType}):{outType}"); }
        private static void ExplanNewLine(StringBuilder builder, int line = 1) { for (int i = 0; i < line; i++) builder.AppendLine(); }
        private static bool IsParamsParam(ParameterInfo param) => param.GetCustomAttribute<ParamArrayAttribute>(false) != null;
        private static bool IsOptionalParam(ParameterInfo param) => param.GetCustomAttribute<OptionalAttribute>(false) != null;
        private static string MakeNonConflictParam(string name) { if (LuaKeyWords.Contains(name)) return $"luaKey__{name}"; else return name; }
        private static string MakeParamsParam(string name) => name.Substring(0, name.Length - 2); // remove the last two chars '[]'
        private static string MakeOverloadMethodParamsParam(string name) => $"...:{MakeParamsParam(name)}";

        private static void ExplanField(StringBuilder builder, FieldInfo field)
        {
            var metadata = ClassMetadata.Obtain(field.FieldType);
            metadata.CollectAllToGlobal();
            ExplanAnnotationPrefix(builder);
            var tags = new List<string>() { "Field" };
            if (field.IsPublic) { tags.Add("Public"); }
            else if (field.IsPrivate) { tags.Add("Private"); }
            else { tags.Add("NonPublic"); }
            tags.Add(field.IsStatic ? "Static" : "Instance");
            builder.Append('`' + tags.Aggregate((tag1, tag2) => $"{tag1} {tag2}") + '`');
            ExplanNewLine(builder);
            ExplanAnnotationField(builder, field.Name, metadata.LuaScriptName);
        }

        private static void ExplanProperty(StringBuilder builder, PropertyInfo property)
        {
            var metadata = ClassMetadata.Obtain(property.PropertyType);
            metadata.CollectAllToGlobal();

            if (property.GetMethod != null)
            {
                ExplanAnnotationPrefix(builder);
                ExplanMethodModifiers(builder, GetMethodModifiers(property.GetMethod), "Getter");
                ExplanNewLine(builder);
            }

            if (property.SetMethod != null)
            {
                ExplanAnnotationPrefix(builder);
                builder.Append(@"<br/>");
                ExplanMethodModifiers(builder, GetMethodModifiers(property.SetMethod), "Setter");
                ExplanNewLine(builder);
            }

            ExplanAnnotationProperty(builder, property.Name, metadata.LuaScriptName);
        }

        private static void ExplanOverloadMethodStartForGenLuaType(StringBuilder builder) => builder.Append(@"fun(");
        private static void ExplanOverloadMethodStart(StringBuilder builder) => builder.Append(@"---@overload fun(");
        private static void ExplanOverloadMethodEnd(StringBuilder builder, MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
            {
                var metadata = ClassMetadata.Obtain(method.ReturnType);
                metadata.CollectAllToGlobal();
                builder.Append($"):{metadata.LuaScriptName}");
            }
            else
            {
                builder.Append(')');
            }
        }

        private static void ExplanOverloadConstructorEnd(StringBuilder builder, string clrName)
        {
            builder.Append($"):{clrName}");
        }

        private static void ExplanOverloadMethodParam(StringBuilder builder, ParameterInfo parameter)
        {
            var paramName = parameter.Name;
            paramName = MakeNonConflictParam(paramName);
            if (IsOptionalParam(parameter)) { paramName += '?'; }
            var metadata = ClassMetadata.Obtain(parameter.ParameterType);
            metadata.CollectAllToGlobal();
            builder.Append(IsParamsParam(parameter) ? MakeOverloadMethodParamsParam(metadata.LuaScriptName) : $"{paramName}:{metadata.LuaScriptName}");
        }

        private static void ExplanPrimaryMethodEnd(StringBuilder builder, MethodInfo method)
        {
            var metadata = ClassMetadata.Obtain(method.ReturnType);
            metadata.CollectAllToGlobal();
            ExplanAnnotationReturn(builder, metadata.LuaScriptName);
        }

        private static void ExplanPrimaryConstructorEnd(StringBuilder builder, string clrName)
        {
            ExplanAnnotationReturn(builder, clrName);
        }

        private static void ExplanPrimaryMethodParam(StringBuilder builder, ParameterInfo parameter)
        {
            var paramName = parameter.Name;
            paramName = MakeNonConflictParam(paramName);
            if (IsOptionalParam(parameter)) { paramName += '?'; }
            var metadata = ClassMetadata.Obtain(parameter.ParameterType);
            metadata.CollectAllToGlobal();
            ExplanAnnotationParam(builder, IsParamsParam(parameter) ? "..." : paramName, metadata.LuaScriptName);
        }

        private static uint GetMethodModifiers(MethodBase methodBase)
        {
            uint result = 0x00;
            if (methodBase.IsPublic) { result |= 0x01; }
            if (methodBase.IsPrivate) { result |= 0x02; }
            if (methodBase.IsStatic) { result |= 0x04; }
            if (methodBase.IsAbstract) { result |= 0x08; }
            if (methodBase.IsVirtual) { result |= 0x10; }
            return result;
        }

        private static void ExplanMethodModifiers(StringBuilder builder, uint modifiers, string prefix = "Method")
        {
            var tags = new List<string>() { prefix };
            if ((modifiers & 0x01) > 0) { tags.Add("Public"); }
            else if ((modifiers & 0x02) > 0) { tags.Add("Private"); }
            else { tags.Add("NonPublic"); }
            tags.Add(((modifiers & 0x04) > 0) ? "Static" : "Instance");
            if ((modifiers & 0x08) > 0) { tags.Add("Abstract"); }
            if ((modifiers & 0x10) > 0) { tags.Add("Virtual"); }
            builder.Append('`' + tags.Aggregate((tag1, tag2) => $"{tag1} {tag2}") + '`');
        }

        private static void ExplanMethods(StringBuilder builder, string clrName, string table, MethodBase[] methods, string methodName)
        {
            var methodSB = new StringBuilder();
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var paramList = new List<string>();
                var parameters = method.GetParameters();
                for (var j = 0; j < parameters.Length; j++)
                {
                    var parameter = parameters[j];
                    paramList.Add(MakeNonConflictParam(parameter.Name));
                    if (i != methods.Length - 1) // belong to the other overload methods
                    {
                        if (j == 0) ExplanOverloadMethodStart(methodSB);
                        ExplanOverloadMethodParam(methodSB, parameter);
                        if (j != parameters.Length - 1)
                        {
                            methodSB.Append(", ");
                        }
                        else
                        {
                            if (method is MethodInfo)
                            {
                                ExplanOverloadMethodEnd(methodSB, method as MethodInfo);
                            }
                            else
                            {
                                ExplanOverloadConstructorEnd(methodSB, clrName);
                            }

                            ExplanNewLine(methodSB);
                        }
                    }
                    else // the default overload method
                    {
                        ExplanPrimaryMethodParam(methodSB, parameter);
                        ExplanNewLine(methodSB);

                        if (j == parameters.Length - 1)
                        {
                            if (method is MethodInfo)
                            {
                                var mi = method as MethodInfo;
                                if (mi.ReturnType != typeof(void))
                                {
                                    ExplanPrimaryMethodEnd(methodSB, mi);
                                    ExplanNewLine(methodSB);
                                }
                            }
                            else
                            {
                                ExplanPrimaryConstructorEnd(methodSB, clrName);
                                ExplanNewLine(methodSB);
                            }
                        }
                    }
                }

                if (i == methods.Length - 1)
                {
                    builder.Append(methodSB);
                    string methodBaseName = methodName == null ? "" : $".{methodName}";
                    string paramSequence = paramList.ToArray().Aggregate("", (p1, p2) =>
                    {
                        if (p1.Equals("")) return p2;
                        return $"{p1}, {p2}";
                    });
                    builder.Append($"{table}{methodBaseName} = function({paramSequence}) end");
                    ExplanNewLine(builder, 2);
                }
            }
        }
    }
}
