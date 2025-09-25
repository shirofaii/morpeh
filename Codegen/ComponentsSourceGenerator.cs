using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
internal class ComponentsSourceGenerator : IIncrementalGenerator
{
    public record Model {
        public string @namespace;
        public string name;
        public Type type;
        public string parent;
    }  

    public enum Type { Cmp, Evt, UCmp }
    
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var cmpDecls = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "CmpAttribute",
            predicate: static (node, _) => node is StructDeclarationSyntax,
            transform: static (context, _) => Parse(context, Type.Cmp));

        var evtDecls = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "EvtAttribute",
            predicate: static (node, _) => node is StructDeclarationSyntax,
            transform: static (context, _) => Parse(context, Type.Evt));

        var ucmpDecls = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "UCmpAttribute",
            predicate: static (node, _) => node is StructDeclarationSyntax,
            transform: static (context, _) => Parse(context, Type.UCmp));

        context.RegisterSourceOutput(cmpDecls, SourceOutput);
        context.RegisterSourceOutput(evtDecls, SourceOutput);
        context.RegisterSourceOutput(ucmpDecls, SourceOutput);
    }
    
    private static void SourceOutput(SourceProductionContext context, Model model) {
        var sourceText = SourceText.From(Generate(model), Encoding.UTF8);
        if (model.parent != null)
        {
            context.AddSource($"{model.parent}.{model.name}.g.cs", sourceText);
        }
        else
        {
            context.AddSource($"{model.name}.g.cs", sourceText);    
        }
        
    }

    private static Model Parse(GeneratorAttributeSyntaxContext context, Type type) {
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.TargetNode)!;
        var ns = symbol.ContainingNamespace;
        return new Model {
            @namespace = ns.IsGlobalNamespace ? "" : ns.ToDisplayString(),
            name = symbol.Name,
            type = type,
            parent = symbol.ContainingType?.Name,
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
                  
                  """);
        s.Append(namespaceBegin);
        s.Append(parentBegin);
        s.Append($$"""
                   [Serializable] public partial struct {{m.name}} : IComponent{{(m.type == Type.UCmp ? ", IValidatableWithGameObject" : "")}} {
                   public static Stash<{{m.name}}> stash;
                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static ref {{m.name}} Get(Entity entity) {
                       return ref stash.Get(entity);
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static ref {{m.name}} Get(Entity entity, out bool exist) {
                       return ref stash.Get(entity, out exist);
                   }
                    
                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static void Set(Entity entity) {
                       stash.Set(entity);
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static void Set(Entity entity, in {{m.name}} value) {
                       stash.Set(entity, in value);
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static bool Has(Entity entity) {
                       return stash.Has(entity);
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static bool Remove(Entity entity) {
                       return stash.Remove(entity);
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static {{m.name}} First() {
                       return stash.GetEnumerator().Current;
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static bool IsEmpty() {
                       return stash.IsEmpty();
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static bool IsNotEmpty() {
                       return stash.IsNotEmpty();
                   }

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public static void RemoveAll() {
                       stash.RemoveAll();
                   }
                   
                   
                   }

                   """);
        s.Append(parentEnd);
        s.Append(namespaceEnd);
        
        return s.ToString();
    }
}