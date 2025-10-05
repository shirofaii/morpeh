using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
internal class SystemSourceGenerator : IIncrementalGenerator
{
    private record System
    {
        public enum Type { Invalid, Pre, Upd, Fix, Lat, Drw, Cln }

        public Type type;
        public string name;
        public string fieldName;
    }
    
    private record Model {
        public string name;
        public EqArray<System> systems;
        public string @namespace;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var decls = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "FeatureAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (context, _) => Parse(context));
        
        context.RegisterSourceOutput(decls, SourceOutput);
    }
    
    private static Model Parse(GeneratorAttributeSyntaxContext context) {
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.TargetNode)!;
        var ns = symbol.ContainingNamespace;
        return new Model {
            @namespace = ns.IsGlobalNamespace ? "" : ns.ToDisplayString(),
            name       = symbol.Name,
            systems    = GetSystems(symbol),
        };
    }
    private static EqArray<System> GetSystems(INamedTypeSymbol symbol) {
        var list = new List<System>();
        foreach (var member in symbol.GetTypeMembers())
        {
            if (member.TypeKind != TypeKind.Class) continue;
            
            var name = member.ToDisplayString(nameOnly);
            var type = System.Type.Invalid;
            foreach (var attr in member.GetAttributes()) {
                switch (attr.AttributeClass!.Name) {
                    case "PreAttribute": type = System.Type.Pre; break;
                    case "UpdAttribute": type = System.Type.Upd; break;
                    case "FixAttribute": type = System.Type.Fix; break;
                    case "LatAttribute": type = System.Type.Lat; break;
                    case "DrwAttribute": type = System.Type.Drw; break;
                    case "ClnAttribute": type = System.Type.Cln; break;
                }
            }
            if(type == System.Type.Invalid) continue;
            
            list.Add(new System {
                type      = type,
                name      = name,
                fieldName = "@" + name.FirstCharToLowerCase()
            });
        }
        
        return new EqArray<System>(list.ToArray());
    }

    private void SourceOutput(SourceProductionContext context, Model model)
    {
        var featureSource = GenerateFeatureSource(model);
        context.AddSource($"{model.name}.g.cs", SourceText.From(featureSource, Encoding.UTF8));
        
        foreach (var system in model.systems)
        {
            var source = GenerateSystemSource(model, system);
            context.AddSource($"{model.name}.{system.name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private string GenerateFeatureSource(Model model) {
        var namespaceBegin = "";
        var namespaceEnd = "";
        if (model.@namespace != "") {
            namespaceBegin = "namespace " + model.@namespace + " {";
            namespaceEnd = "}";
        }

        var s = new StringBuilder();
        
        s.Append($@"using Scellecs.Morpeh;
using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

{namespaceBegin}
public partial class {model.name} : Feature {{
");
        GenerateFieldList(model.systems, s);
        GenerateConstructor(model, s);
        GeneratePreUpdate(model.systems, s);
        GenerateUpdate(model.systems, s);
        GenerateFixedUpdate(model.systems, s);
        GenerateLateUpdate(model.systems, s);
        GenerateDrawUpdate(model.systems, s);
        GenerateCleanupUpdate(model.systems, s);
        s.Append($@"
}}
{namespaceEnd}
");
        return s.ToString();
    }

    private void GenerateCleanupUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void CleanupUpdate() {
    if(!IsEnabled()) return;
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Cln) continue;

            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.Update();\n");
            s.Append($"World.Default.Commit();\n");
            s.Append("}\n");
        }
        s.Append("}\n");
    }
    
    private void GenerateLateUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void LateUpdate() {
    if(!IsEnabled()) return;
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Lat) continue;
            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.Update();\n");
            s.Append($"World.Default.Commit();\n");
            s.Append("}\n");
        }
        s.Append("}\n");
    }

    private void GenerateDrawUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void DrawUpdate(Camera cam) {
    if(!IsEnabled()) return;
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Drw) continue;
            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.DrawUpdate(cam);\n");
            s.Append("}\n");
        }
        s.Append("}\n");
    }

    
    private void GenerateFixedUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void FixedUpdate() {
    if(!IsEnabled()) return;
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Fix) continue;
            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.Update();\n");
            s.Append($"World.Default.Commit();\n");
            s.Append("}\n");
        }
        s.Append("}\n");
    }
    
    private void GenerateUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void Update() {
    if(!IsEnabled()) return;
    do {
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Upd) continue;
            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.Update();\n");
            s.Append($"World.Default.Commit();\n");
            s.Append("}\n");
        }
        s.Append("} while (UpdateWhile());");
        s.Append("}\n");
    }

    private void GeneratePreUpdate(EqArray<System> systems, StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void PreUpdate() {
    if(!IsEnabled()) return;
");
        foreach (var sys in systems)
        {
            if(sys.type != System.Type.Pre) continue;
            s.Append($"if({sys.fieldName}.IsEnabled()) {{\n");
            s.Append($"{sys.fieldName}.Update();\n");
            s.Append($"World.Default.Commit();\n");
            s.Append("}\n");
        }
        s.Append("}\n");
    }

    
    private void GenerateConstructor(Model model, StringBuilder s)
    {
        s.Append($@"
public {model.name}() {{
");
        foreach (var sys in model.systems)
        {
            s.Append($"{sys.fieldName} = new {sys.name}();\n");
            s.Append($"{sys.fieldName}.Init();\n");
        }
        s.Append("}\n");
    }

    private void GenerateFieldList(EqArray<System> systems, StringBuilder s)
    {
        foreach (var sys in systems)
        {
            s.Append($"public {sys.name} {sys.fieldName};\n");
        }
    }


    private static readonly SymbolDisplayFormat nameOnly = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    private string GenerateSystemSource(Model model, System system)
    {
        var namespaceBegin = "";
        var namespaceEnd   = "";
        if (model.@namespace != "") {
            namespaceBegin = "namespace " + model.@namespace + " {";
            namespaceEnd   = "}";
        }

        var s = new StringBuilder();
        s.Append($@"using Scellecs.Morpeh;

{namespaceBegin}
public partial class {model.name} {{
public partial class {system.name} {{
}}
}}
{namespaceEnd}
");
        return s.ToString();
    }
}


public static class Ext
{
    public static string FirstCharToLowerCase(this string str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
            return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str.Substring(1);

        return str;
    }
    
    public static string FirstCharToUpperCase(this string str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsLower(str[0]))
            return str.Length == 1 ? char.ToUpper(str[0]).ToString() : char.ToUpper(str[0]) + str.Substring(1);

        return str;
    }
}