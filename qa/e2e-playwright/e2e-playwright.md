# End-to-end testing with Playwright

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/e2e-playwright)

Integration tests call the API and get the right JSON. The page can still be broken: a selector changed, the fetch URL is wrong, or the form submits and the list never refreshes. Only a browser runs the HTML, the CSS and the JavaScript together.

This appendix builds a small task board, drives it with Playwright, and checks that a task added through the form survives a reload. The test starts its own Kestrel process because a browser cannot connect to `WebApplicationFactory`'s in-memory server.

## The page

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

`src/TaskBoard/wwwroot/app.js` posts the title and reloads the list:

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

The API keeps tasks in a `ConcurrentDictionary`, exposes `GET /api/tasks`, `POST /api/tasks` and `GET /health`, and serves the static files with `UseDefaultFiles` and `UseStaticFiles`.

## Start the app on a real port

Playwright talks HTTP, so the app needs a socket. The test finds a free port, starts the built DLL, and waits for the health endpoint:

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

`GetFreePort` binds a `TcpListener` to port 0, reads the assigned port and releases it. The gap between release and Kestrel binding is small; a collision fails the test with a clear startup error rather than a random port conflict.

`WaitForHealthAsync` retries for ten seconds. A fixed `Thread.Sleep` either wastes time or fails on a slow machine.

The root is found by walking up from `AppContext.BaseDirectory` until `E2E.sln` appears. That avoids hard-coding an absolute path that only works on one machine.

## Write the test first

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

`WaitForSelectorAsync` waits for the browser to paint the new item. The reload proves the item came from the API, not from a DOM node that the script left behind.

Run it with the environment variable that enables the suite:

```sh
RUN_E2E=1 dotnet test E2E.sln
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 2 s
```

## Why the suite is gated

The main CI job runs every solution with `dotnet test`. A browser test needs Chromium and the system libraries it links against, so the regular job would fail on a runner that has neither.

Without the variable, the test reports itself as skipped:

```text
Skipped! - Failed:     0, Passed:     0, Skipped:     1, Total:     1, Duration: 44 ms
```

`.github/workflows/e2e.yml` runs only when `qa/e2e-playwright` or the workflow changes. It builds the solution, installs Chromium with the Playwright script, and runs the test with `RUN_E2E=1`:

```yaml
- name: Install Chromium
  run: pwsh tests/TaskBoard.Tests/bin/Debug/net6.0/playwright.ps1 install --with-deps chromium
- name: Run the browser test
  run: dotnet test E2E.sln --no-build --nologo
```

The browser download is cached under `~/.cache/ms-playwright`. `--with-deps` still installs the apt packages the first time.

## Keep the test stable

- Wait for a condition, not for time. `WaitForSelectorAsync`, `WaitForURLAsync` and the auto-waiting actions cover most cases.
- Prefer role and label locators for user-facing flows: `GetByRoleAsync("button", new() { Name = "Add" })`. They fail when the accessible name changes, which is usually the bug you want to catch.
- Start each test from a fresh browser context. A shared context carries cookies and local storage into the next test.
- Use a unique title per test when the app keeps state between tests. The in-memory store here survives a reload by design.
- Capture a trace or a screenshot on failure. A green local run does not help when CI fails on a slower machine.

## What belongs in E2E

Test the journeys that pay the bills: sign in, create the main entity, complete it, see it after a reload. Leave validation messages, edge cases and error branches to unit and integration tests, where they run in milliseconds and point at the failing class.

An E2E suite that tries to cover every branch becomes slow and flaky, and the team starts re-running it until it passes.

## What is deferred

There is no Firefox or WebKit run, no mobile viewport, no visual snapshot and no trace artifact in CI. A hands-on lab would add a Playwright project per browser, a `storageState` for an authenticated flow, and `--trace on` with the trace uploaded when the job fails.

## Wrapping up

What we have covered:

- A page and an API served by one minimal app.
- A test that starts Kestrel on a free port and waits for health.
- A Playwright flow that fills the form, waits for the item and reloads.
- The `RUN_E2E` gate and a separate workflow that installs Chromium.
- Stable locators, fresh contexts, failure artifacts and the scope an E2E suite should keep.

Next: cloud deployment.
