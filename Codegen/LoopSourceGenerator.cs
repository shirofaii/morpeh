using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
internal class LoopSourceGenerator : ISourceGenerator
{
    private struct Feature
    {
        public IFieldSymbol symbol;
        public string fieldName;
    }
    
    private List<Feature> features;
    
    public void Execute(GeneratorExecutionContext context)
    {
        if(context.Compilation.AssemblyName != "Assembly-CSharp") return;

        var loop = context.Compilation.GetTypeByMetadataName("MorpehLoop");
        if (loop == null) return;

        features = new List<Feature>();
        
        foreach (var f in loop.GetMembers())
        {
            if(f.Kind != SymbolKind.Field) continue;
            
            var symbol = (IFieldSymbol)f;
            
            features.Add(new Feature()
            {
                symbol = symbol,
                fieldName = f.Name,
            });
        }
            
        var source = GenerateSource();
        context.AddSource($"MorpehLoop.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private string GenerateSource()
    {
        var s = new StringBuilder();
        s.Append($@"using Scellecs.Morpeh;
using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

public partial class MorpehLoop {{
");
        GenerateInit(s);
        GeneratePreUpdate(s);
        GenerateUpdate(s);
        GenerateFixedUpdate(s);
        GenerateLateUpdate(s);
        GenerateDrawUpdate(s);
        GenerateCleanupUpdate(s);
        s.Append($@"
}}
");
        return s.ToString();
    }
    
    private void GeneratePreUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void PreUpdate() {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.PreUpdate();\n");
        }
        s.Append("}\n");
    }
    
    private void GenerateCleanupUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void CleanupUpdate() {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.CleanupUpdate();\n");
        }
        s.Append("CleanupEvents();\n");
        s.Append("}\n");
    }
    
    private void GenerateLateUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void LateUpdate() {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.LateUpdate();\n");
        }
        s.Append("}\n");
    }

    private void GenerateDrawUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void DrawUpdate(Camera cam) {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.DrawUpdate(cam);\n");
        }
        s.Append("}\n");
    }

    
    private void GenerateFixedUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void FixedUpdate() {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.FixedUpdate();\n");
        }
        s.Append("}\n");
    }
    
    private void GenerateUpdate(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void Update() {
");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.Update();\n");
        }
        s.Append("}\n");
    }
     
    private void GenerateInit(StringBuilder s)
    {
        s.Append(@"
[MethodImpl(MethodImplOptions.AggressiveInlining)]
[Il2CppSetOption(Option.NullChecks, false)]
public void Init() {

InitStashes();

");
        foreach (var f in features)
        {
            s.Append($"{f.fieldName} = new();\n");
        }
        foreach (var f in features)
        {
            s.Append($"{f.fieldName}.Init();\n");
        }
        
        s.Append("}\n");
    }

    private static readonly SymbolDisplayFormat nameOnly = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
