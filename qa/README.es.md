# Parte 4: Preguntas y apéndices

Esta parte cierra el roadmap. Los capítulos de preguntas responden dudas que no necesitan un proyecto nuevo. Los apéndices cubren los temas 🟠 que quedan fuera del camino principal de TDD; cada uno nombra lo que demuestra y lo que queda diferido.

## Preguntas

1. 🟠 [Preguntas de testing](testing-questions.es.md) ([EN](testing-questions.md)) - Tests unitarios, de integración y de aceptación, fakes frente a mocks, tests asíncronos, aislamiento, cobertura y caminos de error.
2. 🟠 [Preguntas de diseño](design-questions.es.md) ([EN](design-questions.md)) - Dónde va una regla, cuándo extraer una abstracción, CQRS, caché, logging y tiempos de vida de DI.
3. 🟠 [Preguntas de .NET](dotnet-questions.es.md) ([EN](dotnet-questions.md)) - Records frente a clases, IEnumerable frente a IQueryable, Task frente a ValueTask, excepciones frente a resultados, Span y ref.

## Apéndices

- 🟠 [Snapshot testing con Verify](snapshot-verify/snapshot-verify.es.md) ([EN](snapshot-verify/snapshot-verify.md)) - Aprueba una salida formateada con un archivo verificado y revisa sus diffs.
- 🟠 [Tests end-to-end con Playwright](e2e-playwright/e2e-playwright.es.md) ([EN](e2e-playwright/e2e-playwright.md)) - Arranca Kestrel en un puerto libre y maneja el tablero en Chromium.
- 🟠 [Tests de rendimiento con NBomber](performance-nbomber/performance-nbomber.es.md) ([EN](performance-nbomber/performance-nbomber.md)) - Corre una carga fija contra una API y lee throughput y percentiles.
- 🟠 [Datos de test con Bogus](test-data-bogus/test-data-bogus.es.md) ([EN](test-data-bogus/test-data-bogus.md)) - Genera pedidos válidos con reglas y semilla, y deja valores literales para los bordes.
- 🟠 [Herramientas de integración: Respawn, Testcontainers y Docker](integration-testing-tools/integration-testing-tools.es.md) ([EN](integration-testing-tools/integration-testing-tools.md)) - Levanta PostgreSQL en Docker y resetea entre tests.
- 🟠 [Contenedores para la API TaskTracker](containers/containers.es.md) ([EN](containers/containers.md)) - Construye una imagen multi-stage, ejecútala con SQLite sobre un volumen y comprueba OpenAPI desde fuera.
- 🟠 [CI/CD más allá del job de tests](ci-cd/ci-cd.es.md) ([EN](ci-cd/ci-cd.md)) - Cachea paquetes, conserva resultados TRX y publica la API como artefacto condicionado a los tests.
- 🟠 [Monitoreo y telemetría](monitoring-telemetry/monitoring-telemetry.es.md) ([EN](monitoring-telemetry/monitoring-telemetry.md)) - Registra un contador con tags y captúralo con un MeterListener.
- 🟠 [Despliegue en la nube](cloud/cloud.es.md) ([EN](cloud/cloud.md)) - Mapea el artefacto publicado a un host: configuración, secretos, migraciones, probes, rollback y un job de deploy.
- 🟠 [Streaming con IAsyncEnumerable](streaming/streaming.es.md) ([EN](streaming/streaming.md)) - Produce tareas a medida que llegan, cancela a mitad y conserva lo entregado antes de un fallo.
- 🟠 [NoSQL con MongoDB](nosql/nosql.es.md) ([EN](nosql/nosql.md)) - Guarda documentos en MongoDB, levántalo con Testcontainers y corrige un orden sensible a mayúsculas con una collation.
- 🟠 [gRPC](grpc/grpc.es.md) ([EN](grpc/grpc.md)) - Define un contrato proto, genera el servicio y el cliente, y llámalo a través de TestServer.
- 🟠 [GraphQL](graphql/graphql.es.md) ([EN](graphql/graphql.md)) - Publica una query y una mutation sobre un endpoint y prueba los documentos por HTTP.
- 🟠 [Ciclo de vida y límites de entrega de SignalR](signalr/signalr.es.md) ([EN](signalr/signalr.md)) - Complementa la v15 de la Parte 3 y se enfoca en el ciclo de vida de la conexión y los límites de entrega.
- 🟠 [AsyncAPI](asyncapi/asyncapi.es.md) ([EN](asyncapi/asyncapi.md)) - Describe el evento TaskCreated como contrato y valídalo con la CLI oficial.
- 🟠 [Bibliotecas SDK](sdk-libraries/sdk-libraries.es.md) ([EN](sdk-libraries/sdk-libraries.md)) - Construye un cliente HTTP tipado, pruébalo con un stub handler y empaquétalo como paquete NuGet.
- 🟠 [DevOps e infraestructura como código](devops-iac/devops-iac.es.md) ([EN](devops-iac/devops-iac.md)) - Compila una plantilla Bicep, dale una identidad a la app y lee secretos desde un vault.

## Cobertura del roadmap

| Sección del roadmap | Destino | Estado |
|---|---|---|
| Lenguaje C#, CLI de .NET | [Parte 1](../fundamentals/install/install.es.md) | cubierto |
| SOLID, DRY, YAGNI, KISS | [Composición](../fundamentals/composition/composition.es.md) y preguntas de testing | cubierto |
| Inyección de dependencias | [Inyección de dependencias](../fundamentals/dependency-injection/dependency-injection.es.md), [v8](../build-an-app/v8-di/v8-di.es.md) | cubierto |
| Unit testing: MSTest, Moq | [Mocking](../fundamentals/mock/mock.es.md), [Parte 2](../testing/acceptance-tests/acceptance-tests.es.md) | cubierto |
| Unit testing: Bogus | [Datos de test con Bogus](test-data-bogus/test-data-bogus.es.md) | cubierto |
| Integration testing: WebApplicationFactory, TestServer | [Tests de aceptación](../testing/acceptance-tests/acceptance-tests.es.md), [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md) | cubierto |
| Integration testing: Respawn | [Herramientas de integración](integration-testing-tools/integration-testing-tools.es.md) | cubierto |
| Integration testing: Testcontainers, Docker (imagen del roadmap) | [Herramientas de integración](integration-testing-tools/integration-testing-tools.es.md) | cubierto |
| ASP.NET Core básico | [v1](../build-an-app/v1-intro/v1-intro.es.md) a [v15](../build-an-app/v15-signalr/v15-signalr.es.md) | cubierto |
| RDBMS, SQL, EF Core, Dapper | [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md), [v5](../build-an-app/v5-dapper/v5-dapper.es.md) | cubierto |
| APIs REST, OpenAPI/Swagger | [v2](../build-an-app/v2-json-routing/v2-json-routing.es.md), [v12](../build-an-app/v12-openapi/v12-openapi.es.md) | cubierto |
| AsyncAPI | [AsyncAPI](asyncapi/asyncapi.es.md) | cubierto |
| Logging | [v9](../build-an-app/v9-logging/v9-logging.es.md) | cubierto |
| Caché | [v10](../build-an-app/v10-caching/v10-caching.es.md) | cubierto |
| Mensajería | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md) | cubierto |
| Scheduling de tareas | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md) | cubierto |
| Bibliotecas .NET | [Bibliotecas SDK](sdk-libraries/sdk-libraries.es.md) | cubierto |
| SignalR | [Integración principal: v15](../build-an-app/v15-signalr/v15-signalr.es.md); el [apéndice complementario](signalr/signalr.es.md) cubre el ciclo de vida del hub y los límites de entrega | cubierto |
| Snapshot, E2E, rendimiento | [Snapshot](snapshot-verify/snapshot-verify.es.md), [Playwright](e2e-playwright/e2e-playwright.es.md), [NBomber](performance-nbomber/performance-nbomber.es.md) | cubierto |
| Contenedores, CI/CD, monitoreo, nube, DevOps | [Contenedores](containers/containers.es.md), [CI/CD](ci-cd/ci-cd.es.md), [Monitoreo](monitoring-telemetry/monitoring-telemetry.es.md), [Nube](cloud/cloud.es.md), [DevOps](devops-iac/devops-iac.es.md) | cubierto |
| Streaming, NoSQL, gRPC, GraphQL | [gRPC](grpc/grpc.es.md), [GraphQL](graphql/graphql.es.md), [NoSQL](nosql/nosql.es.md), [Streaming](streaming/streaming.es.md) | cubierto |
| Habilidades generales de desarrollo | Prefacio del README y preguntas de testing | cubierto |

**Fuente del roadmap:** El mapa de texto `map.json` del [proyecto del roadmap](https://github.com/Elfocrash/.NET-Backend-Developer-Roadmap) enumera WebApplicationFactory, TestServer y Respawn; la imagen del roadmap agrega Testcontainers y Docker. Parte 2 y Parte 3 cubren WebApplicationFactory/TestServer, y el [apéndice de herramientas de integración](integration-testing-tools/integration-testing-tools.es.md) cubre Respawn, Testcontainers y Docker.

