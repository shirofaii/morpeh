using JetBrains.Annotations;
using Scellecs.Morpeh;
// ReSharper disable InconsistentNaming

public static class Templates {
  [SourceTemplate] public static void get<T>(this Entity entity,
    [Macro(Expression = "completeType()", Editable = 1)] T Type,
    [Macro(Expression = "decapitalize($Type$)", Editable = -1)] T lowerType)
  where T : IComponent
  {
      /*$ ref var $lowerType$ = ref $Type$.Get($entity$);
          $END$*/
  }
  
  [SourceTemplate] public static void set<T>(this Entity entity,
    [Macro(Expression = "completeType()", Editable = 1)] T Type,
    [Macro(Expression = "decapitalize($Type$)", Editable = -1)] T lowerType)
  where T : IComponent
  {
    /*$ $Type$.Set($entity$, new $Type$() {$END$});*/
  }

  [SourceTemplate] public static void rem<T>(this Entity entity,
    [Macro(Expression = "completeType()", Editable = 1)] T Type,
    [Macro(Expression = "decapitalize($Type$)", Editable = -1)] T lowerType)
  where T : IComponent
  {
    /*$ $Type$.Remove($entity$);
        $END$*/
  }
  
}