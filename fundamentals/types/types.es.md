# Tipos y variables

Por que importa para backend .NET: parsing, dinero, banderas y texto dependen del tipo correcto. Este capitulo fija int, double, bool y string con tests.

## Write the test first

Agrega TypeExamplesTests para Add, IsEven y Describe con salida etiquetada.

## Try to run the test

Ejecuta dotnet test en fundamentals slash types. Falla porque TypeExamples aun no existe.

## Write the minimal amount of code for the test to run and check the failing test output

Crea la libreria Types y referenciala desde Types.Tests. Confirma que el fallo es por el tipo faltante.

## Write enough code to make it pass

Agrega TypeExamples con Add, IsEven y Describe usando switch. Sin estado compartido.

## Refactor

Deja tres metodos estaticos pequenos y un test por comportamiento.

## Repeat for new requirements

Agrega un caso Describe para double y bool, mira el fallo y extiende el switch.

## Wrapping up

Los tipos ya son conocimiento ejecutable. El flujo de control sigue con estos predicados.
