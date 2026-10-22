# Tests end-to-end con Playwright

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/e2e-playwright)

Los tests de integración llaman a la API y reciben el JSON correcto. La página igual puede estar rota: cambió un selector, la URL del fetch está mal, o el formulario envía y la lista nunca se refresca. Solo un navegador ejecuta el HTML, el CSS y el JavaScript juntos.

Este apéndice construye un tablero de tareas pequeño, lo maneja con Playwright y comprueba que una tarea agregada por el formulario sobrevive un reload. El test arranca su propio proceso Kestrel porque un navegador no puede conectarse al servidor en memoria de `WebApplicationFactory`.

## Cómo funciona

El navegador prueba la página, el JavaScript y la API como un solo recorrido. El test arranca un proceso Kestrel real porque Playwright no puede conectarse al servidor en memoria de `WebApplicationFactory`.

### La página

`src/TaskBoard/wwwroot/index.html`:

```html
<form id="task-form">
  <label for="title">Task</label>
  <input id="title" name="title" autocomplete="off">
  <button type="submit">Add</button>
</form>

<ul id="tasks"></ul>

<script src="/app.js"></script>
```

`src/TaskBoard/wwwroot/app.js` envía el título y recarga la lista:

```javascript
form.addEventListener("submit", async (event) => {
  event.preventDefault();

  const title = input.value.trim();
  if (!title) {
    return;
  }

  await fetch("/api/tasks", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ title }),
  });

  input.value = "";
  await loadTasks();
});
```

La API guarda tareas en un `ConcurrentDictionary`, expone `GET /api/tasks`, `POST /api/tasks` y `GET /health`, y sirve los archivos estáticos con `UseDefaultFiles` y `UseStaticFiles`.

### Arranca la app en un puerto real

Playwright habla HTTP, así que la app necesita un socket. El test busca un puerto libre, arranca el DLL compilado y espera al endpoint de salud:

```csharp
var port = GetFreePort();
_baseUrl = $"http://127.0.0.1:{port}";

var dll = Path.Combine(root, "src", "TaskBoard", "bin", "Debug", "net6.0", "TaskBoard.dll");

_app = Process.Start(new ProcessStartInfo("dotnet", $"\"{dll}\" --urls {_baseUrl}")
{
    WorkingDirectory = Path.Combine(root, "src", "TaskBoard"),
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
});

await WaitForHealthAsync();
```

`GetFreePort` enlaza un `TcpListener` al puerto 0, lee el puerto asignado y lo libera. La brecha entre liberar y enlazar Kestrel es pequeña; una colisión falla el test con un error de arranque claro en vez de un conflicto de puerto aleatorio.

`WaitForHealthAsync` reintenta durante diez segundos. Un `Thread.Sleep` fijo o desperdicia tiempo o falla en una máquina lenta.

La raíz se encuentra subiendo desde `AppContext.BaseDirectory` hasta que aparece `E2E.sln`. Eso evita una ruta absoluta que solo funciona en una máquina.

## Cómo probarlo

Compila la solución, instala Chromium con el script generado por Playwright y habilita la suite de navegador:

```sh
dotnet build E2E.sln
pwsh tests/TaskBoard.Tests/bin/Debug/net6.0/playwright.ps1 install --with-deps chromium
RUN_E2E=1 dotnet test E2E.sln
```

Sin `RUN_E2E=1`, la ejecución normal de la solución salta el test de navegador. El workflow dedicado instala Chromium y ejecuta la suite con la variable habilitada.

## Escribe primero el test

La página y la API ya existen; este test va primero para el recorrido de navegador, que verifica que el formulario llegue a la API y que el resultado sobreviva un reload.

```csharp
[TestMethod]
public async Task AddingATaskShowsItInTheListAndAfterAReload()
{
    await _page.FillAsync("#title", "Write the chapter");
    await _page.ClickAsync("button[type=submit]");

    await _page.WaitForSelectorAsync("li:has-text('Write the chapter')");

    await _page.ReloadAsync();

    Assert.AreEqual(
        "Write the chapter",
        await _page.Locator("#tasks li").First.TextContentAsync());
}
```

`WaitForSelectorAsync` espera a que el navegador pinte el elemento nuevo. El reload demuestra que el elemento vino de la API, no de un nodo del DOM que el script dejó atrás.

## Intenta ejecutar el test

Ejecuta la suite normal sin habilitar el navegador. El test de navegador aparece como saltado:

```sh
dotnet test E2E.sln
```

```text
Skipped! - Failed:     0, Passed:     0, Skipped:     1, Total:     1, Duration: 44 ms
```

El salto solo significa que el job normal no corrió el navegador. Después de instalar Chromium, usa el comando `RUN_E2E=1` de Cómo probarlo para ejecutar el recorrido.

## Escribe el código mínimo para que el test compile y mira la salida

El fixture arranca Kestrel en un puerto libre, espera `/health` y después crea un contexto de navegador. Esas piezas permiten que el test llegue a la página; la aserción de UI detecta un envío ausente o incorrecto.

## Escribe el código suficiente para que pase

El handler de la página envía el título a `/api/tasks`, limpia el input y recarga la lista. El test vuelve a cargar el navegador y comprueba que la API aún devuelve la tarea nueva. Con Chromium instalado, `RUN_E2E=1 dotnet test E2E.sln` pasa el test de navegador.

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 2 s
```

## Refactor

### Mantén estable el test

- Espera una condición, no un tiempo. `WaitForSelectorAsync`, `WaitForURLAsync` y las acciones con auto-wait cubren la mayoría de los casos.
- Prefiere locators por rol y etiqueta para flujos de usuario: `GetByRoleAsync("button", new() { Name = "Add" })`. Fallan cuando cambia el nombre accesible, que suele ser el bug que quieres atrapar.
- Empieza cada test con un contexto de navegador nuevo. Un contexto compartido arrastra cookies y local storage al siguiente test.
- Usa un título único por test cuando la app guarda estado entre tests. El store en memoria de aquí sobrevive un reload a propósito.
- Captura un trace o una screenshot cuando falla. Una corrida verde local no ayuda cuando CI falla en una máquina más lenta.

### Qué pertenece a E2E

Prueba los recorridos que importan: iniciar sesión, crear la entidad principal, completarla, verla después de un reload. Deja los mensajes de validación, los bordes y las ramas de error a los tests unitarios y de integración, donde corren en milisegundos y señalan la clase que falló.

Una suite E2E que intenta cubrir cada rama se vuelve lenta e inestable, y el equipo empieza a repetirla hasta que pasa.

## Repite con requisitos nuevos

Mantén este ejemplo enfocado en un recorrido de alto valor. Agrega otro test de navegador solo cuando pruebe un flujo visible distinto; usa tests unitarios y de integración para ramas de validación y error.

## Lo que queda diferido

No hay corrida en Firefox ni WebKit, ni viewport móvil, ni snapshot visual, ni trace como artefacto en CI. Un laboratorio práctico agregaría un proyecto de Playwright por navegador, un `storageState` para un flujo autenticado y `--trace on` con el trace subido cuando el job falla.

## Para cerrar

Lo que cubrimos:

- Una página y una API servidas por una app mínima.
- Un test que arranca Kestrel en un puerto libre y espera salud.
- Un flujo de Playwright que llena el formulario, espera el elemento y recarga.
- El gate `RUN_E2E` y un workflow aparte que instala Chromium.
- Locators estables, contextos nuevos, artefactos de fallo y el alcance que debe mantener una suite E2E.

Sigue: despliegue en la nube.
