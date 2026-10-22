# Parte 4: Preguntas y apéndices

Esta parte cierra el roadmap. Los capítulos de preguntas responden dudas que no necesitan un proyecto nuevo. Los apéndices cubren los temas 🟠 que quedan fuera del camino principal de TDD; cada uno nombra lo que demuestra y lo que queda diferido.

## Preguntas

1. 🟠 [Preguntas de testing](testing-questions.es.md) ([EN](testing-questions.md)) - Tests unitarios, de integración y de aceptación, fakes frente a mocks, tests asíncronos, aislamiento, cobertura y caminos de error.
2. 🟠 [Preguntas de diseño](design-questions.es.md) ([EN](design-questions.md)) - Dónde va una regla, cuándo extraer una abstracción, CQRS, caché, logging y tiempos de vida de DI.
3. 🟠 [Preguntas de .NET](dotnet-questions.es.md) ([EN](dotnet-questions.md)) - Records frente a clases, IEnumerable frente a IQueryable, Task frente a ValueTask, excepciones frente a resultados, Span y ref.

## Apéndices

- 🟠 [Snapshot testing con Verify](snapshot-verify/snapshot-verify.es.md) ([EN](snapshot-verify/snapshot-verify.md)) - Aprueba una salida formateada con un archivo verificado y revisa sus diffs.
- 🟠 Tests end-to-end con Playwright. *Planeado.*
- 🟠 [Tests de rendimiento con NBomber](performance-nbomber/performance-nbomber.es.md) ([EN](performance-nbomber/performance-nbomber.md)) - Corre una carga fija contra una API y lee throughput y percentiles.
- 🟠 [Datos de test con Bogus](test-data-bogus/test-data-bogus.es.md) ([EN](test-data-bogus/test-data-bogus.md)) - Genera pedidos válidos con reglas y semilla, y deja valores literales para los bordes.
- 🟠 [Herramientas de integración: Respawn, Testcontainers y Docker](integration-testing-tools/integration-testing-tools.es.md) ([EN](integration-testing-tools/integration-testing-tools.md)) - Levanta PostgreSQL en Docker y resetea entre tests.
- 🟠 [Contenedores para la API TaskTracker](containers/containers.es.md) ([EN](containers/containers.md)) - Construye una imagen multi-stage, ejecútala con SQLite sobre un volumen y comprueba OpenAPI desde fuera.
- 🟠 CI/CD más allá del job único de tests. *Planeado.*
- 🟠 [Monitoreo y telemetría](monitoring-telemetry/monitoring-telemetry.es.md) ([EN](monitoring-telemetry/monitoring-telemetry.md)) - Registra un contador con tags y captúralo con un MeterListener.
- 🟠 Despliegue en la nube. *Planeado.*
- 🟠 Streaming. *Planeado.*
- 🟠 NoSQL. *Planeado.*
- 🟠 gRPC. *Planeado.*
- 🟠 GraphQL. *Planeado.*
- 🟠 SignalR. *Planeado.*
- 🟠 AsyncAPI. *Planeado.*
- 🟠 Bibliotecas SDK. *Planeado.*
- 🟠 DevOps e infraestructura como código. *Planeado.*

## Cobertura del roadmap

| Sección del roadmap | Destino | Estado |
|---|---|---|
| Lenguaje C#, CLI de .NET | [Parte 1](../fundamentals/install/install.es.md) | cubierto |
| SOLID, DRY, YAGNI, KISS | [Composición](../fundamentals/composition/composition.es.md) y preguntas de testing | cubierto |
| Inyección de dependencias | [Inyección de dependencias](../fundamentals/dependency-injection/dependency-injection.es.md), [v8](../build-an-app/v8-di/v8-di.es.md) | cubierto |
| Unit testing: MSTest, Moq | [Mocking](../fundamentals/mock/mock.es.md), [Parte 2](../testing/acceptance-tests/acceptance-tests.es.md) | cubierto |
| Unit testing: Bogus | [Datos de test con Bogus](test-data-bogus/test-data-bogus.es.md) | cubierto |
| Integration testing: WebApplicationFactory, TestServer | [Tests de aceptación](../testing/acceptance-tests/acceptance-tests.es.md), [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md) | cubierto |
| Integration testing: Respawn, Testcontainers, Docker | [Herramientas de integración](integration-testing-tools/integration-testing-tools.es.md) | cubierto |
| ASP.NET Core básico | [v1](../build-an-app/v1-intro/v1-intro.es.md) a [v14](../build-an-app/v14-cqrs/v14-cqrs.es.md) | cubierto |
| RDBMS, SQL, EF Core, Dapper | [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md), [v5](../build-an-app/v5-dapper/v5-dapper.es.md) | cubierto |
| APIs REST, OpenAPI/Swagger | [v2](../build-an-app/v2-json-routing/v2-json-routing.es.md), [v12](../build-an-app/v12-openapi/v12-openapi.es.md) | cubierto |
| AsyncAPI | Apéndice de AsyncAPI | planeado |
| Logging | [v9](../build-an-app/v9-logging/v9-logging.es.md) | cubierto |
| Caché | [v10](../build-an-app/v10-caching/v10-caching.es.md) | cubierto |
| Mensajería | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md) | cubierto |
| Scheduling de tareas | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md) | cubierto |
| Bibliotecas .NET | Apéndice de bibliotecas SDK | planeado |
| SignalR | Apéndice de SignalR | planeado |
| Snapshot, E2E, rendimiento | [Snapshot](snapshot-verify/snapshot-verify.es.md), [NBomber](performance-nbomber/performance-nbomber.es.md), apéndice de Playwright | snapshot y rendimiento cubiertos; E2E planeado |
| Contenedores, CI/CD, monitoreo, nube, DevOps | [Contenedores](containers/containers.es.md), [Monitoreo](monitoring-telemetry/monitoring-telemetry.es.md), apéndices de CI/CD, nube y DevOps | contenedores y monitoreo cubiertos; el resto planeado |
| Streaming, NoSQL, gRPC, GraphQL | Apéndices de protocolos y almacenamiento | planeado |
| Habilidades generales de desarrollo | Prefacio del README y preguntas de testing | cubierto |

