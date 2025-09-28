using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
internal class SystemSourceGenerator : ISourceGenerator
{
    private struct System
    {
        public enum Type { Pre, Upd, Fix, Lat, Drw, Cln }

        public Type type;
        public INamedTypeSymbol symbol;
        public string name;
        public string fieldName;
    }
    
    public void Execute(GeneratorExecutionContext context)
    {
        if(context.Compilation.AssemblyName != "Assembly-CSharp") {
            return;
        }

        if (!(context.SyntaxContextReceiver is SystemSyntaxReceiver receiver))
        {
            return;
        }

        foreach (var system in receiver.found)
        {
            var feature = system.symbol.ContainingType.ToDisplayString(nameOnly);
            var source = GenerateSystemSource(system);
            
            var name = system.symbol.ToDisplayString(nameOnly);
            context.AddSource($"{feature}.{name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
        
        foreach (var g in receiver.found.GroupBy(x => x.symbol.ContainingType, SymbolEqualityComparer.Default))
        {
            var source = GenerateFeatureSource(g.Key, g.ToList());
            var name = g.Key.ToDisplayString(nameOnly);
            context.AddSource($"{name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private string GenerateFeatureSource(ISymbol feature, List<System> systems)
    {
        var namespaceBegin = feature.ContainingNamespace.IsGlobalNamespace ? ""
            : "namespace " + feature.ContainingNamespace.ToDisplayString() + " {";
        var namespaceEnd = feature.ContainingNamespace.IsGlobalNamespace ? "" : "}";
        var featureName = feature.ToDisplayString(nameOnly);
        
        var s = new StringBuilder();
        
        s.Append($@"using Scellecs.Morpeh;
using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

{namespaceBegin}
public partial class {featureName} : Feature {{
");
        GenerateFieldList(systems, s);
        GenerateConstructor(featureName, systems, s);
        GeneratePreUpdate(systems, s);
        GenerateUpdate(systems, s);
        GenerateFixedUpdate(systems, s);
        GenerateLateUpdate(systems, s);
        GenerateDrawUpdate(systems, s);
        GenerateCleanupUpdate(systems, s);
        s.Append($@"
}}
{namespaceEnd}
");
        return s.ToString();
    }

    private void GenerateCleanupUpdate(List<System> systems, StringBuilder s)
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
    
    private void GenerateLateUpdate(List<System> systems, StringBuilder s)
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

    private void GenerateDrawUpdate(List<System> systems, StringBuilder s)
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

    
    private void GenerateFixedUpdate(List<System> systems, StringBuilder s)
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
    
    private void GenerateUpdate(List<System> systems, StringBuilder s)
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

    private void GeneratePreUpdate(List<System> systems, StringBuilder s)
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

    
    private void GenerateConstructor(string feature, List<System> systems, StringBuilder s)
    {
        s.Append($@"
public {feature}() {{
");
        foreach (var sys in systems)
        {
            s.Append($"{sys.fieldName} = new {sys.name}();\n");
            s.Append($"{sys.fieldName}.Init();\n");
        }
        s.Append("}\n");
    }

    private void GenerateFieldList(List<System> systems, StringBuilder s)
    {
        foreach (var sys in systems)
        {
            s.Append($"public {sys.name} {sys.fieldName};\n");
        }
    }


    private static readonly SymbolDisplayFormat nameOnly = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    private string GenerateSystemSource(System system)
    {
        var symbol = system.symbol;
        var feature = symbol.ContainingType.ToDisplayString(nameOnly);

        var namespaceBegin = symbol.ContainingNamespace.IsGlobalNamespace ? ""
            : "namespace " + symbol.ContainingNamespace.ToDisplayString() + " {";
        var namespaceEnd = symbol.ContainingNamespace.IsGlobalNamespace ? "" : "}";
        
        var s = new StringBuilder();
        s.Append($@"using Scellecs.Morpeh;

{namespaceBegin}
public partial class {feature} {{
public partial class {system.name} {{
}}
}}
{namespaceEnd}
");
        return s.ToString();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SystemSyntaxReceiver());
    }

    private class SystemSyntaxReceiver : ISyntaxContextReceiver
    {
        public readonly List<System> found = new List<System>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax decl) || decl.AttributeLists.Count == 0) return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(decl) as INamedTypeSymbol;
            if(symbol == null) return;
            
            foreach (var attr in symbol.GetAttributes())
            {
                var name = symbol.ToDisplayString(nameOnly);
                var sys = new System() {
                    symbol = symbol,
                    name = name,
                    fieldName = "@" + name.FirstCharToLowerCase(),
                };
                switch (attr.AttributeClass.Name)
                {
                    case "PreAttribute":
                        sys.type = System.Type.Pre;
                        found.Add(sys);
                        return;
                    case "UpdAttribute":
                        sys.type = System.Type.Upd;
                        found.Add(sys);
                        return;
                    case "FixAttribute":
                        sys.type = System.Type.Fix;
                        found.Add(sys);
                        return;
                    case "LatAttribute":
                        sys.type = System.Type.Lat;
                        found.Add(sys);
                        return;
                    case "DrwAttribute":
                        sys.type = System.Type.Drw;
                        found.Add(sys);
                        return;
                    case "ClnAttribute":
                        sys.type = System.Type.Cln;
                        found.Add(sys);
                        return;
                }
            }
        }
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