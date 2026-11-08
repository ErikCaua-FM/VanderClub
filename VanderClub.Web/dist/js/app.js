/* Interface nativa do navegador: HTML + CSS + JavaScript, sem framework. */
(async function () {
  "use strict";
  const localPackage = ["localhost", "127.0.0.1", "[::1]"].includes(location.hostname);
  const V = window.VC,
    $ = (s) => document.querySelector(s),
    app = $("#app"),
    dialog = $("#dialog");
  const h = (s) =>
    String(s ?? "").replace(
      /[&<>"']/g,
      (c) =>
        ({
          "&": "&amp;",
          "<": "&lt;",
          ">": "&gt;",
          '"': "&quot;",
          "'": "&#39;",
        })[c],
    );
  const paths = {
    home: '<path d="m3 10 9-7 9 7v10H3Z"/><path d="M9 20v-7h6v7"/>',
    building:
      '<rect x="4" y="3" width="16" height="18" rx="2"/><path d="M9 21v-5h6v5M8 7h2m4 0h2M8 11h2m4 0h2"/>',
    shield:
      '<path d="m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6Z"/><path d="m8 12 3 3 5-6"/>',
    calendar:
      '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 3v4m10-4v4M3 11h18m-13 4h2m4 0h2m-8 3h2"/>',
    vote: '<path d="m9 10 2 2 4-5M6 3h12v12H6ZM6 12l-3 4v5h18v-5l-3-4M3 16h18"/>',
    users:
      '<circle cx="9" cy="8" r="3"/><path d="M3 21v-3a6 6 0 0 1 12 0v3m1-16a3 3 0 0 1 0 6m2 4a5 5 0 0 1 3 4v2"/>',
    file: '<path d="M14 3H5v18h14V8Zm0 0v5h5M8 12h8m-8 4h6"/>',
    alert:
      '<path d="M10 4 2 19a1 1 0 0 0 1 2h18a1 1 0 0 0 1-2L14 4a2 2 0 0 0-4 0Z"/><path d="M12 9v5m0 3v.1"/>',
    chat: '<path d="M21 11a8 8 0 0 1-8 8H8l-5 3v-7a8 8 0 1 1 18-4Z"/><path d="M7 10h10m-10 4h6"/>',
    layers: '<path d="m12 3 10 5-10 5L2 8Zm-10 9 10 5 10-5M2 16l10 5 10-5"/>',
    search: '<circle cx="10.5" cy="10.5" r="7"/><path d="m16 16 5 5"/>',
    plus: '<path d="M12 5v14M5 12h14"/>',
    arrow: '<path d="M4 12h16m-6-6 6 6-6 6"/>',
    chevron: '<path d="m9 5 7 7-7 7"/>',
    left: '<path d="m14 5-7 7 7 7"/>',
    down: '<path d="m6 9 6 6 6-6"/>',
    close: '<path d="m6 6 12 12M6 18 18 6"/>',
    check: '<path d="m5 12 4 4 10-11"/>',
    pin: '<path d="m15 3 6 6-4 1-4 4v4l-2 2-7-7 2-2h4l4-4Z M9 15l-6 6"/>',
    mail: '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 6 9 7 9-7"/>',
    phone: '<path d="m7 3 3 5-2 3c1 3 3 5 6 6l3-2 4 3-1 3C9 23 1 15 3 4Z"/>',
    map: '<path d="M19 10c0 5-7 11-7 11S5 15 5 10a7 7 0 0 1 14 0Z"/><circle cx="12" cy="10" r="2"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v6h4"/>',
    edit: '<path d="m16 3 5 5-12 12-6 1 1-6Zm-2 2 5 5"/>',
    trash: '<path d="M3 6h18M8 6V3h8v3M5 6l1 15h12l1-15M10 10v7m4-7v7"/>',
    logout: '<path d="M10 4H4v16h6m4-12 4 4-4 4m-6-4h13"/>',
    bell: '<path d="M18 8a6 6 0 0 0-12 0c0 7-3 8-3 8h18s-3-1-3-8M9 20h6"/>',
    flask: '<path d="M8 3h8M10 3v7L4 20h16l-6-10V3M7 15h10"/>',
    wallet:
      '<rect x="3" y="5" width="18" height="15" rx="2"/><path d="M3 8h18m0 4h-6v5h6m-3-3v.1"/>',
    sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l2 2m10 10 2 2M5 19l2-2M17 7l2-2"/>',
    eye: '<path d="M2 12S6 5 12 5s10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>',
    lock: '<rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 4v3"/>',
    send: '<path d="m3 3 18 9-18 9 4-9Zm4 9h14"/>',
    info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10v.1"/>',
    download: '<path d="M12 3v12m-4-4 4 4 4-4M4 16v5h16v-5"/>',
    heart:
      '<path d="M12 21S2 14 2 8c0-6 8-7 10-1 2-6 10-5 10 1 0 6-10 13-10 13Z"/>',
    refresh:
      '<path d="M20 7v5h-5M4 17v-5h5M5 7a8 8 0 0 1 14 0M5 17a8 8 0 0 0 14 0"/>',
  };
  const icon = (n, c = "") =>
    `<svg class="icon ${c}" viewBox="0 0 24 24" aria-hidden="true">${paths[n] || paths.file}</svg>`;
  const role = (r) =>
    r === "ADM_GERAL"
      ? "ADM Geral"
      : r === "ADM_LOCAL"
        ? "ADM Local"
        : r === "Titular"
          ? "Sócio titular"
          : r;
  const initials = (n) =>
    n
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((x) => x[0])
      .join("");
  const avatar = (n, c = "") =>
    `<span class="avatar ${c}">${h(initials(n))}</span>`;
  const tag = (text, color = "") =>
    `<span class="tag ${color}">${h(text)}</span>`;
  const action = (label, name, id = "", style = "secondary sm", ic = "") =>
    `<button type="button" class="btn ${style}" data-action="${name}" data-id="${h(id)}">${ic ? icon(ic) : ""}${h(label)}</button>`;
  const link = (label, route, style = "link", ic = "arrow") =>
    `<a class="${style}" href="#${route}">${h(label)}${ic ? icon(ic) : ""}</a>`;
  let svc,
    D,
    session,
    route,
    section = "inicio",
    modalAction = null,
    chatDraft = "",
    chatUnit = "",
    flashAfterNav = "";
  const filters = {};
  const getFilters = () =>
    filters[section] ||
    (filters[section] = {
      q: "",
      units: [],
      flags: [],
      type: "",
      tab: "Todas",
      page: 1,
    });
  const unit = (id) => D.units.find((u) => u.id === id);
  const unitName = (id) => unit(id)?.name || "Toda a rede";
  const timezone = (id) => unit(id)?.timezone || "America/Sao_Paulo";
  const now = () => new Date().toISOString();
  const formatDate = (s, id) =>
    !s
      ? "—"
      : s.length === 10
        ? s.split("-").reverse().join("/")
        : new Intl.DateTimeFormat("pt-BR", {
            timeZone: timezone(id),
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
          }).format(new Date(s));
  const formatTime = (s, id) =>
    new Intl.DateTimeFormat("pt-BR", {
      timeZone: timezone(id),
      hour: "2-digit",
      minute: "2-digit",
    }).format(new Date(s));
  const when = (s, id) =>
    s ? formatDate(s, id) + " às " + formatTime(s, id) : "—";
  const dateTile = (iso, id) => {
    const p = V.localInput(iso, timezone(id));
    const month = new Intl.DateTimeFormat("pt-BR", {
      month: "short",
      timeZone: timezone(id),
    })
      .format(new Date(iso))
      .replace(".", "");
    return `<div class="date-tile"><b>${p.slice(8, 10)}</b><span>${h(month)}</span></div>`;
  };
  const isGeneral = () => D.actor.role === "ADM_GERAL";
  const operatingUnits = () =>
    D.units.filter((u) => isGeneral() || u.id === D.actor.unitId);
  const currentVersion = (p) =>
    p.versions.find((v) => v.id === p.currentVersionId);
  const contract = (p) => D.contracts.find((c) => c.id === p.contractId);
  const pending = (c) =>
    D.people.filter(
      (p) =>
        p.contractId === c.id &&
        p.type !== "Titular" &&
        p.approval === "Pendente",
    );
  const overdue = (c) =>
    D.invoices.filter(
      (i) =>
        i.contractId === c.id &&
        V.invoiceStatus(i, V.today(now(), timezone(c.unitId))) === "Atrasada",
    );
  const contractStatus = (c) =>
    c.end <= V.today(now(), timezone(c.unitId)) ? "Encerrado" : c.status;
  const field = (
    label,
    name,
    value = "",
    type = "text",
    attrs = "",
    hint = "",
  ) =>
    `<label class="field ${attrs.includes("data-full") ? "full" : ""}" for="f-${name}">${label}<input id="f-${name}" name="${name}" type="${type}" value="${h(value)}" ${attrs}>${hint ? `<small>${hint}</small>` : ""}</label>`;
  const area = (label, name, value = "", attrs = "", hint = "") =>
    `<label class="field full" for="f-${name}">${label}<textarea id="f-${name}" name="${name}" ${attrs}>${h(value)}</textarea>${hint ? `<small>${hint}</small>` : ""}</label>`;
  const select = (label, name, options, value = "", attrs = "", hint = "") =>
    `<label class="field ${attrs.includes("data-full") ? "full" : ""}" for="f-${name}">${label}<select id="f-${name}" name="${name}" ${attrs}>${options.map((x) => `<option value="${h(x[0])}" ${String(x[0]) === String(value) ? "selected" : ""}>${h(x[1])}</option>`).join("")}</select>${hint ? `<small>${hint}</small>` : ""}</label>`;
  const check = (label, name, checked = false) =>
    `<label class="check"><input type="checkbox" name="${name}" ${checked ? "checked" : ""}>${label}</label>`;
  const unitSelect = (value = "", includeAll = false) =>
    select(
      "Unidade do clube",
      "unitId",
      [
        ...(includeAll ? [["", "Todas as unidades"]] : []),
        ...operatingUnits()
          .filter((u) => u.active || u.id === value)
          .map((u) => [u.id, u.name]),
      ],
      value || D.actor.unitId || operatingUnits()[0]?.id || "",
      "required",
    );
  const formSection = (n, title, body) =>
    `<section class="card form-section"><div class="section-title"><span class="section-number">${n}</span><h2>${title}</h2></div><div class="fields">${body}</div></section>`;
  const formActions = (back, label = "Salvar cadastro") =>
    `<div class="form-actions">${link("Cancelar", back, "btn secondary", "")}<button type="submit" class="btn">${icon("check")}${label}</button></div>`;
  const formError = () => '<div class="form-error" role="alert"></div>';
  const hidden = (n, v) => `<input type="hidden" name="${n}" value="${h(v)}">`;
  const header = (title, subtitle, buttons = "") =>
    `<div class="page-head"><div><h1>${title}</h1><p>${subtitle}</p></div>${buttons}</div>`;
  const crumb = (text, back) =>
    `<div class="crumb">${link("Início", "inicio", "", "")}${icon("chevron")}${back ? link(text, back, "", "") : h(text)}${back ? icon("chevron") + "<span>Detalhes</span>" : ""}</div>`;
  const empty = (title, description, ic = "search") =>
    `<div class="empty card"><div class="empty-icon">${icon(ic)}</div><h3>${title}</h3><p>${description}</p>${action("Limpar filtros", "clear", "", "secondary sm")}</div>`;
  const search = (placeholder, extra = "") =>
    `<div class="toolbar"><label class="search" for="search"><span>${icon("search")}</span><input id="search" type="search" placeholder="${placeholder}" aria-label="${placeholder}" data-filter="q" value="${h(getFilters().q)}"></label>${extra}<span class="count" id="count"></span></div>`;
  function unitFilters(flags = false) {
    const f = getFilters();
    return `<div class="card filters"><div class="filter-line"><span class="filter-label">Unidades</span>${operatingUnits()
      .map(
        (u) =>
          `<label class="check"><input type="checkbox" data-unit-filter="${u.id}" ${f.units.includes(u.id) ? "checked" : ""}>${h(u.acronym + " · " + u.name.replace("Clube ", ""))}</label>`,
      )
      .join("")}</div>${
      flags
        ? `<div class="filter-divider"></div><div class="filter-line"><span class="filter-label">Indicações</span>${[
            ["pending", "Convidados pendentes"],
            ["late", "Mensalidades atrasadas"],
            ["suspended", "Sócios suspensos"],
          ]
            .map(
              ([id, title]) =>
                `<label class="check"><input type="checkbox" data-flag-filter="${id}" ${f.flags.includes(id) ? "checked" : ""}>${title}</label>`,
            )
            .join("")}</div>`
        : ""
    }</div>`;
  }
  const matches = (obj, extra = "") =>
    V.norm(JSON.stringify(obj) + " " + extra).includes(V.norm(getFilters().q));
  const inUnits = (id) =>
    !getFilters().units.length || getFilters().units.includes(id);
  function paginate(rows) {
    const f = getFilters(),
      max = Math.max(1, Math.ceil(rows.length / 10));
    f.page = Math.min(f.page, max);
    const page = rows.slice((f.page - 1) * 10, f.page * 10);
    const footer = `<div class="pagination"><span>${rows.length ? `${(f.page - 1) * 10 + 1}–${Math.min(f.page * 10, rows.length)} de ${rows.length} registros` : "Nenhum registro"}</span><div class="pager-buttons"><button class="icon-btn" type="button" data-action="page-prev" aria-label="Página anterior" ${f.page === 1 ? "disabled" : ""}>${icon("left")}</button><button class="icon-btn" type="button" data-action="page-next" aria-label="Próxima página" ${f.page === max ? "disabled" : ""}>${icon("chevron")}</button></div></div>`;
    return { page, footer };
  }
  function toast(text, error = false) {
    const e = document.createElement("div");
    e.className = "toast" + (error ? " error" : "");
    e.innerHTML = icon(error ? "alert" : "check") + `<span>${h(text)}</span>`;
    $("#toasts").append(e);
    setTimeout(() => e.remove(), 6000);
  }
  function modal(title, body) {
    modalAction = null;
    dialog.innerHTML = `<div class="dialog-head"><h2 id="dialog-title">${title}</h2><button type="button" class="icon-btn" data-action="close-dialog" aria-label="Fechar">${icon("close")}</button></div><div class="dialog-body">${body}</div>`;
    if (!dialog.open) dialog.showModal();
  }
  function confirmAction(
    title,
    message,
    label,
    callback,
    danger = false,
    needsReason = false,
  ) {
    modal(
      title,
      `<form data-form="confirm">${formError()}<p>${h(message)}</p>${needsReason ? area("Motivo", "reason", "", 'required maxlength="1000"') : ""}<div class="dialog-actions">${action("Cancelar", "close-dialog", "", "secondary")}<button type="submit" class="btn ${danger ? "danger" : ""}">${h(label)}</button></div></form>`,
    );
    modalAction = callback;
  }
  const NAV = [
    ["unidades", "building", "Unidades"],
    ["administradores", "shield", "Administradores"],
    ["eventos", "calendar", "Eventos"],
    ["votacoes", "vote", "Votações"],
    ["socios", "users", "Sócios"],
    ["solicitacoes", "file", "Solicitações"],
    ["reclamacoes", "alert", "Reclamações"],
    ["chat", "chat", "Chat"],
    ["planos", "layers", "Planos"],
  ];
  function shell(content) {
    return `<header class="top-shell"><div class="topbar"><div class="flex"><a class="brand" href="#inicio" aria-label="VanderClub, início"><img src="assets/logo.svg" alt=""><div><div class="brand-name">VanderClub<span style="color:#97bfb9">.</span></div><div class="brand-caption">Portal de administração</div></div></a><div class="top-scope">${isGeneral() ? "GESTÃO DA REDE" : "SUA UNIDADE"}<strong>${isGeneral() ? "Todos os clubes, um só lugar" : h(unitName(D.actor.unitId))}</strong></div></div><div class="top-actions"><button class="btn secondary sm" data-action="refresh">${icon("refresh")}Atualizar</button><button class="icon-btn notification-btn" data-action="notifications" aria-label="Ver pendências">${icon("bell")}</button><div class="user">${avatar(D.actor.name, "circle")}<div><strong>${h(D.actor.name)}</strong><small>${role(D.actor.role)}${isGeneral() ? " · Presidente" : ""}</small></div></div><button class="icon-btn" data-action="logout" aria-label="Sair da conta" title="Sair da conta">${icon("logout")}</button></div></div><div class="navbar-border"><nav class="navbar" aria-label="Navegação principal">${NAV.map(([key, ic, label]) => `<a class="nav-item ${section === key ? "active" : ""}" href="#${key}" ${section === key ? 'aria-current="page"' : ""}>${icon(ic)}${label}</a>`).join("")}</nav></div></header><main id="main" class="page-body" tabindex="-1">${content}</main><footer class="footer"><span>© ${new Date().getFullYear()} VanderClub · Uma rede de bons momentos.</span><div class="flex wrap gap8"><span>Gestão da rede VanderClub</span><span>·</span>${localPackage ? "" : '<a href="downloads/VanderClub_Web_v2_0.zip" download>Código-fonte</a><span>·</span>'}<button data-action="about">Sobre esta versão</button></div></footer>`;
  }
  function loginPage() {
    app.innerHTML = `<main class="login" id="main"><section class="login-story"><div class="brand"><img src="assets/logo.svg" alt=""><div><div class="brand-name">VanderClub.</div><div class="brand-caption">Uma rede de bons momentos</div></div></div><img class="login-art" src="assets/social.svg" alt="Ilustração de um clube com jardim e espaços de convivência"><h1>Um clube é feito<br>de pessoas.</h1><p>Conectamos unidades, aproximamos associados e cuidamos dos detalhes para que os bons momentos aconteçam.</p><div class="story-footer">${icon("heart")} Mais convivência. Mais comunidade. Mais VanderClub.</div></section><section class="login-panel"><div class="login-form"><div class="eyebrow">Portal administrativo</div><h2>Que bom ter<br>você por aqui.</h2><p>Entre para cuidar da sua unidade e das pessoas que fazem parte dela.</p><form data-form="login">${formError()}${field("E-mail", "login", "", "email", 'required autocomplete="username" placeholder="seunome@vanderclub.com"')}<label class="field" for="f-password">Senha<div class="password-wrap"><input id="f-password" name="password" type="password" required autocomplete="current-password" placeholder="Digite sua senha"><button type="button" data-action="show-password" aria-label="Mostrar senha">${icon("eye")}</button></div></label><button type="submit" class="btn full">Entrar no portal ${icon("arrow")}</button></form><div class="login-help">Acesso exclusivo à administração.<br>Para solicitar uma conta, procure o Presidente.</div><div class="login-legal">${icon("lock")}Acesso com segurança. Seus dados são consultados no serviço VanderClub.</div></div></section></main>`;
  }
  function homePage() {
    const active = D.contracts.filter(
        (c) => contractStatus(c) === "Ativo",
      ).length,
      waiting = D.people.filter((p) => p.approval === "Pendente"),
      openTickets = D.tickets.filter((t) => !t.responses.length);
    const upcoming = D.events
      .filter((e) => !e.cancelledAt && e.start > now())
      .sort((a, b) => a.start.localeCompare(b.start))
      .slice(0, 3);
    const late = D.contracts.filter((c) => overdue(c).length);
    const first = D.actor.name.split(" ")[0];
    return `${header("Olá, " + h(first) + ". Vamos cuidar do clube?", "Acompanhe o que acontece e faça os bons momentos continuarem.", `<div class="welcome-date">${icon("calendar")}${new Intl.DateTimeFormat("pt-BR", { dateStyle: "long", timeZone: "America/Sao_Paulo" }).format(new Date())}</div>`)}<section class="card hero"><div class="hero-copy"><div class="eyebrow">Esporte · Lazer · Comunidade</div><h2>Sua rede,<br>mais próxima.</h2><p>Um lugar para se encontrar, compartilhar histórias e viver bem. Juntos, fazemos de cada unidade um clube para chamar de nosso.</p><div class="flex gap8">${link("Gerenciar sócios", "socios", "btn", "users")}${link("Ver programação", "eventos", "btn secondary", "arrow")}</div></div><div class="hero-image"><img src="assets/social.svg" alt="Ilustração da sede do clube entre árvores"><div class="hero-stamp">${icon("heart")}<div><strong>Mais do que um clube. Uma comunidade.</strong><span>Cuidar das pessoas é o que nos conecta.</span></div></div></div></section><section class="stat-grid" aria-label="Resumo da operação">${[
      [
        operatingUnits().filter((u) => u.active).length,
        "Unidades em operação",
        "building",
        "unidades",
      ],
      [active, "Sócios titulares ativos", "users", "socios"],
      [waiting.length, "Convidados aguardando", "shield", "socios"],
      [openTickets.length, "Atendimentos pendentes", "chat", "solicitacoes"],
    ]
      .map(
        ([n, title, ic, r]) =>
          `<a href="#${r}" class="card stat"><div class="stat-icon">${icon(ic)}</div><div><strong>${n}</strong><span>${title}</span></div></a>`,
      )
      .join(
        "",
      )}</section><div class="dashboard-bottom"><section class="card card-pad"><div class="section-head"><h2>Próximos encontros</h2>${link("Ver todos", "eventos")}</div>${upcoming.length ? upcoming.map((e) => `<a href="#eventos/${e.id}" class="event-row">${dateTile(e.start, e.unitId)}<div class="grow"><h3>${h(e.title)}</h3><p>${h(unitName(e.unitId))} · ${formatTime(e.start, e.unitId)}–${formatTime(e.end, e.unitId)}</p></div>${tag(e.audience === "PUBLICO" ? "Público aberto" : "Associados", e.audience === "PUBLICO" ? "teal" : "gray")}</a>`).join("") : '<p class="muted small">Nenhum evento próximo. Que tal planejar um encontro?</p>'}</section><section class="card card-pad"><div class="section-head"><h2>Precisa de você</h2>${tag(waiting.length + openTickets.length + " pendências", "amber")}</div><a class="attention" href="#socios"><span class="bubble-icon">${icon("shield")}</span><div><h3>${waiting.length} cadastros para analisar</h3><p>Novos convidados e dependentes.</p></div>${icon("chevron", "arrow")}</a><a class="attention" href="#solicitacoes"><span class="bubble-icon">${icon("file")}</span><div><h3>${openTickets.length} contatos aguardam resposta</h3><p>Um retorno faz toda a diferença.</p></div>${icon("chevron", "arrow")}</a><a class="attention" href="#socios"><span class="bubble-icon">${icon("wallet")}</span><div><h3>${late.length} contratos com parcelas atrasadas</h3><p>Acompanhe a situação dos associados.</p></div>${icon("chevron", "arrow")}</a></section></div><section class="mt"><div class="section-head"><h2>${isGeneral() ? "Conheça nossa rede" : "Sua unidade, nosso ponto de encontro"}</h2>${link("Explorar unidades", "unidades")}</div><div class="grid3">${operatingUnits()
      .map(
        (u) =>
          `<a href="#unidades/${u.id}" class="unit-mini"><img src="assets/${h(u.image)}" alt="Ilustração da unidade"><div><h3>${h(u.name)}</h3><p>${h(u.city)} · ${h(u.uf)} &nbsp; ${u.active ? "Em atividade" : "Inativa"}</p></div>${icon("chevron")}</a>`,
      )
      .join("")}</div></section>`;
  }
  function unitsPage() {
    const list = D.units.filter((u) => matches(u));
    return `${crumb("Unidades")}${header("Nossa rede de clubes", "Lugares diferentes. O mesmo jeito de acolher.", isGeneral() ? link("Novo Clube +", "unidades/novo", "btn", "plus") : "")}${search("Buscar por nome, sigla, cidade, contato…", tag(D.units.length + " unidades", "gray"))}<div class="grid3">${list.length ? list.map((u) => `<article class="card unit-card"><div class="unit-cover"><img src="assets/${h(u.image)}" alt="Ilustração da unidade">${tag(u.active ? "Em atividade" : "Inativa", u.active ? "green" : "gray")}</div><div class="unit-content"><div class="unit-code">${h(u.acronym)}</div><h2>${h(u.name)}</h2><div class="unit-info">${icon("map")}<span>${h(u.street)}, ${h(u.number)} · ${h(u.district)}<br>${h(u.city)} — ${h(u.uf)} · CEP ${h(u.cep)}</span></div><div class="unit-info">${icon("phone")}<span>${h(u.phone)}</span></div><div class="unit-info">${icon("mail")}<span>${h(u.email)}</span></div><div class="unit-actions">${link("Ver detalhes", "unidades/" + u.id)}${isGeneral() ? link("Editar", "unidades/" + u.id + "/editar", "btn secondary sm", "edit") : ""}</div></div></article>`).join("") : empty("Nenhuma unidade encontrada", "Tente pesquisar por outro nome, cidade ou contato.")}</div>`;
  }
  function adminsPage() {
    const list = D.admins
      .filter((a) => matches({ ...a, password: undefined }, unitName(a.unitId)))
      .sort(
        (a, b) =>
          (a.role === "ADM_GERAL" ? -1 : 0) -
            (b.role === "ADM_GERAL" ? -1 : 0) || a.name.localeCompare(b.name),
      );
    const { page, footer } = paginate(list);
    return `${crumb("Administradores")}${header("Pessoas que cuidam da rede", "Conheça a equipe e os responsáveis por cada unidade.", isGeneral() ? link("Novo Administrador +", "administradores/novo", "btn", "plus") : "")}${search("Buscar por nome, unidade, telefone ou e-mail…", tag(D.admins.filter((a) => a.active).length + " ativos", "gray"))}${list.length ? `<div class="table-wrap"><table class="table"><thead><tr><th>Administrador</th><th>Perfil / unidade</th><th>Contato</th><th>Situação</th><th></th></tr></thead><tbody>${page.map((a) => `<tr class="${a.role === "ADM_GERAL" ? "president" : ""}"><td class="person-cell"><div class="flex">${avatar(a.name, a.role === "ADM_GERAL" ? "" : "teal")}<div><strong>${h(a.name)}</strong><small>${a.role === "ADM_GERAL" ? "Presidente da rede" : "Gestão da unidade"}</small></div></div></td><td>${tag(role(a.role))}<div class="small muted" style="margin-top:5px">${h(unitName(a.unitId))}</div></td><td><div>${h(a.email)}</div><div class="small muted">${h(a.phone)}</div></td><td>${tag(a.active ? "Ativo" : "Inativo", a.active ? "green" : "gray")}</td><td>${link(isGeneral() ? "Ver / editar" : "Ver detalhes", "administradores/" + a.id, "btn secondary sm", "")}</td></tr>`).join("")}</tbody></table></div>${footer}` : empty("Nenhum administrador encontrado", "Tente outro nome ou unidade.")}`;
  }
  function eventStatus(e) { return e.status; }
  function eventsPage() {
    const f = getFilters();
    const list = D.events
      .filter(
        (e) =>
          inUnits(e.unitId) &&
          matches(
            e,
            unitName(e.unitId) +
              " " +
              when(e.start, e.unitId) +
              " " +
              when(e.end, e.unitId),
          ) &&
          (!f.type || eventStatus(e) === f.type),
      )
      .sort(
        (a, b) =>
          (a.end < now()) - (b.end < now()) || a.start.localeCompare(b.start),
      );
    return `${crumb("Eventos")}${header("Momentos para compartilhar", "A programação organizada pelo clube, para aproximar pessoas.", link("Novo Evento +", "eventos/novo", "btn", "plus"))}${unitFilters()}${search("Buscar por evento, unidade, data ou local…", `<select class="btn secondary" data-filter="type" aria-label="Filtrar eventos por situação">${["", "Agendado", "Confirmado", "Realizado", "Cancelado"].map((s) => `<option value="${s}" ${f.type === s ? "selected" : ""}>${s || "Todas as situações"}</option>`).join("")}</select>`)}<div class="grid3">${list.length ? list.map((e) => `<article class="card event-card"><div class="event-cover"><img src="assets/${h(e.image)}" alt="Ilustração do espaço do evento">${tag(eventStatus(e), eventStatus(e) === "Agendado" ? "teal" : eventStatus(e) === "Cancelado" ? "red" : "gray")}</div><div class="event-body"><div class="eyebrow">${h(unit(e.unitId).acronym)} · ${e.audience === "PUBLICO" ? "Público aberto" : "Exclusivo para associados"}</div><h2>${h(e.title)}</h2><div class="event-meta">${icon("calendar")}${formatDate(e.start, e.unitId)}${formatDate(e.start, e.unitId) !== formatDate(e.end, e.unitId) ? " a " + formatDate(e.end, e.unitId) : ""}</div><div class="event-meta">${icon("clock")}${formatTime(e.start, e.unitId)} às ${formatTime(e.end, e.unitId)}</div><div class="event-meta">${icon("map")}${h(e.location)}</div><div class="bottom">${link("Ver evento", "eventos/" + e.id)}<span class="tiny muted">${e.capacity} lugares</span></div></div></article>`).join("") : empty("Nenhum evento por aqui", "Altere os filtros ou crie um novo encontro.", "calendar")}</div>`;
  }
  function pollsPage() {
    const f = getFilters(),
      list = D.polls
        .filter(
          (p) =>
            inUnits(p.unitId) &&
            matches(
              p,
              unitName(p.unitId) +
                " " +
                when(p.start, p.unitId) +
                " " +
                when(p.end, p.unitId),
            ) &&
            (!f.type || V.pollStatus(p, now()) === f.type),
        )
        .sort((a, b) => b.start.localeCompare(a.start));
    return `${crumb("Votações")}${header("Decisões construídas juntos", "Crie consultas, acompanhe a participação e confira os resultados.", link("Nova Votação +", "votacoes/nova", "btn", "plus"))}${unitFilters()}${search("Buscar por motivo, unidade ou data…", `<select class="btn secondary" data-filter="type" aria-label="Filtrar votações por status">${["", "Aberta", "Agendada", "Encerrada"].map((s) => `<option ${f.type === s ? "selected" : ""} value="${s}">${s || "Todos os status"}</option>`).join("")}</select>`)}<div class="grid3">${
      list.length
        ? list
            .map((p) => {
              const votes = D.votes.filter((v) => v.pollId === p.id);
              const status = V.pollStatus(p, now());
              return `<article class="card poll-card"><div class="between">${tag(status, status === "Aberta" ? "green" : status === "Agendada" ? "amber" : "gray")}<span class="tiny muted">${h(unit(p.unitId).acronym)}</span></div><h2>${h(p.title)}</h2><p>${h(p.description.slice(0, 145))}${p.description.length > 145 ? "…" : ""}</p><div class="event-meta">${icon("calendar")}De ${when(p.start, p.unitId)}</div><div class="event-meta">${icon("clock")}Até ${when(p.end, p.unitId)}</div><p class="text-teal small">${votes.length} voto${votes.length === 1 ? "" : "s"} registrado${votes.length === 1 ? "" : "s"}${status === "Encerrada" ? " · " + h(pollResult(p)) : ""}</p><div class="bottom">${link(status === "Encerrada" ? "Ver resultado" : "Acompanhar votação", "votacoes/" + p.id)}</div></article>`;
            })
            .join("")
        : empty(
            "Nenhuma votação encontrada",
            "Altere os filtros ou crie uma nova consulta.",
            "vote",
          )
    }</div>`;
  }
  function pollResult(p) {
    const votes = D.votes.filter((v) => v.pollId === p.id),
      counts = p.options.map((o) => ({
        ...o,
        total: votes.filter((v) => v.optionId === o.id).length,
      }));
    if (
      votes.length < Math.ceil(((p.electorate?.length || 0) * p.quorum) / 100)
    )
      return "Quórum não atingido";
    if (!votes.length) return "Sem votos";
    const max = Math.max(...counts.map((o) => o.total)),
      winners = counts.filter((o) => o.total === max);
    return winners.length > 1
      ? "Empate entre as opções mais votadas"
      : winners[0].text;
  }
  function membersPage() {
    const f = getFilters();
    const list = D.people
      .filter((p) => {
        const c = contract(p);
        return (
          inUnits(c.unitId) &&
          matches(p, c.snapshot.name + " " + unitName(c.unitId)) &&
          (!f.type || p.type === f.type) &&
          (!f.flags.length ||
            f.flags.some((flag) =>
              flag === "pending"
                ? p.type === "Titular"
                  ? pending(c).length
                  : p.approval === "Pendente"
                : flag === "late"
                  ? overdue(c).length
                  : contractStatus(c) === "Suspenso",
            ))
        );
      })
      .sort(
        (a, b) =>
          (a.type === "Titular" ? -1 : 0) - (b.type === "Titular" ? -1 : 0) ||
          a.name.localeCompare(b.name),
      );
    const { page, footer } = paginate(list);
    return `${crumb("Sócios")}${header("Quem faz parte do clube", "Acompanhe cadastros, convidados e a situação de cada associação.", tag(D.contracts.length + " contratos", "teal"))}${unitFilters(true)}${search("Buscar por nome, plano, telefone ou e-mail…", `<select class="btn secondary" data-filter="type" aria-label="Filtrar tipo de sócio">${["", "Titular", "Convidado", "Dependente"].map((t) => `<option value="${t}" ${f.type === t ? "selected" : ""}>${t || "Todos os tipos"}</option>`).join("")}</select>`)}${
      list.length
        ? `<div class="table-wrap"><table class="table"><thead><tr><th>Sócio</th><th>Tipo / unidade</th><th>Contato</th><th>Plano contratado</th><th></th></tr></thead><tbody>${page
            .map((p) => {
              const c = contract(p),
                np =
                  p.type === "Titular"
                    ? pending(c).length
                    : p.approval === "Pendente"
                      ? 1
                      : 0;
              return `<tr class="${np ? "flagged" : ""}"><td class="person-cell"><div class="flex">${avatar(p.name, p.type === "Titular" ? "" : "teal")}<div><div class="flex gap8"><strong>${h(p.name)}</strong>${np ? `<span title="Convidado aguardando aprovação" aria-label="Convidado aguardando aprovação">${icon("alert", "alert-icon")}</span>` : ""}</div><small>${contractStatus(c)}${p.type !== "Titular" ? " · " + p.approval : ""}</small>${np ? '<span class="inline-alert">' + np + " cadastro(s) para analisar</span>" : ""}</div></div></td><td>${tag(p.type, "gray")}<div class="tiny muted" style="margin-top:5px">${h(unit(c.unitId).acronym)} · ${h(unit(c.unitId).city)}</div></td><td><div>${h(p.phone)}</div><div class="tiny muted">${h(p.email)}</div></td><td><strong class="small">${h(c.snapshot.name)}</strong><div class="tiny muted">Versão ${c.snapshot.number} · ${V.money(c.condition.totalCents / c.condition.installments)} / parcela</div>${overdue(c).length ? tag(overdue(c).length + " parcela(s) atrasada(s)", "red") : ""}</td><td>${link("Ver detalhes", "socios/" + p.id, "btn secondary sm", "")}</td></tr>`;
            })
            .join("")}</tbody></table></div>${footer}`
        : empty(
            "Nenhum sócio encontrado",
            "Ajuste a pesquisa ou as indicações selecionadas.",
            "users",
          )
    }`;
  }
  function ticketsPage(type) {
    const f = getFilters(),
      all = D.tickets.filter(
        (t) =>
          t.type === type &&
          inUnits(t.unitId) &&
          matches(
            t,
            (D.people.find((p) => p.id === t.personId)?.name || "") +
              " " +
              unitName(t.unitId) +
              " " +
              when(t.createdAt, t.unitId),
          ),
      );
    const list = all
      .filter(
        (t) =>
          f.tab === "Todas" ||
          (f.tab === "Pendentes" ? !t.responses.length : !!t.responses.length),
      )
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    const { page, footer } = paginate(list);
    return `${crumb(type === "Solicitação" ? "Solicitações" : "Reclamações")}${header(type === "Solicitação" ? "A escuta que aproxima" : "Cuidar começa por ouvir", type === "Solicitação" ? "Responda às solicitações e acompanhe cada atendimento." : "Receba os relatos dos associados e dê um retorno atencioso.")}${unitFilters()}${search("Buscar por protocolo, motivo, sócio ou unidade…")}<div class="tabs" role="tablist" aria-label="Situação do atendimento">${["Todas", "Pendentes", "Respondidas"].map((t) => `<button type="button" role="tab" aria-selected="${f.tab === t}" class="tab ${f.tab === t ? "active" : ""}" data-tab="${t}">${t}<span>${t === "Todas" ? all.length : t === "Pendentes" ? all.filter((x) => !x.responses.length).length : all.filter((x) => x.responses.length).length}</span></button>`).join("")}</div>${list.length ? `<div class="table-wrap"><table class="table"><thead><tr><th>Protocolo / motivo</th><th>Associado</th><th>Unidade</th><th>Recebido em</th><th>${type === "Solicitação" ? "Status" : "Resposta"}</th><th></th></tr></thead><tbody>${page.map((t) => `<tr><td class="person-cell"><div class="tiny text-teal strong">${h(t.number)}</div><strong>${h(t.title)}</strong></td><td>${h(D.people.find((p) => p.id === t.personId)?.name)}</td><td>${h(unit(t.unitId).acronym)}</td><td>${formatDate(t.createdAt, t.unitId)}<div class="tiny muted">${formatTime(t.createdAt, t.unitId)}</div></td><td>${tag(t.responses.length ? (type === "Solicitação" ? "Atendida" : "Respondida") : type === "Solicitação" ? "Pendente" : "Aguardando retorno", t.responses.length ? "green" : "amber")}</td><td>${link(t.responses.length ? "Ver detalhes" : "Responder", section + "/" + t.id, "btn secondary sm", "")}</td></tr>`).join("")}</tbody></table></div>${footer}` : empty(f.tab === "Pendentes" ? "Tudo respondido por aqui" : "Nenhum atendimento encontrado", "Altere os filtros para consultar outros registros.", type === "Solicitação" ? "file" : "chat")}`;
  }
  function plansPage() {
    const f = getFilters(),
      list = D.plans.filter(
        (p) =>
          (f.type === "arquivados" ? !p.active : p.active) &&
          matches(currentVersion(p), p.code),
      );
    return `${crumb("Planos")}${header("Um plano para cada momento", "Ofertas da rede, com benefícios que acompanham cada associado.", isGeneral() ? link("Novo Plano +", "planos/novo", "btn", "plus") : "")}${search("Buscar por nome, código ou benefício…", `<select class="btn secondary" data-filter="type" aria-label="Ofertas de planos"><option value="">Planos disponíveis</option><option value="arquivados" ${f.type === "arquivados" ? "selected" : ""}>Excluídos da oferta</option></select>`)}<div class="grid3">${
      list.length
        ? list
            .map((p, i) => {
              const v = currentVersion(p),
                installment = v.conditions[0],
                cash = v.conditions[1];
              return `<article class="card plan-card ${v.scope === "REDE" ? "featured" : ""} ${p.active ? "" : "archived"}"><div class="between">${tag(v.scope === "REDE" ? "Toda a rede" : "Unidade de origem", v.scope === "REDE" ? "teal" : "gray")}<span class="tiny muted">v${v.number}</span></div><h2>${h(v.name)}</h2><p>${h(v.description)}</p><div class="price">${V.money(installment.totalCents / installment.installments)}<small> / parcela</small></div><div class="small muted">${installment.installments} parcelas · ${v.durationMonths} meses de clube</div><div class="cash">ou ${V.money(cash.totalCents)} à vista</div><ul class="features"><li>${icon("users")}Até ${v.quota} convidados e dependentes</li>${v.benefits
                .split("\n")
                .filter(Boolean)
                .slice(0, 4)
                .map((b) => `<li>${icon("check")}${h(b)}</li>`)
                .join(
                  "",
                )}</ul><div class="actions">${link("Ver detalhes", "planos/" + p.id, "btn secondary sm", "")}${isGeneral() && p.active ? link("Editar", "planos/" + p.id + "/editar", "btn soft sm", "edit") : ""}${isGeneral() && p.active ? action("Excluir", "archive-plan", p.id, "danger sm", "trash") : ""}</div></article>`;
            })
            .join("")
        : empty(
            "Nenhum plano encontrado",
            "Altere a pesquisa ou confira os planos excluídos.",
            "layers",
          )
    }</div><div class="notice">${icon("shield")}Alterações criam uma nova versão. Quem já contratou mantém os valores e benefícios originais até o fim do contrato.</div>`;
  }
  const UFS =
    "AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO".split(
      " ",
    );
  const addressFields = (p = {}) =>
    field(
      "CEP",
      "cep",
      p.cep,
      "text",
      'required inputmode="numeric" maxlength="9" placeholder="00000000"',
    ) +
    field(
      "Logradouro",
      "street",
      p.street,
      "text",
      'required maxlength="200" placeholder="Rua, avenida…"',
    ) +
    field("Número", "number", p.number, "text", 'required maxlength="20"') +
    field(
      'Complemento <span class="muted">· opcional</span>',
      "complement",
      p.complement,
      "text",
      'maxlength="100"',
    ) +
    field(
      "Bairro",
      "district",
      p.district,
      "text",
      'required maxlength="100"',
    ) +
    field("Cidade", "city", p.city, "text", 'required maxlength="150"') +
    select(
      "UF",
      "uf",
      UFS.map((x) => [x, x]),
      p.uf || "PR",
      "required",
    );
  const personalFields = (p = {}) =>
    field(
      "Nome completo",
      "name",
      p.name,
      "text",
      'required maxlength="200" autocomplete="name"',
    ) +
    field(
      "CPF",
      "cpf",
      p.cpf,
      "text",
      'required maxlength="14" inputmode="numeric" placeholder="Somente números"',
    ) +
    field(
      "Nascimento",
      "birth",
      p.birth,
      "date",
      'required max="' + V.today(now()) + '"',
    ) +
    field(
      "Telefone com DDD",
      "phone",
      p.phone,
      "tel",
      'required maxlength="20"',
    ) +
    field(
      "E-mail para contato",
      "email",
      p.email,
      "email",
      'required maxlength="254"',
    ) +
    select(
      'Estado civil <span class="muted">· opcional</span>',
      "maritalStatus",
      [
        ["", "Não informado"],
        ["Solteiro(a)", "Solteiro(a)"],
        ["Casado(a)", "Casado(a)"],
        ["Divorciado(a)", "Divorciado(a)"],
        ["Viúvo(a)", "Viúvo(a)"],
        ["Outro", "Outro"],
      ],
      p.maritalStatus,
    ) +
    select(
      'Gênero <span class="muted">· opcional</span>',
      "gender",
      [
        ["", "Prefiro não informar"],
        ["Feminino", "Feminino"],
        ["Masculino", "Masculino"],
        ["Outro", "Outro"],
      ],
      p.gender,
    );
  const kv = (items) =>
    `<dl class="kv">${items.map(([k, v, full]) => `<div class="${full ? "full" : ""}"><dt>${h(k)}</dt><dd>${h(v ?? "—")}</dd></div>`).join("")}</dl>`;
  const addressText = (p) =>
    `${p.street || ""}, ${p.number || ""}${p.complement ? " · " + p.complement : ""} · ${p.district || ""} · ${p.city || ""}/${p.uf || ""} · CEP ${p.cep || ""}`;
  function unitForm(u = {}) {
    if (!isGeneral()) return forbidden();
    return `${crumb("Unidades", "unidades")}${header(u.id ? "Editar unidade" : "Um novo lugar para pertencer", "Preencha os dados da unidade que fará parte da rede.")}
<form data-form="unit">${formError()}${hidden("id", u.id || "")}${hidden("version", u.version || "")}<div class="form-layout"><div>${formSection(
      "01",
      "Identificação do clube",
      field(
        "Nome do clube",
        "name",
        u.name,
        "text",
        'required maxlength="150"',
      ) +
        field(
          "Sigla",
          "acronym",
          u.acronym,
          "text",
          'required maxlength="20"',
        ) +
        area(
          "Descrição",
          "description",
          u.description,
          'required maxlength="2000"',
        ) +
        select(
          "Imagem ilustrativa",
          "image",
          [
            ["social.svg", "Sede e jardim"],
            ["quiosque.svg", "Lazer e natureza"],
            ["futebol.svg", "Espaços esportivos"],
          ],
          u.image || "social.svg",
        ) +
        `<div class="field">Situação${check("Unidade em atividade", "active", u.active !== false)}</div>`,
    )}${formSection(
      "02",
      "Endereço e contatos",
      addressFields(u) +
        field("Telefone", "phone", u.phone, "tel", "required") +
        field("E-mail da unidade", "email", u.email, "email", "required") +
        select(
          "Fuso horário",
          "timezone",
          [
            ["America/Sao_Paulo", "Brasília — America/Sao_Paulo"],
            ["America/Manaus", "Manaus — America/Manaus"],
            ["America/Rio_Branco", "Rio Branco — America/Rio_Branco"],
            ["America/Noronha", "Noronha — America/Noronha"],
            ["America/Campo_Grande", "Campo Grande — America/Campo_Grande"],
          ],
          u.timezone || "America/Sao_Paulo",
          "required",
          "Eventos e votações usam o horário local da unidade.",
        ),
    )}${formActions("unidades")}</div><aside class="note">${icon("building")}<h3>Uma unidade, muitas possibilidades.</h3><p>Este cadastro identifica o clube nos eventos, atendimentos e conversas da rede.</p><p>Os administradores locais serão vinculados à unidade na área Administradores.</p></aside></div></form>`;
  }
  function unitDetail(u) {
    return `${crumb("Unidades", "unidades")}${header(h(u.name), "Conheça os dados e os contatos desta unidade.", isGeneral() ? link("Editar unidade", "unidades/" + u.id + "/editar", "btn", "edit") : "")}<div class="detail-grid"><section class="card card-pad"><img class="cover-detail" src="assets/${h(u.image)}" alt="Ilustração da unidade"><div class="between"><h2>${h(u.name)}</h2>${tag(u.active ? "Em atividade" : "Inativa", u.active ? "green" : "gray")}</div><p class="small muted preline">${h(u.description)}</p>${kv(
      [
        ["Sigla", u.acronym],
        ["Cidade / UF", u.city + " / " + u.uf],
        ["Telefone", u.phone],
        ["E-mail", u.email],
        ["Endereço", addressText(u), true],
        ["Fuso horário", u.timezone, true],
      ],
    )}</section><aside class="card card-pad"><h2>Quem cuida desta unidade</h2><p class="small muted">Equipe administrativa local.</p>${
      D.admins
        .filter((a) => a.unitId === u.id && a.active)
        .map(
          (a) =>
            `<div class="beneficiary">${avatar(a.name, "teal")}<div><div class="name">${h(a.name)}</div><p>${h(a.email)}<br>${h(a.phone)}</p></div></div>`,
        )
        .join("") ||
      '<p class="small muted">Nenhum administrador local vinculado.</p>'
    }</aside></div>`;
  }
  function adminForm(a = {}) {
    if (!isGeneral())
      return `${crumb("Administradores", "administradores")}${header(h(a.name), "Contato da equipe administrativa.")}<section class="card card-pad">${kv(
        [
          ["Nome", a.name],
          ["Classe", role(a.role)],
          ["Unidade", unitName(a.unitId)],
          ["Telefone", a.phone],
          ["E-mail", a.email],
          ["Situação", a.active ? "Ativo" : "Inativo"],
        ],
      )}</section>`;
    return `${crumb("Administradores", "administradores")}${header(a.id ? "Cadastro do administrador" : "Uma nova pessoa para cuidar do clube", a.id ? "Atualize os dados e o vínculo com a unidade." : "Crie uma conta de ADM Local e defina sua unidade de atuação.")}<form data-form="admin">${formError()}${hidden("id", a.id || "")}${hidden("version", a.version || "")}<div class="form-layout"><div>${formSection("01", "Dados pessoais", personalFields(a))}${formSection("02", "Endereço", addressFields(a))}${formSection("03", "Acesso e unidade", field("E-mail para login", "login", a.login, "email", 'required autocomplete="off"') + (a.role === "ADM_GERAL" ? field("Classe", "role", "ADM Geral / Presidente", "text", "readonly") : unitSelect(a.unitId)) + field(a.id ? "Nova senha · opcional" : "Senha", "password", "", "password", (a.id ? "" : "required ") + 'minlength="8" maxlength="128" autocomplete="new-password"', "Use letras e números, com pelo menos 8 caracteres.") + field("Confirmar senha", "confirmPassword", "", "password", (a.id ? "" : "required ") + 'autocomplete="new-password"') + `<div class="field full">Situação da conta${check("Administrador ativo", "active", a.active !== false)}</div>`)}${formActions("administradores", a.id ? "Salvar alterações" : "Cadastrar administrador")}</div><aside class="note">${icon("shield")}<h3>Acesso com responsabilidade.</h3><p>O ADM Local opera apenas os registros da unidade vinculada. Não gerencia o catálogo de planos, unidades ou outros administradores.</p><p>O e-mail pessoal é usado para contato. O e-mail para login pode ser comercial, diferente ou igual ao pessoal.</p><p>A senha nunca é exibida após o cadastro. Em uma edição, deixe os campos de senha em branco para mantê-la.</p></aside></div></form>`;
  }
  function eventForm(e = {}) {
    const uid = e.unitId || D.actor.unitId || operatingUnits()[0]?.id || "",
      tz = timezone(uid),
      day = V.today(now(), tz),
      d1 = V.addDays(day, 7);
    const p = {
      ...e,
      start: e.start ? V.localInput(e.start, tz) : d1 + "T12:00",
      end: e.end ? V.localInput(e.end, tz) : d1 + "T16:00",
      regStart: e.regStart ? V.localInput(e.regStart, tz) : day + "T08:00",
      regEnd: e.regEnd
        ? V.localInput(e.regEnd, tz)
        : V.addDays(d1, -1) + "T18:00",
    };
    return `${crumb("Eventos", "eventos")}${header(e.id ? "Editar evento" : "Um novo encontro para o clube", "Organize a programação social da unidade. Eventos são diferentes das reservas dos sócios.")}<form data-form="event">${formError()}${hidden("id", e.id || "")}${hidden("version", e.version || "")}<div class="form-layout"><div>${formSection(
      "01",
      "Sobre o evento",
      field(
        "Nome do evento",
        "title",
        e.title,
        "text",
        'required maxlength="200"',
      ) +
        unitSelect(uid) +
        select("Situação", "status", [["Agendado", "Agendado"], ["Confirmado", "Confirmado"]], e.status || "Agendado") +
        area(
          "Descrição",
          "description",
          e.description,
          'required maxlength="4000"',
        ) +
        select(
          "Público",
          "audience",
          [
            ["ASSOCIADOS", "Somente associados"],
            ["PUBLICO", "Aberto ao público"],
          ],
          e.audience || "ASSOCIADOS",
        ) +
        field(
          "Capacidade de pessoas",
          "capacity",
          e.capacity || 120,
          "number",
          'required min="1" max="100000"',
        ) +
        field(
          "Local / endereço do encontro",
          "location",
          e.location,
          "text",
          'required maxlength="250"',
        ) +
        select(
          "Área do clube (opcional)",
          "areaId",
          [
            ["", "Local externo / sem ocupar área"],
            ...D.areas
              .filter((a) => a.unitId === uid)
              .map((a) => [a.id, a.name]),
          ],
          e.areaId || "",
          "",
          "Quando selecionada, o evento bloqueia essa área na agenda do clube.",
        ) +
        select(
          "Ilustração do evento",
          "image",
          [
            ["social.svg", "Salão social"],
            ["festa.svg", "Salão de festa"],
            ["futebol.svg", "Campo e esportes"],
            ["quiosque.svg", "Quiosque e natureza"],
          ],
          e.image || "festa.svg",
        ),
    )}${formSection("02", "Datas e horários", field("Data e horário de início", "start", p.start, "datetime-local", "required") + field("Data e horário de fim", "end", p.end, "datetime-local", "required") + field("Abertura das inscrições", "regStart", p.regStart, "datetime-local", "required") + field("Encerramento das inscrições", "regEnd", p.regEnd, "datetime-local", "required"))}${formActions("eventos", e.id ? "Salvar evento" : "Cadastrar evento")}</div><aside class="note teal">${icon("calendar")}<h3>Espaço para se encontrar.</h3><p>Os horários serão interpretados no fuso da unidade escolhida. O término deve ser posterior ao início.</p><p>Se o evento usar uma área do clube, conferimos os outros eventos e as reservas dos associados para evitar sobreposição.</p><p>As datas de inscrição ficam registradas; a inscrição de participantes será integrada na próxima etapa.</p></aside></div></form>`;
  }
  function eventDetail(e) {
    return `${crumb("Eventos", "eventos")}${header(h(e.title), h(unitName(e.unitId)), !e.cancelledAt && e.end > now() ? link("Editar evento", "eventos/" + e.id + "/editar", "btn", "edit") : "")}<div class="detail-grid"><section class="card card-pad"><img class="cover-detail" src="assets/${h(e.image)}" alt="Ilustração do evento"><div class="between"><h2>Sobre o encontro</h2>${tag(eventStatus(e), e.cancelledAt ? "red" : "teal")}</div><p class="preline small muted">${h(e.description)}</p>${kv(
      [
        ["Início", when(e.start, e.unitId)],
        ["Término", when(e.end, e.unitId)],
        ["Local", e.location, true],
        [
          "Público",
          e.audience === "PUBLICO" ? "Aberto ao público" : "Somente associados",
        ],
        ["Capacidade", e.capacity + " pessoas"],
        ["Inscrições abrem", when(e.regStart, e.unitId)],
        ["Inscrições encerram", when(e.regEnd, e.unitId)],
        [
          "Área vinculada",
          D.areas.find((a) => a.id === e.areaId)?.name || "Sem área reservada",
          true,
        ],
      ],
    )}${e.cancelledAt ? `<div class="notice warn">${icon("info")}Cancelado: ${h(e.cancellationReason)}</div>` : ""}</section><aside class="stack"><div class="note">${icon("calendar")}<h3>Programação da unidade</h3><p>Evento organizado pela administração do clube. O histórico fica preservado mesmo após seu encerramento ou cancelamento.</p></div>${!e.cancelledAt && e.end > now() ? action("Cancelar evento", "cancel-event", e.id, "danger", "close") : ""}</aside></div>`;
  }
  function pollForm() {
    const uid = D.actor.unitId || operatingUnits()[0]?.id || "",
      day = V.today(now(), timezone(uid));
    return `${crumb("Votações", "votacoes")}${header("Vamos decidir juntos", "Cadastre uma votação de escolha única para os titulares da unidade.")}<form data-form="poll">${formError()}<div class="form-layout"><div>${formSection("01", "A decisão", field("Motivo da votação", "title", "", "text", 'required maxlength="200"') + unitSelect(uid) + area("Descrição detalhada", "description", "", 'required maxlength="4000"') + area("Opções de voto", "options", "Opção 1\nOpção 2", 'required maxlength="2000"', "Uma opção por linha. Informe de 2 a 8 alternativas diferentes.") + area("Regras da votação", "rules", "Escolha única. Apenas titulares ativos da unidade de origem podem votar. O voto confirmado não pode ser alterado.", 'required maxlength="2000"'))}${formSection(
      "02",
      "Período e critérios",
      field(
        "Data e horário de início",
        "start",
        V.localInput(now(), timezone(uid)),
        "datetime-local",
        "required",
      ) +
        field(
          "Data e horário de término",
          "end",
          V.addDays(day, 7) + "T20:00",
          "datetime-local",
          "required",
        ) +
        select(
          "Status inicial",
          "initialStatus",
          [
            ["Aberta", "Aberta conforme o período"],
            ["Encerrada", "Encerrada (sem novos votos)"],
          ],
          "Aberta",
          "",
          "Com início futuro, ficará Agendada até a abertura.",
        ) +
        field(
          "Quórum mínimo (%)",
          "quorum",
          0,
          "number",
          'required min="0" max="100"',
          "0 significa sem quórum mínimo.",
        ) +
        `<div class="field full">Elegibilidade${check("Exigir mensalidades em dia", "requirePaid", false)}</div>`,
    )}${formActions("votacoes", "Criar votação")}</div><aside class="note">${icon("vote")}<h3>Cada voz tem seu lugar.</h3><p>Convidados e dependentes não votam. Um titular participa uma única vez, na unidade de origem do contrato.</p><p>Depois de criada, as opções ficam preservadas. É possível encerrar a votação, mas não reabri-la ou alterar os votos.</p></aside></div></form>`;
  }
  function pollDetail(p) {
    const votes = D.votes.filter((v) => v.pollId === p.id),
      electorate = p.electorate?.length || 0,
      required = Math.ceil((electorate * p.quorum) / 100),
      status = V.pollStatus(p, now()),
      met = votes.length >= required;
    return `${crumb("Votações", "votacoes")}${header(h(p.title), h(unitName(p.unitId)), tag(status, status === "Aberta" ? "green" : status === "Agendada" ? "amber" : "gray"))}<div class="detail-grid"><section class="stack"><div class="card card-pad"><h2>Sobre esta decisão</h2><p class="preline small muted">${h(p.description)}</p>${kv(
      [
        ["Abertura", when(p.start, p.unitId)],
        ["Encerramento previsto", when(p.end, p.unitId)],
        [
          "Encerramento antecipado",
          p.closedAt ? when(p.closedAt, p.unitId) : "Não",
        ],
        ["Adimplência exigida", p.requirePaid ? "Sim" : "Não"],
        ["Regras", p.rules, true],
      ],
    )}</div><div class="card card-pad"><div class="section-head"><h2>${status === "Encerrada" ? "Apuração final" : "Apuração parcial"}</h2>${tag(votes.length + " votos", "teal")}</div>${p.options
      .map((o) => {
        const n = votes.filter((v) => v.optionId === o.id).length,
          percent = votes.length ? (n / votes.length) * 100 : 0;
        return `<div class="option-result"><div class="between"><strong>${h(o.text)}</strong><span class="small muted">${n} · ${percent.toFixed(1).replace(".", ",")}%</span></div><div class="bar"><span style="width:${percent}%"></span></div></div>`;
      })
      .join(
        "",
      )}${status === "Encerrada" ? `<div class="notice ${met ? "" : "warn"}">${icon(met ? "check" : "info")}${met ? "Resultado: " + h(pollResult(p)) : "Quórum não atingido. Resultado sem validade conforme a regra cadastrada."}</div>` : '<p class="tiny muted mb0">Contagens recebidas do serviço. Use Atualizar para consultar a apuração mais recente.</p>'}</div></section><aside class="stack"><div class="grid2"><div class="card number-card"><div class="big-number">${votes.length}</div><div class="tiny muted">votos registrados</div></div><div class="card number-card"><div class="big-number">${status === "Agendada" ? "—" : electorate}</div><div class="tiny muted">titulares elegíveis</div></div></div><div class="note"><h3>Participação da unidade</h3><p>Quórum mínimo: ${p.quorum}%. ${p.quorum ? required + " voto(s) necessário(s)." : "Não há mínimo obrigatório."}</p><p>A apuração apresentada é fornecida pelo serviço da rede. As regras de elegibilidade são definidas na abertura da votação.</p></div>${status !== "Encerrada" ? action("Encerrar votação", "close-poll", p.id, "danger", "lock") : ""}</aside></div>`;
  }
  function memberDetail(p) {
    const c = contract(p),
      owner = D.people.find((x) => x.id === c.holderId),
      guests = D.people.filter(
        (x) => x.contractId === c.id && x.type !== "Titular",
      ),
      bills = D.invoices
        .filter((i) => i.contractId === c.id)
        .sort((a, b) => a.number - b.number),
      late = overdue(c),
      status = contractStatus(c);
    return `${crumb("Sócios", "socios")}${header(h(p.name), h(p.type + " · " + unitName(c.unitId)), tag(p.type === "Titular" ? status : p.approval, p.approval === "Pendente" ? "amber" : status === "Suspenso" ? "red" : "green"))}<div class="detail-grid"><section class="stack"><div class="card"><div class="profile-summary">${avatar(p.name, "big")}<div><h2>${h(p.name)}</h2><p class="small muted mb0">${h(p.type)}${p.type !== "Titular" ? " · Responsável: " + h(owner.name) : " / fundador"} · ${h(c.number)}</p></div></div><div class="card-pad">${kv(
      [
        ["CPF", p.cpf],
        ["Nascimento", formatDate(p.birth)],
        ["Telefone", p.phone],
        ["E-mail", p.email],
        ["Endereço", addressText(p), true],
        ["Estado civil", p.maritalStatus || "Não informado"],
        ["Gênero", p.gender || "Não informado"],
        ...(p.type !== "Titular"
          ? [
              ["Vínculo com o titular", p.relationship],
              ["Solicitado em", when(p.createdAt, c.unitId)],
            ]
          : []),
      ],
    )}</div></div>${p.type !== "Titular" ? `<div class="card card-pad ${p.approval === "Pendente" ? "alert-card" : ""}"><h2>${p.approval === "Pendente" ? "Cadastro aguardando aprovação" : "Análise do cadastro"}</h2><p class="small muted">Confira os dados do convidado e seu vínculo com o titular antes de decidir.</p>${p.approval === "Pendente" ? `<div class="flex wrap">${action("Aceitar cadastro", "approve-guest", p.id, "", "check")}${action("Recusar cadastro", "reject-guest", p.id, "danger", "close")}</div>` : `<div class="notice">${icon("info")}${h(p.reviewReason || "Cadastro já analisado pela administração.")}</div>`}<div class="mt">${link("Ver cadastro do titular", "socios/" + owner.id)}</div></div>` : ""}<div class="card card-pad"><div class="section-head"><h2>Convidados e dependentes</h2>${tag(guests.filter((x) => ["Pendente", "Aprovado"].includes(x.approval)).length + " / " + c.snapshot.quota + " vagas", "gray")}</div><p class="small muted">Pessoas vinculadas ao contrato de ${h(owner.name)}.</p>${guests.length ? guests.map((g) => `<div class="beneficiary ${g.approval === "Pendente" ? "flagged" : ""}">${avatar(g.name, "teal")}<div class="grow"><div class="flex gap8"><span class="name">${h(g.name)}</span>${g.approval === "Pendente" ? icon("alert", "alert-icon") : ""}</div><p>${h(g.type)} · ${h(g.relationship)}</p>${tag(g.approval, g.approval === "Aprovado" ? "green" : g.approval === "Recusado" ? "red" : "amber")}</div>${link(g.approval === "Pendente" ? "Analisar" : "Ver dados", "socios/" + g.id, "btn secondary sm", "")}</div>`).join("") : '<p class="small muted">Nenhum beneficiário cadastrado neste contrato.</p>'}</div></section><aside class="stack"><div class="card card-pad"><div class="eyebrow">Contrato original · versão ${c.snapshot.number}</div><h2>${h(c.snapshot.name)}</h2><p class="small muted">${c.snapshot.scope === "REDE" ? "Acesso a toda a rede" : "Acesso à unidade de origem"}</p>${kv(
      [
        ["Início", formatDate(c.start)],
        ["Vigência até", formatDate(V.addDays(c.end, -1))],
        ["Total contratado", V.money(c.condition.totalCents)],
        ["Condição", c.condition.name],
        ["Convidados + dependentes", "Até " + c.snapshot.quota],
        ["Unidade de origem", unitName(c.unitId)],
      ],
    )}<div class="notice">${icon("shield")}Valores e benefícios preservados, mesmo se a oferta atual mudar ou for excluída.</div><details><summary class="small text-teal strong">Benefícios e regras do contrato</summary><p class="small muted preline mt">${h(c.snapshot.benefits)}\n\n${h(c.snapshot.terms)}</p></details></div><div class="card card-pad ${late.length ? "alert-card" : ""}"><h2>Situação da associação</h2>${tag(status, status === "Suspenso" ? "red" : status === "Encerrado" ? "gray" : "green")}<p class="small muted mt">${late.length ? late.length + " parcela(s) em atraso, totalizando " + V.money(late.reduce((s, i) => s + i.amount, 0)) + "." : "Nenhuma parcela atrasada neste contrato."}</p>${status === "Ativo" && late.length ? action("Desligar temporariamente", "suspend", c.id, "danger", "lock") : status === "Suspenso" && !late.length ? action("Reativar associação", "reactivate", c.id, "", "check") : ""}${status === "Suspenso" ? '<p class="tiny muted mt">' + h(c.reason || "Suspenso por inadimplência") + ". A reativação depende da regularização das parcelas e da confirmação da administração.</p>" : ""}</div></aside></div><section class="mt"><div class="section-head"><div><h2>Mensalidades e pagamentos</h2><p class="small muted mb0">Contrato de ${h(owner.name)} · ${bills.filter((i) => i.paidAt).length} de ${bills.length} parcelas pagas.</p></div>${isGeneral() && bills.some(i => !i.paidAt) ? action("Registrar recebimento", "record-payment", owner.id, "secondary", "wallet") : ""}</div><div class="table-wrap"><table class="table"><thead><tr><th>Parcela</th><th>Vencimento</th><th>Valor</th><th>Status</th><th>Pagamento / origem</th></tr></thead><tbody>${bills
      .map((i) => {
        const status = V.invoiceStatus(i, V.today(now(), timezone(c.unitId)));
        return `<tr><td><strong>${String(i.number).padStart(2, "0")}/${String(i.total).padStart(2, "0")}</strong></td><td>${formatDate(i.due)}</td><td>${V.money(i.amount)}</td><td>${tag(status, status === "Paga" ? "green" : status === "Atrasada" ? "red" : "amber")}</td><td>${i.paidAt ? when(i.paidAt, c.unitId) + '<div class="tiny muted">' + h(i.origin === "SIMULADO" ? "Simulado no Mobile · sem cobrança real" : i.origin) + "</div>" : '<span class="muted">Aguardando pagamento</span>'}</td></tr>`;
      })
      .join(
        "",
      )}</tbody></table></div><p class="tiny muted mt">Os pagamentos são consultados no serviço da rede. Pagamentos simulados no Mobile aparecem como pagos, identificados pela origem. Recebimentos reais continuam sendo conferidos pelo Presidente. Esta tela não realiza cobranças bancárias.</p></section>`;
  }
  function ticketDetail(t) {
    const p = D.people.find((p) => p.id === t.personId);
    return `${crumb(t.type === "Solicitação" ? "Solicitações" : "Reclamações", section)}${header(h(t.title), h(t.number) + " · " + h(unitName(t.unitId)), tag(t.responses.length ? "Respondida" : "Aguardando resposta", t.responses.length ? "green" : "amber"))}<div class="detail-grid"><section class="card card-pad"><h2>Histórico do atendimento</h2><div class="ticket-message"><div class="meta"><strong>${h(p.name)}</strong> · ${when(t.createdAt, t.unitId)}</div><p class="message-text">${h(t.description)}</p></div>${t.responses.map((r) => `<div class="ticket-message admin"><div class="meta"><strong>${h(r.authorName)} · ${role(r.role)}</strong> · ${when(r.at, t.unitId)}</div><p class="message-text">${h(r.text)}</p></div>`).join("")}<form data-form="reply">${hidden("id", t.id)}${hidden("version", t.version)}${formError()}${area(t.responses.length ? "Complementar resposta" : "Sua resposta", "text", "", 'required maxlength="4000" placeholder="Escreva um retorno para o associado…"')}<div class="dialog-actions"><button class="btn" type="submit">${icon("send")}Enviar resposta</button></div></form></section><aside class="stack"><div class="card card-pad"><div class="flex mb0">${avatar(p.name)}<div><h3>${h(p.name)}</h3><span class="tiny muted">${h(p.type)}</span></div></div><div class="mt">${kv([["Protocolo", t.number], ["Tipo", t.type], ["Recebido em", when(t.createdAt, t.unitId), true], ["Unidade", unitName(t.unitId), true], ...(t.type === "Solicitação" ? [["Status", t.status]] : [])])}</div><div class="mt">${link("Ver sócio", "socios/" + p.id)}</div></div><div class="note">${icon("chat")}<h3>Um retorno faz a diferença.</h3><p>A resposta fica no histórico. O aplicativo do associado consulta esse mesmo atendimento.</p></div></aside></div>`;
  }
  function planForm(p) {
    if (!isGeneral()) return forbidden();
    const v = p
      ? currentVersion(p)
      : {
          name: "",
          description: "",
          scope: "LOCAL",
          quota: 2,
          dependentLimit: 2,
          guestLimit: 2,
          durationMonths: 12,
          graceDays: 0,
          futureReservationsLimit: 5,
          benefits: "Acesso às áreas do clube\nReservas pelo aplicativo",
          terms:
            "Associação anual. Beneficiários sujeitos à aprovação. Respeitar as regras da unidade.",
          conditions: [
            { totalCents: 118800, installments: 12 },
            { totalCents: 106920, installments: 1 },
          ],
        };
    return `${crumb("Planos", "planos")}${header(p ? "Uma nova versão do plano" : "Novas formas de fazer parte", p ? "As mudanças valerão apenas para futuras contratações." : "Defina a oferta que ficará disponível em toda a rede.")}<form data-form="plan">${formError()}${hidden("id", p?.id || "")}${hidden("version", p?.version || "")}<div class="form-layout"><div>${formSection(
      "01",
      "O plano",
      field("Nome", "name", v.name, "text", 'required maxlength="150"') +
        field(
          "Código",
          "code",
          p?.code || "",
          "text",
          'required maxlength="40"',
        ) +
        area(
          "Descrição",
          "description",
          v.description,
          'required maxlength="2000"',
        ) +
        select(
          "Abrangência de acesso",
          "scope",
          [
            ["LOCAL", "Uma unidade de origem"],
            ["REDE", "Todas as unidades da rede"],
          ],
          v.scope,
        ) +
        field(
          "Duração do contrato (meses)",
          "durationMonths",
          v.durationMonths,
          "number",
          'required min="1" max="60"',
        ),
    )}${formSection("02", "Pessoas e benefícios", field("Cota total compartilhada", "quota", v.quota, "number", 'required min="0" max="50"', "Convidados e dependentes somados.") + field("Máximo de dependentes", "dependentLimit", v.dependentLimit, "number", 'required min="0" max="50"') + field("Máximo de convidados", "guestLimit", v.guestLimit, "number", 'required min="0" max="50"') + field("Limite de reservas futuras", "futureReservationsLimit", v.futureReservationsLimit, "number", 'required min="0" max="100"', "0 significa que não permite novas reservas.") + field("Tolerância de atraso (dias)", "graceDays", v.graceDays, "number", 'required min="0" max="90"') + area("Benefícios", "benefits", v.benefits, 'required maxlength="4000"', "Um benefício por linha.") + area("Regras de contratação e termos", "terms", v.terms, 'required maxlength="4000"'))}${formSection("03", "Condições de pagamento", field("Valor de cada parcela (R$)", "installmentValue", (v.conditions[0].totalCents / v.conditions[0].installments / 100).toFixed(2), "text", 'required inputmode="decimal"', "Sem separador de milhar. Ex.: 149,90.") + field("Quantidade de parcelas mensais", "installments", v.conditions[0].installments, "number", 'required min="1" max="36"') + field("Valor total à vista (R$)", "cashValue", (v.conditions[1].totalCents / 100).toFixed(2), "text", 'required inputmode="decimal"') + `<div class="notice full">${icon("info")}Estas condições compõem a oferta do plano. A criação da oferta não gera cobranças.</div>`)}${formActions("planos", p ? "Publicar nova versão" : "Cadastrar plano")}</div><aside class="stack"><div class="note">${icon("shield")}<h3>O combinado continua valendo.</h3><p>Cada contrato guarda a versão contratada. Preços, cotas e benefícios de sócios existentes não são alterados.</p><p>Excluir um plano encerra apenas sua oferta para novas adesões. Os contratos atuais seguem até o término.</p></div><div class="note teal"><h3>Uma oferta para toda a rede</h3><p>Mesmo um plano de acesso local pode ser contratado em qualquer unidade. O associado escolhe sua unidade de origem.</p></div></aside></div></form>`;
  }
  function planDetail(p) {
    const v = currentVersion(p),
      contracts = D.contracts.filter((c) => c.planId === p.id);
    return `${crumb("Planos", "planos")}${header(h(v.name), h(p.code) + " · " + (p.active ? "Disponível para novas contratações" : "Excluído da oferta"), isGeneral() && p.active ? link("Editar plano", "planos/" + p.id + "/editar", "btn", "edit") : "")}<div class="detail-grid"><section class="stack"><div class="card card-pad"><div class="section-head"><h2>Oferta atual · versão ${v.number}</h2>${tag(v.scope === "REDE" ? "Toda a rede" : "Unidade de origem", "teal")}</div><p class="preline small muted">${h(v.description)}</p>${kv(
      [
        ["Duração", v.durationMonths + " meses"],
        ["Cota total", v.quota + " beneficiários"],
        ["Limite de dependentes", v.dependentLimit],
        ["Limite de convidados", v.guestLimit],
        ["Reservas futuras", v.futureReservationsLimit],
        ["Tolerância cadastrada", v.graceDays + " dias"],
      ],
    )}<h3 class="mt">Benefícios</h3><p class="preline small muted">${h(v.benefits)}</p><h3>Regras e termos</h3><p class="preline small muted">${h(v.terms)}</p>${v.conditions.map((c) => `<div class="beneficiary"><div class="grow"><div class="name">${h(c.name)}</div><p>${h(c.method)} · ${c.installments} parcela(s)</p></div><strong>${V.money(c.totalCents)}</strong></div>`).join("")}</div><div class="card card-pad"><h2>Histórico de versões</h2>${[
      ...p.versions,
    ]
      .reverse()
      .map(
        (x) =>
          `<div class="version-row"><div class="between"><h3>Versão ${x.number} · ${h(x.name)}</h3>${tag(contracts.filter((c) => c.planVersionId === x.id).length + " contratos", "gray")}</div><p class="small muted mb0">Publicada em ${when(x.publishedAt)}<br>${x.quota} beneficiários · ${x.scope === "REDE" ? "Rede completa" : "Unidade local"} · ${x.conditions[0].installments} × ${V.money(x.conditions[0].totalCents / x.conditions[0].installments)}<br>À vista: ${V.money(x.conditions[1].totalCents)}</p><details class="mt"><summary class="small text-teal">Ver benefícios desta versão</summary><p class="small muted preline mt">${h(x.benefits)}\n\n${h(x.terms)}</p></details></div>`,
      )
      .join(
        "",
      )}</div></section><aside class="stack"><div class="card number-card"><div class="big-number">${contracts.length}</div><p class="small muted mb0">contratos vinculados no seu escopo</p></div><div class="note">${icon("layers")}<h3>Histórico preservado.</h3><p>Um contrato mantém sua própria versão, incluindo preço e benefícios. A oferta atual é usada somente por novas adesões.</p></div>${isGeneral() && p.active ? action("Excluir da oferta", "archive-plan", p.id, "danger", "trash") : ""}</aside></div>`;
  }
  function chatPage() {
    const units = operatingUnits().filter((u) => u.active);
    if (!units.some((u) => u.id === chatUnit)) chatUnit = units[0]?.id || "";
    if (!chatUnit)
      return empty(
        "Nenhuma unidade ativa",
        "Ative uma unidade para utilizar o chat.",
        "chat",
      );
    const messages = D.messages
        .filter((m) => m.unitId === chatUnit)
        .sort((a, b) => a.at.localeCompare(b.at)),
      pins = messages.filter((m) => m.pinned);
    return `${crumb("Chat")}${header("Conversas que conectam", "Um espaço para avisos, novidades e a vida em comunidade.")}
<section class="card chat-layout"><aside class="chat-sidebar"><h3>Grupos das unidades</h3>${units.map((u) => `<button class="channel ${u.id === chatUnit ? "active" : ""}" data-action="channel" data-id="${u.id}"><span class="avatar">${h(u.acronym)}</span><div><strong>${h(u.name.replace("Clube ", ""))}</strong><span>Chat geral da unidade</span></div></button>`).join("")}</aside><div class="chat-main"><div class="chat-head"><div class="between"><div><h2>${h(unitName(chatUnit))}</h2><p>Grupo geral · Associados e administração</p></div>${tag("Chat da unidade", "teal")}</div></div>${pins.length ? `<div class="pinned">${icon("pin")}<span><strong>Aviso fixado</strong><br>${h(pins.at(-1).text)}</span></div>` : ""}<div class="chat-scroll" id="chat-scroll" aria-label="Histórico do chat"><div class="chat-day">Conversa da unidade</div>${messages.map((m) => `<div class="message ${m.authorId === D.actor.id ? "mine" : ""}">${avatar(m.authorName)}<div class="message-bubble"><div class="message-author">${h(m.authorName)} · ${h(role(m.role))}</div><div class="message-text">${h(m.text)}</div><div class="message-time">${formatDate(m.at, chatUnit)} · ${formatTime(m.at, chatUnit)}${m.role.startsWith("ADM") ? `<button data-action="pin" data-id="${m.id}" aria-label="${m.pinned ? "Desafixar" : "Fixar"} aviso" title="${m.pinned ? "Desafixar" : "Fixar"} aviso">${icon("pin")}</button>` : ""}</div></div></div>`).join("")}</div><form class="chat-compose" data-form="message"><textarea name="text" id="message-input" rows="1" maxlength="2000" required aria-label="Escrever mensagem" placeholder="Escreva uma mensagem para o grupo…">${h(chatDraft)}</textarea><button class="btn" type="submit">${icon("send")}<span class="send-word">Enviar</span></button></form><div class="typing-hint">Enter para enviar · Shift + Enter para uma nova linha · Atualização automática enquanto a conversa estiver aberta</div></div></section>`;
  }
  function forbidden() {
    return `${header("Acesso restrito", "Esta ação é exclusiva do Presidente.")}<div class="note">${icon("shield")}Você pode consultar o cadastro, mas não alterar a configuração da rede.</div><div class="mt">${link("Voltar ao início", "inicio", "btn", "left")}</div>`;
  }
  function content() {
    const id = route[1],
      edit = route[2] === "editar";
    if (section === "inicio") return homePage();
    if (section === "unidades") {
      if (id === "novo") return unitForm();
      if (id) {
        const u = V.find(D, "units", id);
        return edit ? unitForm(u) : unitDetail(u);
      }
      return unitsPage();
    }
    if (section === "administradores") {
      if (id === "novo") return isGeneral() ? adminForm() : forbidden();
      if (id) return adminForm(V.find(D, "admins", id));
      return adminsPage();
    }
    if (section === "eventos") {
      if (id === "novo") return eventForm();
      if (id) {
        const e = V.find(D, "events", id);
        return edit ? eventForm(e) : eventDetail(e);
      }
      return eventsPage();
    }
    if (section === "votacoes") {
      if (id === "nova") return pollForm();
      if (id) return pollDetail(V.find(D, "polls", id));
      return pollsPage();
    }
    if (section === "socios") {
      if (id) return memberDetail(V.find(D, "people", id));
      return membersPage();
    }
    if (section === "solicitacoes" || section === "reclamacoes") {
      if (id) {
        const t = V.find(D, "tickets", id);
        if (
          t.type !== (section === "solicitacoes" ? "Solicitação" : "Reclamação")
        )
          throw Error("O protocolo pertence a outro tipo de atendimento.");
        return ticketDetail(t);
      }
      return ticketsPage(
        section === "solicitacoes" ? "Solicitação" : "Reclamação",
      );
    }
    if (section === "planos") {
      if (id === "novo") return planForm();
      if (id) {
        const p = V.find(D, "plans", id);
        return edit ? planForm(p) : planDetail(p);
      }
      return plansPage();
    }
    if (section === "chat") return chatPage();
    return `${header("Página não encontrada", "Escolha uma opção no menu para continuar.")}${link("Voltar ao início", "inicio", "btn", "left")}`;
  }
  function render(keep = false) {
    const scrollY = window.scrollY,
      focus = document.activeElement?.id,
      selection = document.activeElement?.selectionStart;
    let chatBottom = true;
    const oldScroll = $("#chat-scroll");
    if (oldScroll)
      chatBottom =
        oldScroll.scrollHeight - oldScroll.scrollTop - oldScroll.clientHeight <
        100;
    try {
      if (!session) { loginPage(); return; }
      D = svc.view(session.id);
    } catch (err) {
      session = null;
      loginPage();
      toast(err.message, true);
      return;
    }
    route = (location.hash.slice(1) || "inicio").split("/");
    section = route[0];
    try {
      app.innerHTML = shell(content());
    } catch (err) {
      app.innerHTML = shell(
        `${header("Não foi possível abrir este registro", h(err.message))}${link("Voltar ao início", "inicio", "btn", "left")}`,
      );
    }
    if (keep) {
      window.scrollTo(0, scrollY);
      if (focus) {
        const el = document.getElementById(focus);
        el?.focus({ preventScroll: true });
        if (
          typeof selection === "number" &&
          el?.setSelectionRange &&
          ["text", "search", "tel", "password", "email"].includes(el.type)
        )
          try {
            el.setSelectionRange(selection, selection);
          } catch {}
      }
    } else window.scrollTo(0, 0);
    const cs = $("#chat-scroll");
    if (cs && chatBottom) cs.scrollTop = cs.scrollHeight;
    if (flashAfterNav) {
      toast(flashAfterNav);
      flashAfterNav = "";
    }
  }
  function go(hash, message = "") {
    if (message) flashAfterNav = message;
    if (location.hash === "#" + hash) {
      render();
    } else location.hash = hash;
  }
  const drafts = {};
  document.addEventListener("click", async (e) => {
    const b = e.target.closest("[data-action]");
    if (!b || b.disabled) return;
    const key = b.dataset.action,
      id = b.dataset.id;
    try {
      if (key === "close-dialog") {
        dialog.close();
        return;
      }
      if (key === "show-password") {
        const input = $("#f-password");
        input.type = input.type === "password" ? "text" : "password";
        b.setAttribute(
          "aria-label",
          input.type === "password" ? "Mostrar senha" : "Ocultar senha",
        );
        return;
      }
      if (key === "refresh") {
        b.disabled = true;
        try { await svc.refresh(); render(true); toast("Dados atualizados."); }
        finally { b.disabled = false; }
        return;
      }
      if (key === "logout") {
        confirmAction("Sair do portal?", "Sua sessão administrativa será encerrada.", "Sair da conta", async () => {
          await svc.logout(); session = null; chatDraft = ""; D = null;
          Object.keys(drafts).forEach(k => delete drafts[k]);
          location.hash = "inicio";
        });
        return;
      }
      if (key === "clear") {
        filters[section] = {
          q: "",
          units: [],
          flags: [],
          type: "",
          tab: "Todas",
          page: 1,
        };
        render(true);
        return;
      }
      if (key === "page-prev" || key === "page-next") {
        getFilters().page = Math.max(
          1,
          getFilters().page + (key === "page-next" ? 1 : -1),
        );
        render(true);
        return;
      }
      if (key === "channel") {
        drafts[chatUnit] = chatDraft;
        chatUnit = id;
        chatDraft = drafts[id] || "";
        render(true);
        return;
      }
      if (key === "pin") {
        await svc.pin(session.id, id);
        render(true);
        toast("Aviso atualizado.");
        return;
      }
      if (key === "notifications") {
        const n = D.people.filter((p) => p.approval === "Pendente").length,
          t = D.tickets.filter((t) => !t.responses.length).length;
        modal(
          "Sua atenção faz a diferença",
          `<p>Acompanhe os itens que aguardam uma ação da administração.</p><div class="stack">${link(n + " convidados para analisar", "socios", "btn secondary", "users")}${link(t + " contatos aguardando resposta", "solicitacoes", "btn secondary", "file")}${link("Consultar reclamações", "reclamacoes", "btn secondary", "chat")}</div>`,
        );
        return;
      }
      if (key === "about") {
        modal("Sobre o VanderClub Web", `<p>Portal de administração da rede, com acesso de Presidente e ADM Local.</p><p>A gestão de unidades, pessoas, eventos e atendimentos utiliza o serviço VanderClub. Alterações nos planos preservam os contratos existentes.</p><p>Versão 2.0 · HTML, CSS e JavaScript.</p>`);
        return;
      }
      if (key === "record-payment") {
        if (!isGeneral()) throw Error("Esta ação é exclusiva do ADM Geral.");
        const person = V.find(D, "people", id), c = contract(person);
        const invoices = D.invoices.filter(i => i.contractId === c.id && !i.paidAt);
        if (!invoices.length) return;
        modal("Registrar recebimento", `<form data-form="payment">${formError()}<p>Registre somente um pagamento já recebido e conferido. Esta ação atualiza o histórico financeiro e não cobra o associado.</p>${select("Parcela recebida", "invoiceId", invoices.map(i => [i.id, "Parcela " + i.number + " · " + V.money(i.amount) + " · " + i.due]), invoices[0].id)}${field("Data e horário do recebimento", "receivedAt", V.localInput(now(), timezone(c.unitId)), "datetime-local", "required")}${select("Meio informado", "method", [["PIX","Pix"],["BOLETO","Boleto"],["CARTAO","Cartão"],["OUTRO","Outro"]], "PIX")}${field("Referência do comprovante", "reference", "", "text", 'required maxlength="200"')}${area("Observação", "note", "", 'maxlength="1000"')}<div class="dialog-actions"><button type="submit" class="btn">Confirmar recebimento conferido</button></div></form>`);
        return;
      }
      if (key === "approve-guest" || key === "reject-guest") {
        const p = V.find(D, "people", id),
          approve = key === "approve-guest";
        confirmAction(
          approve ? "Aprovar cadastro?" : "Recusar cadastro?",
          (approve
            ? "Confirma a conferência dos dados de "
            : "Informe o motivo da recusa de ") +
            p.name +
            ".",
          approve ? "Aceitar cadastro" : "Recusar cadastro",
          async (fd) => {
            await svc.reviewGuest(
              session.id,
              id,
              approve ? "Aprovado" : "Recusado",
              fd.get("reason") || "",
              p.version,
            );
            toast(
              approve
                ? "Cadastro aprovado."
                : "Recusa registrada com o motivo.",
            );
          },
          !approve,
          !approve,
        );
        return;
      }
      if (key === "suspend") {
        const c = V.find(D, "contracts", id);
        confirmAction(
          "Desligar temporariamente?",
          "O titular e os beneficiários ficarão suspensos por inadimplência. O contrato e o histórico de pagamentos serão preservados.",
          "Confirmar suspensão",
          async (fd) => {
            await svc.suspend(session.id, c.id, fd.get("reason"));
            toast("Associação suspensa temporariamente.");
          },
          true,
          true,
        );
        return;
      }
      if (key === "reactivate") {
        await svc.reactivate(session.id, id);
        render(true);
        toast("Associação reativada.");
        return;
      }
      if (key === "cancel-event") {
        confirmAction(
          "Cancelar evento?",
          "O evento permanecerá no histórico e liberará sua área na agenda.",
          "Cancelar evento",
          async (fd) => {
            await svc.cancelEvent(session.id, id, fd.get("reason"));
            toast("Evento cancelado.");
          },
          true,
          true,
        );
        return;
      }
      if (key === "close-poll") {
        confirmAction(
          "Encerrar votação?",
          "Novos votos deixarão de ser aceitos. O resultado e o histórico serão preservados, sem possibilidade de reabertura.",
          "Encerrar votação",
          async () => {
            await svc.closePoll(session.id, id);
            toast("Votação encerrada.");
          },
          true,
        );
        return;
      }
      if (key === "archive-plan") {
        const p = V.find(D, "plans", id);
        confirmAction(
          "Excluir plano da oferta?",
          "O plano não será oferecido em novas adesões. Quem já contratou continuará com os mesmos valores e benefícios até o fim do contrato.",
          "Excluir da oferta",
          async () => {
            await svc.archivePlan(session.id, id, p.version);
            toast("Oferta excluída. Contratos anteriores preservados.");
          },
          true,
        );
        return;
      }
    } catch (err) {
      toast(err.message, true);
    }
  });
  document.addEventListener("click", (e) => {
    const tab = e.target.closest("[data-tab]");
    if (tab) {
      getFilters().tab = tab.dataset.tab;
      getFilters().page = 1;
      render(true);
    }
    if (e.target.closest('a[href^="#"]') && dialog.open) dialog.close();
  });
  document.addEventListener("input", (e) => {
    if (e.target.matches('[data-filter="q"]')) {
      getFilters().q = e.target.value;
      getFilters().page = 1;
      render(true);
    }
    if (e.target.id === "message-input") {
      chatDraft = e.target.value;
      drafts[chatUnit] = chatDraft;
      e.target.style.height = "auto";
      e.target.style.height = Math.min(e.target.scrollHeight, 120) + "px";
    }
  });
  document.addEventListener("change", (e) => {
    const t = e.target;
    if (t.matches("[data-unit-filter]")) {
      const f = getFilters(),
        id = t.dataset.unitFilter;
      f.units = t.checked ? [...f.units, id] : f.units.filter((x) => x !== id);
      f.page = 1;
      render(true);
    } else if (t.matches("[data-flag-filter]")) {
      const f = getFilters(),
        id = t.dataset.flagFilter;
      f.flags = t.checked ? [...f.flags, id] : f.flags.filter((x) => x !== id);
      f.page = 1;
      render(true);
    } else if (t.matches("[data-filter]")) {
      getFilters()[t.dataset.filter] = t.value;
      getFilters().page = 1;
      render(true);
    } else if (t.matches("[data-sim-person]"))
      simForm(t.dataset.simPerson, t.value);
    else if (t.matches("[data-sim-poll]")) {
      const poll = D.polls.find((p) => p.id === t.value),
        opt = dialog.querySelector('[name="optionId"]');
      opt.innerHTML = (poll?.options || [])
        .map((o) => `<option value="${o.id}">${h(o.text)}</option>`)
        .join("");
    } else if (t.name === "unitId" && t.closest('[data-form="event"]')) {
      const selectArea = t.closest("form").querySelector('[name="areaId"]');
      selectArea.innerHTML =
        '<option value="">Local externo / sem ocupar área</option>' +
        D.areas
          .filter((a) => a.unitId === t.value)
          .map((a) => `<option value="${a.id}">${h(a.name)}</option>`)
          .join("");
    }
  });
  document.addEventListener("keydown", (e) => {
    if (e.target.id === "message-input" && e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      e.target.closest("form").requestSubmit();
    }
  });
  let messageKey = V.uid("client");
  document.addEventListener("submit", async (e) => {
    const form = e.target.closest("form[data-form]");
    if (!form) return;
    e.preventDefault();
    if (form.dataset.busy) return;
    form.dataset.busy = "true";
    const button = form.querySelector('[type="submit"]'),
      original = button?.innerHTML;
    if (button) {
      button.disabled = true;
      button.textContent = "Salvando…";
    }
    const error = form.querySelector(".form-error");
    if (error) error.textContent = "";
    const fd = new FormData(form),
      p = Object.fromEntries(fd.entries());
    try {
      const kind = form.dataset.form;
      if (kind === "login") {
        const a = await svc.login(p.login, p.password);
        session = { id: a.id };
        location.hash = "inicio";
        render();
        toast("Bem-vindo(a), " + a.name.split(" ")[0] + ".");
      }
      if (kind === "unit") {
        p.active = fd.has("active");
        await svc.saveUnit(session.id, p);
        go("unidades", "Unidade salva com sucesso.");
      }
      if (kind === "admin") {
        p.active = fd.has("active");
        if (p.confirmPassword && !p.password)
          throw Error("Preencha a nova senha e sua confirmação.");
        await svc.saveAdmin(session.id, p);
        go("administradores", "Administrador salvo com sucesso.");
      }
      if (kind === "event") {
        await svc.saveEvent(session.id, p);
        go("eventos", "Evento salvo na programação.");
      }
      if (kind === "poll") {
        p.options = p.options
          .split("\n")
          .map((x) => x.trim())
          .filter(Boolean);
        p.requirePaid = fd.has("requirePaid");
        p.closed = p.initialStatus === "Encerrada";
        const poll = await svc.savePoll(session.id, p);
        go("votacoes/" + poll.id, "Votação criada.");
      }
      if (kind === "payment") {
        await svc.recordPayment(session.id, p);
        dialog.close(); render(true); toast("Recebimento registrado pela administração.");
      }
      if (kind === "reply") {
        await svc.reply(session.id, p.id, p.text, p.version);
        render(true);
        toast("Resposta registrada no histórico.");
      }
      if (kind === "plan") {
        await svc.savePlan(session.id, p);
        go(
          "planos",
          "Oferta publicada. Contratos anteriores permanecem iguais.",
        );
      }
      if (kind === "message") {
        await svc.send(session.id, chatUnit, p.text, messageKey);
        messageKey = V.uid("client");
        chatDraft = "";
        drafts[chatUnit] = "";
        render(true);
        const box = $("#chat-scroll");
        if (box) box.scrollTop = box.scrollHeight;
        $("#message-input")?.focus({ preventScroll: true });
      }
      if (kind === "confirm") {
        if (modalAction) await modalAction(fd);
        dialog.close();
        render(true);
      }
    } catch (err) {
      if (error?.isConnected) error.textContent = err.message;
      else toast(err.message, true);
    } finally {
      delete form.dataset.busy;
      if (button) {
        button.disabled = false;
        button.innerHTML = original;
      }
    }
  });
  window.addEventListener("hashchange", () => {
    dialog.close();
    render();
  });
  let refreshing = false, refreshFailed = false;
  setInterval(async () => {
    if (!session || document.hidden || dialog.open || refreshing || document.querySelector("form[data-busy]")) return;
    if ($("#main form") && section !== "chat") return;
    refreshing = true;
    try { await svc.refresh(); refreshFailed = false; render(true); }
    catch (err) { if (err.status === 401) { session = null; render(); } else if (!refreshFailed) { toast("Não foi possível atualizar os dados. Use Atualizar para tentar novamente.", true); refreshFailed = true; } }
    finally { refreshing = false; }
  }, window.VANDERCLUB_CONFIG.refreshMs || 30000);
  try {
    // Descarta somente as chaves da antiga versão local, sem importar dados fictícios.
    localStorage.removeItem("vanderclub.web.v1");
    sessionStorage.removeItem("vc.web.session");
  } catch {}
  svc = new V.ApiService(window.VANDERCLUB_CONFIG);
  svc.onUnauthorized = () => { session = null; D = null; chatDraft = ""; dialog.close(); Object.keys(drafts).forEach(k => delete drafts[k]); render(); };
  try { const actor = await svc.restore(); session = actor ? { id: actor.id } : null; render(); }
  catch (err) { session = null; loginPage(); toast(err.message, true); }
})();
