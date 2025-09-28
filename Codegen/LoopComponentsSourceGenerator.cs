using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
internal class LoopComponentsSourceGenerator : ISourceGenerator
{
    private enum Type { Cmp, Evt, UCmp }

    private struct Cmp {
        public Type type;
        public string name;
        public bool customSave;
    }
    
    public void Execute(GeneratorExecutionContext context)
    {
        if(context.Compilation.AssemblyName != "Assembly-CSharp") return;
        if(!(context.SyntaxContextReceiver is ComponentSyntaxReceiver receiver)) return;

        var src = GenerateEventsAndComponents(receiver.foundCmp);
        var entityProviderSrc = this.GenerateDeserialization(receiver.foundCmp);
        context.AddSource("MorpehLoop.Events.g.cs", SourceText.From(src, Encoding.UTF8));
        context.AddSource("EntityProvider.Deserialization.g.cs", SourceText.From(entityProviderSrc, Encoding.UTF8));
    }
    
    private string GenerateDeserialization(List<Cmp> receiverFoundCmp) {
        var s = new StringBuilder();
        s.Append("""
                 using Scellecs.Morpeh;
                 using System;
                 using System.Runtime.CompilerServices;
                 using Unity.IL2CPP.CompilerServices;
                 using System.IO;
                 
                 public static class AllComponents {

                 [MethodImpl(MethodImplOptions.AggressiveInlining)]
                 [Il2CppSetOption(Option.NullChecks, false)]
                 public static void SetComponent(Entity entity, IComponent comp) {
                     switch(comp) {
                    
                 """);
        foreach (var cmp in receiverFoundCmp) {
            s.Append($"        case {cmp.name} x: World.Default.GetStash<{cmp.name}>().Set(entity, x); return;\n");
        }
        s.Append("""
                     }
                 }
                 }
                 """);
        return s.ToString();
    }

    private string GenerateEventsAndComponents(List<Cmp> list) {
        var s = new StringBuilder();
        s.Append("""
                   using Scellecs.Morpeh;
                   using System;
                   using System.Runtime.CompilerServices;
                   using Unity.IL2CPP.CompilerServices;
                   using System.IO;

                   public partial class MorpehLoop {

                   [MethodImpl(MethodImplOptions.AggressiveInlining)]
                   [Il2CppSetOption(Option.NullChecks, false)]
                   public void InitStashes() {
                   
                   """);
        foreach(var c in list) {
            s.Append($"{c.name}.stash = World.Default.GetStash<{c.name}>();\n");
        }
        s.Append("""
                     }

                 [MethodImpl(MethodImplOptions.AggressiveInlining)]
                 [Il2CppSetOption(Option.NullChecks, false)]
                 [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
                 public void Save(BinaryWriter w) {
                    
                 """);

        for(var i = 0; i < list.Count; i++) {
            var c = list[i];
            if(c.name == "UPrefab") continue;
            if(c.type != Type.Cmp && !c.customSave) continue;

            if(c.type == Type.UCmp) {
                s.Append($$"""
                           foreach(var x in {{c.name}}.stash.Ids) {
                               w.Write(x);
                               {{c.name}}.stash.GetById(x).Save(w);
                           }
                           """);
            } else {
                s.AppendLine($"var data{i} = {c.name}.stash.data;");
                s.Append($$"""
                           w.Write(data{{i}}.Length);
                           w.Write({{c.name}}.stash.Length);
                           {{c.name}}.stash.Save(w);
                           foreach(ref var x in {{c.name}}.stash) {
                               x.Save(w);
                           }

                           """);
            }
        }

        s.Append("""
                 }

                 [MethodImpl(MethodImplOptions.AggressiveInlining)]
                 [Il2CppSetOption(Option.NullChecks, false)]
                 [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
                 public void Load(BinaryReader r) {
                 
                 """);

        for(var i = 0; i < list.Count; i++) {
            var c = list[i];
            if(c.name == "UPrefab") continue;
            if(c.type != Type.Cmp && !c.customSave) continue;

            if(c.type == Type.UCmp) {
                s.AppendLine($"for(var i = 0; i < {c.name}.stash.Length; i++) {{");
                s.AppendLine($"    {c.name}.stash.GetById(r.ReadInt32()).Load(r);");
                s.AppendLine("}");
            } else {
                s.AppendLine($"{c.name}.stash.Load(r, r.ReadInt32());");
                s.AppendLine($"foreach(ref var x in {c.name}.stash) {{");
                s.AppendLine($"    x.Load(r);");
                s.AppendLine("}");
            }
        }

        s.Append("""

                 }


                 [MethodImpl(MethodImplOptions.AggressiveInlining)]
                 [Il2CppSetOption(Option.NullChecks, false)]
                 public void CleanupEvents() {

                 """);
        foreach(var e in list) {
            if(e.type != Type.Evt) continue;
            s.Append($"{e.name}.stash.RemoveAll();\n");
        }
        s.Append("""

                    World.Default.Commit();
                    }}

                    """);
        return s.ToString();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ComponentSyntaxReceiver());
    }

    private class ComponentSyntaxReceiver : ISyntaxContextReceiver
    {
        public readonly List<Cmp> foundCmp = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!(context.Node is StructDeclarationSyntax decl) || decl.AttributeLists.Count == 0) return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(decl) as INamedTypeSymbol;
            if(symbol == null) return;
            
            foreach (var attr in symbol.GetAttributes())
            {
                switch (attr.AttributeClass!.Name)
                {
                    case "CmpAttribute":
                        foundCmp.Add(new Cmp {
                            type = Type.Cmp,
                            name = symbol.ToDisplayString(),
                            customSave = HasCustomSave(symbol),
                        });
                        return;
                    case "UCmpAttribute":
                        foundCmp.Add(new Cmp {
                            type = Type.UCmp,
                            name = symbol.ToDisplayString(),
                            customSave = HasCustomSave(symbol),
                        });
                        return;
                    case "EvtAttribute":
                        foundCmp.Add(new Cmp {
                            type = Type.Evt,
                            name = symbol.ToDisplayString()
                        });
                        return;
                }
            }
        }
        
        private bool HasCustomSave(INamedTypeSymbol symbol) {
            var save = false;
            var load = false;
            foreach (var name in symbol.MemberNames) {
                if(name == "Save") save = true;
                if(name == "Load") load = true;
            }
            
            return save && load;
        }
    }
}