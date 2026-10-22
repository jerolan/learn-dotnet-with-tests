# Hola, mundo

Por que importa para backend .NET: todo servicio empieza con un comportamiento minimo verificado. Este capitulo ensena el ciclo TDD con el caso mas pequeno.

## Write the test first

Agrega GreeterTests con SayHello_ReturnsHelloWorld que verifica que Greeter.SayHello devuelve Hello, World.

## Try to run the test

Ejecuta dotnet test en fundamentals slash hello-world. Falla porque Greeter aun no existe.

## Write the minimal amount of code for the test to run and check the failing test output

Crea la libreria HelloWorld y referenciala desde HelloWorld.Tests. Confirma que el fallo nombra el tipo faltante.

## Write enough code to make it pass

Agrega Greeter con SayHello que devuelve Hello, World. Crea la solucion y la referencia.

## Refactor

Deja una clase y un metodo, un test con Arrange, Act y Assert. Sin ramas extra.

## Repeat for new requirements

Cambia el test para pedir un nombre, mira el fallo y extiende SayHello. Mismo ciclo, comportamiento mayor.

## Wrapping up

Red, Green, Refactor ya es memoria muscular. Los siguientes capitulos crecen tipos, flujo y errores con este ritmo.
