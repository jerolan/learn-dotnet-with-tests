# Instalar

Por que importa para backend .NET: sin SDK, IDE y proyecto de tests no se compila ni se prueba nada. Este capitulo deja esa base en verde con TDD.

## Write the test first

Queremos prueba de que la cadena funciona desde un test. Agrega SdkInfoTests que verifica que SdkInfo.GetDotnetVersion devuelve un texto no vacio.

## Try to run the test

Ejecuta dotnet test en fundamentals slash install. Falla porque SdkInfo aun no existe. Ese fallo es lo esperado.

## Write the minimal amount of code for the test to run and check the failing test output

Crea la libreria InstallCheck y referenciala desde InstallCheck.Tests. Lee el fallo para confirmar que es por el tipo faltante, no por el entorno.

## Write enough code to make it pass

Agrega SdkInfo con GetDotnetVersion que devuelve Environment.Version. Crea la solucion con dotnet new sln, classlib y mstest, agrega los proyectos y la referencia.

## Refactor

Deja el codigo en un solo metodo estatico y el test en una sola afirmacion. Sin ayudantes extra.

## Repeat for new requirements

Verifica el IDE: abre la carpeta en VS Code o Visual Studio, restaura y repite dotnet test. Mismo verde desde ambas entradas.

## Wrapping up

SDK, IDE, solucion y proyecto MSTest en verde. Cada capitulo siguiente repite el ciclo: test primero, codigo minimo, refactor.
