using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

[Generator]
internal class SaveComponentsSourceGenerator : IIncrementalGenerator
{
    private enum Kind { Simple, Array, List, Enum }
    private record Type {
        public string name;
        public Kind kind;
        public Type elementType;
    }

    private record Field {
        public Type type;
        public string fieldName;
    }
    private record Model {
        public string @namespace;
        public string name;
        public string parent;
        public EqArray<Field> fields;
        
        public string dotParent => parent == null ? "" : $"{parent}.";
    }  

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var cmpDecls = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "CmpAttribute",
            predicate: static (node, _) => node is StructDeclarationSyntax,
            transform: static (context, _) => Parse(context));

        context.RegisterSourceOutput(cmpDecls, SourceOutput);
    }
    
    private static void SourceOutput(SourceProductionContext context, Model model) {
        var sourceText = SourceText.From(Generate(model), Encoding.UTF8);
        context.AddSource($"{model.dotParent}{model.name}.SaveLoad.g.cs", sourceText);
    }

    private static Model Parse(GeneratorAttributeSyntaxContext context) {
        var structDecl = (StructDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(structDecl)!;
        var ns = symbol.ContainingNamespace;

        var fields = structDecl.Members
            .OfType<FieldDeclarationSyntax>()
            .Select(x => (x.Declaration.Variables, TypeInfo: context.SemanticModel.GetTypeInfo(x.Declaration.Type)))
            .SelectMany(pair => pair.Variables.Select(x => {
                return new Field {
                    fieldName = x.Identifier.ToString(),
                    type = GetFieldType(pair.TypeInfo.Type),
                };
            })).ToArray();
        
        return new Model {
            @namespace = ns.IsGlobalNamespace ? "" : ns.ToDisplayString(),
            name = symbol.Name,
            fields = fields,
            parent = symbol.ContainingType?.Name,
        };
    }
    
    private static Type GetFieldType(ITypeSymbol type) {
        if(type is IArrayTypeSymbol array) {
            return new Type {
                name = type.Name,
                kind = Kind.Array,
                elementType = GetFieldType(array.ElementType),
            };
        }

        if(type is not INamedTypeSymbol ntype) return null;

        if(type.TypeKind == TypeKind.Enum) {
            return new Type {
                name = type.Name,
                kind = Kind.Enum,
                elementType = GetFieldType(ntype.EnumUnderlyingType),
            };
        }
        
        if(ntype.IsGenericType && ntype.TypeArguments.Length == 1 && ntype.Name.StartsWith("List")) {
            return new Type {
                name = type.Name,
                kind = Kind.List,
                elementType = GetFieldType(ntype.TypeArguments[0]),
            };
        }
        
        return new Type {
            name = type.Name,
            kind = Kind.Simple,
        };
    }

    private static string Generate(Model m) {
        var s = new StringBuilder();
        
        var namespaceBegin = m.@namespace == "" ? ""
            : "namespace " + m.@namespace + " {\n";
        var namespaceEnd = m.@namespace == "" ? "" : "}";

        var parentBegin = m.parent == null ? "" : $"partial struct {m.parent}" + " {\n";
        var parentEnd = m.parent == null ? "" : "}";

        
        s.Append("""
                  using Scellecs.Morpeh;
                  using System;
                  using System.Runtime.CompilerServices;
                  using Unity.IL2CPP.CompilerServices;
                  using System.IO;
                  using UnityEngine;
                  
                  """);
        s.Append(namespaceBegin);
        s.Append(parentBegin);
        s.Append($$"""
                   partial struct {{m.name}} {
                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
                   public void Save(BinaryWriter w) {
                   
                   """);

        GenSave(s, m);
        
        s.Append($$"""
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
                   public void Load(BinaryReader r) {
                   
                   """);
        
        GenLoad(s, m);
        
        s.Append("}}");
        s.Append(parentEnd);
        s.Append(namespaceEnd);
        
        return s.ToString();
    }

    private static void GenSave(StringBuilder s, Model m) {
        foreach(var field in m.fields) {
            SaveType(s, field.type, field.fieldName);
        }
    }

    private static void GenLoad(StringBuilder s, Model m) {
        foreach(var field in m.fields) {
            LoadType(s, field.type, field.fieldName);
        }
    }
  
    private static void SaveType(StringBuilder s, Type type, string name, int it = 0) {
        if(type == null) return;
        if(type.kind == Kind.Array) {
            s.AppendLine($"w.Write({name}.Length);");
            s.AppendLine($"foreach(var x{it} in {name}) {{");
            SaveType(s, type.elementType, "x"+it, it+1);
            s.AppendLine("}");
        }
        if(type.kind == Kind.List) {
            s.AppendLine($"w.Write({name}.Count);");
            s.AppendLine($"foreach(var x{it} in {name}) {{");
            SaveType(s, type.elementType, "x"+it, it+1);
            s.AppendLine("}");
        }
        if(type.kind == Kind.Enum) {
            s.AppendLine($"w.Write(({type.elementType.name}){name});");
        }
        
        if(type.kind == Kind.Simple) {
             switch(type.name) {
                 case "Boolean":
                 case "Int32":
                 case "UInt32":
                 case "Single":
                 case "Double":
                 case "Decimal":
                 case "Int64":
                 case "UInt64":
                 case "Int16":
                 case "UInt16":
                 case "Byte":
                 case "SByte":
                 case "Char":
                 case "String":
                     s.AppendLine($"w.Write({name});");
                     return;
                 case "Entity":
                     s.AppendLine($"w.Write({name}.id);");
                     return;
                 case "Vector3":
                 case "Vector3Int":
                     s.AppendLine($"w.Write({name}.x);");
                     s.AppendLine($"w.Write({name}.y);");
                     s.AppendLine($"w.Write({name}.z);");
                     return;
                 case "Vector2":
                 case "Vector2Int":
                     s.Append($"w.Write({name}.x);");
                     s.Append($"w.Write({name}.y);");
                     return;
             }
        }
    }

    private static void LoadType(StringBuilder s, Type type, string name, int it = 0, Type castTo = null) {
        if(type.kind == Kind.Array) {
            s.AppendLine($"var len = r.ReadInt32();");
            s.AppendLine($"{name} = new {type.elementType.name}[len];");
            s.AppendLine($"for(var i{it} = 0; i{it} < len; i{it}++) {{");
            LoadType(s, type.elementType, $"{name}[i{it}]", it+1);
            s.AppendLine($"}}\n");
        }
        if(type.kind == Kind.List) {
            s.AppendLine($"var len = r.ReadInt32();");
            s.AppendLine($"{name} = new(len);");
            s.AppendLine($"for(var i{it} = 0; i{it} < len; i{it}++) {{");
            LoadType(s, type.elementType, $"{name}[i{it}]", it+1);
            s.AppendLine($"}}");
        }
        if(type.kind == Kind.Enum) {
            LoadType(s, type.elementType, name, castTo: type);
        }
        if(type.kind == Kind.Simple) {
            switch(type.name) {
                case "Boolean":
                case "Int32":
                case "UInt32":
                case "Single":
                case "Double":
                case "Decimal":
                case "Int64":
                case "UInt64":
                case "Int16":
                case "UInt16":
                case "Byte":
                case "SByte":
                case "Char":
                case "String":
                    var cast = castTo != null ? $"({castTo.name})" : "";
                    s.Append($"{name} = {cast}r.Read{type.name}();\n");
                    return;
                case "Entity":
                    s.Append($"{name} = r.ReadEntity();\n");
                    return;
                case "Vector3":
                    s.Append($"{name} = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());\n");
                    return;
                case "Vector3Int":
                    s.Append($"{name} = new Vector3Int(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());\n");
                    return;
                case "Vector2":
                    s.Append($"{name} = new Vector2(r.ReadSingle(), r.ReadSingle());\n");
                    return;
                case "Vector2Int":
                    s.Append($"{name} = new Vector2Int(r.ReadInt32(), r.ReadInt32());\n");
                    return;
            }
        }
    }
}