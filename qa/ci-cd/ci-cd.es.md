# CI/CD más allá del job de tests

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/ci-cd)

Una corrida verde de CI demuestra que los tests pasan en ese commit. No demuestra que puedas desplegar lo que pasó. El workflow de este repositorio restauraba todas las soluciones desde cero en cada push, corría los tests y tiraba los resultados al terminar el job.

Este apéndice cambia ese workflow: cachea los paquetes, conserva los resultados y publica la API como artefacto. Se detiene en el artefacto, porque este repositorio no tiene una cuenta de nube donde desplegar.

## Lo que hacía el workflow

El job de tests se veía así:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 6.0.x
      - run: |
          set -euo pipefail
          for sln in $(find . -name "*.sln" -print); do
            echo "Testing $sln"
            dotnet test "$sln" --nologo
          done
```

`set -euo pipefail` es lo que hace honesto el loop: sin eso, el job reporta el código de salida del último comando y una solución que falla en medio de la lista desaparece. Un capítulo nuevo agrega una solución y `find` la recoge sin editar el workflow.

El costo se paga en cada corrida. Cada solución restaura sus paquetes otra vez, y el primer test que falla corta el loop sin nada que inspeccionar después.

## Cachea los paquetes

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```

La clave cambia cuando cambia cualquier archivo de proyecto. Un bump de versión o un paquete nuevo producen una caché nueva; un cambio en un `.cs` reutiliza la existente. `restore-keys` da un acierto parcial cuando falta la clave exacta, así un build después de agregar un proyecto arranca desde la caché anterior.

## Conserva los resultados

```yaml
- name: Restore and test
  run: |
    set -euo pipefail
    mkdir -p artifacts/test-results
    for sln in $(find . -name "*.sln" -print | sort); do
      echo "Testing $sln"
      name=$(echo "$sln" | tr '/' '_')
      dotnet test "$sln" --nologo \
        --logger "trx;LogFileName=$name.trx" \
        --results-directory artifacts/test-results
    done
- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: artifacts/test-results
```

Cada solución escribe su propio archivo TRX, nombrado a partir de su ruta. Reemplazar la barra importa: el logger trata el nombre como archivo, no como árbol de directorios. `if: always()` sube los resultados aunque el step de tests haya fallado, que es justo cuando valen la pena.

## Construye una vez, despliega ese build

```yaml
publish:
  needs: test
  if: github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 6.0.x
    - name: Publish the API
      run: |
        dotnet publish build-an-app/v14-cqrs/src/TaskTracker.Api/TaskTracker.Api.csproj \
          -c Release \
          -o artifacts/api \
          /p:UseAppHost=false
    - name: Upload the API
      uses: actions/upload-artifact@v4
      with:
        name: tasktracker-api
        path: artifacts/api
```

`needs: test` hace que el publish espere los tests verdes. La condición de rama evita que un pull request produzca un artefacto desplegable. Un job de deploy descargaría `tasktracker-api` y copiaría esos bytes al destino; no debería volver a clonar el repositorio y construir.

Los tests corrieron contra el mismo commit, no contra el mismo binario. Si necesitas confianza a nivel de binario, agrega un smoke test que arranque el artefacto publicado y llame a un endpoint antes del deploy.

## Corre el paso de publish aquí

El mismo comando, sin Actions:

```sh
dotnet publish build-an-app/v14-cqrs/src/TaskTracker.Api/TaskTracker.Api.csproj \
  -c Release \
  -o /tmp/tt-publish \
  /p:UseAppHost=false
ASPNETCORE_URLS=http://localhost:5098 \
  ConnectionStrings__TaskTracker="Data Source=/tmp/tt-publish/tasktracker.db" \
  dotnet /tmp/tt-publish/TaskTracker.Api.dll
```

En otra terminal:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5098/swagger/v1/swagger.json
```

La salida es `200` y la carpeta publicada pesa 38 MB. Esa carpeta contiene la app y sus dependencias, no el SDK; `dotnet TaskTracker.Api.dll` basta para ejecutarla.

## Entornos, secretos y rollback

Un job de deploy agrega piezas que este repositorio no tiene:

- `environment: production` en el job, con revisores requeridos en la configuración del repositorio. El job espera aprobación en vez de desplegar en cada push a `main`.
- Secretos guardados en ese entorno, no a nivel de repositorio. Un secreto de staging no lo puede leer un job de producción.
- `permissions: id-token: write` y un login en la nube que cambie el token de GitHub por credenciales de vida corta. Las claves de nube de larga duración en secretos del repositorio son lo que hay que evitar.
- Un tag inmutable: el SHA del commit, o el digest de la imagen. Desplegar `latest` convierte el rollback en una adivinanza.
- Un camino de rollback que vuelva a desplegar el artefacto o digest anterior. La versión anterior tiene que seguir existiendo.

## Lo que queda diferido

Este apéndice no tiene un destino en la nube, credenciales ni una URL viva. Un laboratorio práctico necesitaría:

- Un destino que acepte una imagen de contenedor o una carpeta publicada, como Container Apps, App Service o ECS.
- Una relación de confianza OIDC entre el repositorio y ese destino.
- Un entorno de staging, un smoke test contra él y un paso de promoción a producción.
- Un simulacro de rollback que despliegue el SHA anterior y revise el endpoint de salud.

Sin eso, un job de deploy solo haría echo de comandos y se declararía exitoso. El workflow termina en un artefacto que se puede descargar y ejecutar, que es la parte que este repositorio puede demostrar.

## Para cerrar

Lo que cubrimos:

- Un loop de tests que falla con la primera solución rota y cubre todas las soluciones del árbol.
- Una caché de NuGet con clave basada en los archivos de proyecto.
- Resultados TRX subidos incluso cuando el job falla.
- Un job de publish condicionado a los tests y a `main`, que produce un artefacto descargable.
- El publish y la ejecución en local, con un `200` del documento OpenAPI.
- Entornos, credenciales de vida corta, tags inmutables y rollback, con el destino faltante nombrado.

Sigue: despliegue en la nube.
