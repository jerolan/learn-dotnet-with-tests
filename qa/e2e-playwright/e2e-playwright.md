# End-to-end testing with Playwright

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/e2e-playwright)

Integration tests call the API and get the right JSON. The page can still be broken: a selector changed, the fetch URL is wrong, or the form submits and the list never refreshes. Only a browser runs the HTML, the CSS and the JavaScript together.

This appendix builds a small task board, drives it with Playwright, and checks that a task added through the form survives a reload. The test starts its own Kestrel process because a browser cannot connect to `WebApplicationFactory`'s in-memory server.

## How it works

The browser exercises the page, JavaScript and API as one journey. The test starts a real Kestrel process because Playwright cannot connect to `WebApplicationFactory`'s in-memory server.

### The page

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

### Start the app on a real port

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

## How to test

Build the solution, install Chromium with the generated Playwright script, then enable the gated browser suite:

```sh
dotnet build E2E.sln
pwsh tests/TaskBoard.Tests/bin/Debug/net6.0/playwright.ps1 install --with-deps chromium
RUN_E2E=1 dotnet test E2E.sln
```

Without `RUN_E2E=1`, the regular solution run skips the browser test. The dedicated workflow installs Chromium and runs the suite with the variable enabled.

## Write the test first

The page and API already exist; this test comes first for the browser journey, which verifies that the form submission reaches the API and the result survives a reload.

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

## Try to run the test

Run the normal suite without the browser gate. It reports the browser test as skipped:

```sh
dotnet test E2E.sln
```

```text
Skipped! - Failed:     0, Passed:     0, Skipped:     1, Total:     1, Duration: 44 ms
```

The skip only means the regular job did not run the browser. After installing Chromium, use the `RUN_E2E=1` command from How to test to execute the journey.

## Write the minimal amount of code for the test to run and check the failing test output

The test fixture starts Kestrel on a free port, waits for `/health`, then creates a browser context. Those pieces let the test reach the page; the UI assertion is what detects a missing or incorrect form submission.

## Write enough code to make it pass

The page handler posts the title to `/api/tasks`, clears the input and reloads the list. The test then reloads the browser and checks that the API still returns the new task. With Chromium installed, `RUN_E2E=1 dotnet test E2E.sln` passes the browser test.

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 2 s
```

## Refactor

### Keep the test stable

- Wait for a condition, not for time. `WaitForSelectorAsync`, `WaitForURLAsync` and the auto-waiting actions cover most cases.
- Prefer role and label locators for user-facing flows: `GetByRoleAsync("button", new() { Name = "Add" })`. They fail when the accessible name changes, which is usually the bug you want to catch.
- Start each test from a fresh browser context. A shared context carries cookies and local storage into the next test.
- Use a unique title per test when the app keeps state between tests. The in-memory store here survives a reload by design.
- Capture a trace or a screenshot on failure. A green local run does not help when CI fails on a slower machine.

### What belongs in E2E

Test the journeys that pay the bills: sign in, create the main entity, complete it, see it after a reload. Leave validation messages, edge cases and error branches to unit and integration tests, where they run in milliseconds and point at the failing class.

An E2E suite that tries to cover every branch becomes slow and flaky, and the team starts re-running it until it passes.

## Repeat for new requirements

Keep this sample focused on one high-value journey. Add another browser test only when it proves a separate user-visible workflow; use unit and integration tests for validation branches and error handling.

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
