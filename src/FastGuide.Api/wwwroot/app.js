const state = {
  channels: [],
  now: [],
  search: { channels: [], programs: [] }
};

const views = ["home", "directory", "search", "now"];

function setView(view) {
  views.forEach(v => document.getElementById(`${v}-view`).classList.toggle("active", v === view));
  document.querySelectorAll(".menu-item,.tab").forEach(el => {
    el.classList.toggle("active", el.dataset.view === view);
  });
}

async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Request failed: ${response.status}`);
  return response.json();
}

function formatTime(utc) {
  return new Date(utc).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
}

function renderDirectory() {
  const grid = document.getElementById("channel-grid");
  grid.innerHTML = state.channels.map(ch => `
    <article class="card">
      <div class="tag">${(ch.category ?? "General").toUpperCase()}</div>
      <h4>${ch.name}</h4>
      <p>${ch.description ?? "No channel description yet."}</p>
    </article>
  `).join("");
  document.getElementById("channel-count").textContent = `${state.channels.length} channels`;
}

function renderNow() {
  const cards = document.getElementById("now-grid");
  const list = document.getElementById("now-list");
  const rows = state.now.map(slot => `
    <article class="card">
      <div class="tag">LIVE • ${slot.channelName}</div>
      <h4>${slot.title}</h4>
      <p>${slot.description ?? "No description provided."}</p>
      <p class="small">${formatTime(slot.startTimeUtc)} - ${formatTime(slot.endTimeUtc)}</p>
    </article>
  `).join("");

  cards.innerHTML = rows || `<article class="card"><h4>No live slots</h4><p>Ingestion has not populated current programs yet.</p></article>`;
  list.innerHTML = state.now.map(slot => `
    <div class="row">
      <div class="small">${formatTime(slot.startTimeUtc)} - ${formatTime(slot.endTimeUtc)}</div>
      <div><strong>${slot.title}</strong><div class="small">${slot.channelName}</div></div>
      <div class="small">Now</div>
    </div>
  `).join("");
}

function renderSearch() {
  document.getElementById("search-channel-grid").innerHTML = state.search.channels.map(ch => `
    <article class="card"><h4>${ch.name}</h4><p>${ch.category ?? "General"}</p></article>
  `).join("");

  document.getElementById("search-program-grid").innerHTML = state.search.programs.map(p => `
    <article class="card"><h4>${p.title}</h4><p>${p.channelName} • ${formatTime(p.startTimeUtc)}</p></article>
  `).join("");

  document.getElementById("search-count").textContent = `${state.search.channels.length + state.search.programs.length} results`;
}

async function initialize() {
  state.channels = await fetchJson("/channels");
  state.now = await fetchJson("/now");
  renderDirectory();
  renderNow();
  renderSearch();

  document.querySelectorAll(".menu-item,.tab").forEach(btn => btn.addEventListener("click", () => setView(btn.dataset.view)));

  document.getElementById("search-form").addEventListener("submit", async event => {
    event.preventDefault();
    const query = document.getElementById("search-input").value.trim();
    if (!query) {
      state.search = { channels: [], programs: [] };
      renderSearch();
      return;
    }

    state.search = await fetchJson(`/search?query=${encodeURIComponent(query)}`);
    renderSearch();
    setView("search");
  });
}

initialize().catch(err => {
  console.error(err);
  document.body.innerHTML = `<main style="padding:32px;font-family:Arial">Unable to load FastGuide UI. ${err.message}</main>`;
});
