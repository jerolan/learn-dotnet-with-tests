# Lista de comprobación para refactorizar

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/refactoring-checklist)

Refactorizar es cambiar la estructura del código sin cambiar su comportamiento. Los tests son la prueba de que el comportamiento siguió igual. Una lista de comprobación mantiene el cambio pequeño y frena los "ya que estoy aquí" que convierten una limpieza en una reescritura.

Vamos a tomar un cálculo de descuentos que funciona y limpiarlo con los tests en verde. La solución terminada está en `testing/refactoring-checklist`.

## Cómo funciona

`Discounts.Apply` rechaza porcentajes fuera de 0–100, resta el descuento porcentual y después resta otro cinco por ciento a miembros.

## Cómo probarlo

Usa MSTest para fijar el resultado normal, el descuento adicional para miembros y el porcentaje inválido. Estos ejemplos definen el comportamiento que el refactor debe conservar.

## Escribe primero el test

```csharp
[TestClass]
public sealed class DiscountsTests
{
    [TestMethod]
    public void Apply_WithPercent_SubtractsItFromThePrice()
    {
        Assert.AreEqual(90m, Discounts.Apply(100m, 10, member: false));
    }

    [TestMethod]
    public void Apply_WithMember_AddsTheExtraDiscount()
    {
        Assert.AreEqual(85.5m, Discounts.Apply(100m, 10, member: true));
    }

    [TestMethod]
    public void Apply_WithInvalidPercent_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => Discounts.Apply(100m, 101, member: false));
    }
}
```

Estos tests se escribieron antes del refactor. Este capítulo parte de una implementación existente para verificar que la base está en verde antes de cambiar su estructura.

## Intenta ejecutar el test

Ejecuta `mise exec dotnet@10.0.402 -- dotnet test testing/refactoring-checklist/RefactoringChecklist.sln -m:1`. Los tres tests pasan; un refactor debe empezar en verde.

## Escribe el código mínimo para que el test compile y mira la salida

La app ya compila y pasa, así que no hace falta un cambio solo para compilar. Esta es la implementación deliberadamente sencilla antes del refactor:

```csharp
public static decimal Apply(decimal price, int percent, bool member)
{
    if (percent < 0 || percent > 100)
    {
        throw new ArgumentOutOfRangeException(nameof(percent));
    }

    var d = price - price * percent / 100;

    if (member)
    {
        d = d - d * 0.05m;
    }

    return d;
}
```

## Escribe el código suficiente para que pase

No hace falta agregar código de comportamiento: los tres tests ya pasan. Déjalos sin cambios mientras mejoramos los nombres y las expresiones.

## Refactor

### La lista

Recórrela en orden, ejecutando los tests después de cada cambio:

1. ¿Los tests están en verde antes de tocar nada?
2. ¿Cada nombre dice qué significa el valor?
3. ¿Hay un número o una cadena mágica que merezca un nombre?
4. ¿Hay un cálculo o una condición que se leería mejor de otra forma?
5. ¿Hay duplicación que una extracción pequeña elimine?
6. ¿Los guards están arriba, antes del trabajo?
7. ¿Hay código muerto, una variable sin usar o un comentario que repite el código?
8. ¿Los tests siguen sin cambios y en verde?
9. ¿Es un buen momento para hacer commit?

Si un test tiene que cambiar, detente. Eso es un cambio de comportamiento, no un refactor; necesita su propio ciclo rojo-verde.

### Aplica la lista

La primera pasada es de nombres. `d` no dice qué guarda. Renómbrala a `afterPercent`:

```csharp
var afterPercent = price - price * percent / 100;
```

Ejecuta los tests. Verde.

La siguiente pasada es el número mágico. `0.05m` es el descuento de miembro; dale un nombre:

```csharp
private const decimal MemberDiscount = 0.05m;
```

Úsalo:

```csharp
if (member)
{
    afterPercent = afterPercent - afterPercent * MemberDiscount;
}
```

Verde. La constante también documenta que el descuento es una tasa, no un importe.

El cálculo repite `price` y `afterPercent`. Un factor se lee mejor:

```csharp
var afterPercent = price * (1 - percent / 100m);
```

y para el paso de miembro:

```csharp
afterPercent * (1 - MemberDiscount)
```

Verde. La división usa `100m` para que la aritmética siga siendo decimal; `100` también funcionaría por promoción, pero el sufijo hace explícita la intención.

La rama de miembro ahora es una sola expresión. Una expresión condicional hace que el return se lea como una decisión:

```csharp
return member
    ? afterPercent * (1 - MemberDiscount)
    : afterPercent;
```

Verde. Si la rama tuviera más pasos, un `if` seguiría siendo más claro; la lista no es una regla para usar el ternario en todas partes.

La versión final:

```csharp
public static class Discounts
{
    private const decimal MemberDiscount = 0.05m;

    public static decimal Apply(decimal price, int percent, bool member)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        var afterPercent = price * (1 - percent / 100m);

        return member
            ? afterPercent * (1 - MemberDiscount)
            : afterPercent;
    }
}
```

El guard usa un patrón relacional, `percent is < 0 or > 100`. Dice lo mismo que las dos comparaciones y mantiene la condición en un solo lugar. Es una elección de legibilidad; las dos formas son correctas.

### Qué no hacer

No extraigas un método solo para que la clase parezca más ocupada. `ApplyPercent` y `ApplyMemberDiscount` serían métodos de una línea usados una vez. Eso es indirección sin concepto; la lista pide nombres que aclaren, no más capas.

No mezcles un renombre con un cambio de comportamiento. Si el descuento de miembro debe aplicarse antes del descuento por porcentaje, escribe un test que falle para el nuevo orden, hazlo pasar y recién entonces refactoriza. Los tests te dicen qué cambio estás haciendo.

No refactorices sin una base verde. Si la suite está en rojo, no puedes saber si un fallo vino del refactor o de un bug que ya estaba.

## Repite con requisitos nuevos

Este capítulo no agrega comportamiento nuevo. Si un requisito posterior cambia el orden de los descuentos, primero escribe un test que falle e implementa ese comportamiento en su propio ciclo rojo-verde; después usa esta lista para limpiar el código.

## Para cerrar

Lo que cubrimos:

- Un refactor que cambia nombres, constantes y expresiones mientras el comportamiento sigue igual.
- La lista: base verde, nombres, valores mágicos, duplicación, guards, código muerto, volver a correr, commit.
- La diferencia entre un refactor y un cambio de comportamiento.
- Patrones relacionales y aritmética decimal como elecciones de legibilidad.
- Cuándo una extracción agrega un concepto y cuándo solo agrega ruido.

El capítulo sigue la [Refactoring checklist de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/refactoring-checklist): pasos pequeños, tests en verde y commit cuando el código se lee mejor. La lista es la misma; los ejemplos son C#.

Con esto terminan los fundamentos de testing. La siguiente parte construye una aplicación con estas herramientas.

