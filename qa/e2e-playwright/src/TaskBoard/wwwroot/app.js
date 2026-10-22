const form = document.querySelector("#task-form");
const input = document.querySelector("#title");
const list = document.querySelector("#tasks");

async function loadTasks() {
  const response = await fetch("/api/tasks");
  const tasks = await response.json();

  list.replaceChildren(
    ...tasks.map((task) => {
      const item = document.createElement("li");
      item.textContent = task.title;
      return item;
    }),
  );
}

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

loadTasks();
