/* Transporte HTTP. Sem persistência de registros ou autenticação local. */
(function (root) {
  "use strict";
  const V = root.VC;
  class ApiError extends Error {
    constructor(message, status = 0, requestId = "") {
      super(message); this.name = "ApiError"; this.status = status; this.requestId = requestId;
    }
  }
  class ApiService {
    constructor(config = {}, fetcher = (...args) => fetch(...args)) {
      this.config = config; this.fetcher = fetcher; this.data = null;
      this.csrfToken = ""; this.pending = new Map(); this.epoch = 0;
      this.onUnauthorized = null; this.refreshPromise = null;
    }
    endpoint(path) {
      if (!this.config.apiBaseUrl) throw new ApiError("O serviço do VanderClub ainda não foi configurado. O acesso estará disponível após a integração com a API.");
      const base = new URL(this.config.apiBaseUrl, root.location?.origin);
      const local = ["localhost", "127.0.0.1", "[::1]"].includes(base.hostname);
      if (base.protocol !== "https:" && !(base.protocol === "http:" && local))
        throw new ApiError("O serviço precisa utilizar uma conexão HTTPS.");
      if (base.username || base.password || base.search || base.hash)
        throw new ApiError("A configuração do serviço contém um endereço inválido.");
      return new URL(path, base.href.replace(/\/$/, "") + "/").href;
    }
    clear() { this.epoch++; this.data = null; this.csrfToken = ""; this.pending.clear(); this.refreshPromise = null; }
    async request(method, path, body, explicitKey) {
      const url = this.endpoint(path), headers = { Accept: "application/json" };
      const serialized = body === undefined ? undefined : JSON.stringify(body);
      const mutation = method !== "GET";
      const identity = method + ":" + path + ":" + (serialized || "");
      if (serialized !== undefined) headers["Content-Type"] = "application/json";
      if (mutation && this.csrfToken) headers["X-CSRF-Token"] = this.csrfToken;
      if (mutation && !path.startsWith("auth/")) {
        if (!this.pending.has(identity)) this.pending.set(identity, explicitKey || V.uid("request"));
        headers["Idempotency-Key"] = this.pending.get(identity);
      }
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), this.config.timeoutMs || 25000);
      try {
        const response = await this.fetcher(url, { method, credentials: "include", cache: "no-store", headers,
          body: serialized, signal: controller.signal, redirect: "error" });
        const text = await response.text(); let value = null;
        try { if (text) value = JSON.parse(text); } catch {
          if (response.ok) throw new ApiError("O serviço retornou um formato incompatível.");
        }
        if (!response.ok) {
          if ([409, 422].includes(response.status)) this.pending.delete(identity);
          if (response.status === 401) { this.clear(); this.onUnauthorized?.(); }
          const fallback = { 401: "E-mail ou senha incorretos, ou sessão expirada.", 403: "Você não tem permissão para esta ação.",
            409: "Este registro mudou. Atualize a página antes de continuar.", 429: "Aguarde um momento antes de tentar novamente." };
          throw new ApiError(value?.message || fallback[response.status] || "O serviço não conseguiu concluir a solicitação.", response.status, value?.requestId || "");
        }
        return value;
      } catch (error) {
        if (error instanceof ApiError) throw error;
        throw new ApiError(error.name === "AbortError"
          ? "O serviço demorou para responder. Confira o registro antes de tentar novamente."
          : "Não foi possível conectar ao serviço. Confira a conexão e tente novamente.");
      } finally { clearTimeout(timer); }
    }
    acceptSession(value) {
      if (!value?.actor || !["ADM_GERAL", "ADM_LOCAL"].includes(value.actor.role) || !value.csrfToken)
        throw new ApiError("A resposta de autenticação é incompatível com este portal.");
      this.csrfToken = value.csrfToken; return value.actor;
    }
    async login(address, password) {
      V.email(address); V.required(password, "a senha", 128); this.clear();
      const actor = this.acceptSession(await this.request("POST", "auth/admin/login", { email: address.trim().toLowerCase(), password }));
      await this.refresh(); return actor;
    }
    async restore() {
      if (!this.config.apiBaseUrl) return null;
      try {
        const actor = this.acceptSession(await this.request("GET", "auth/admin/session"));
        await this.refresh(); return actor;
      } catch (err) { this.clear(); if (err.status === 401) return null; throw err; }
    }
    async logout() { await this.request("POST", "auth/admin/logout"); this.clear(); }
    view() { if (!this.data) throw new ApiError("Sua sessão precisa ser iniciada novamente.", 401); return this.data; }
    async refresh() {
      if (this.refreshPromise) return this.refreshPromise;
      const epoch = this.epoch;
      const task = (async () => {
        const data = await this.request("GET", "admin/bootstrap");
        const lists = ["units", "admins", "areas", "people", "contracts", "invoices", "events", "polls", "votes", "tickets", "messages", "plans", "reservations", "audit"];
        if (!data?.actor || lists.some(name => !Array.isArray(data[name])))
          throw new ApiError("Os dados recebidos não correspondem ao contrato da API.");
        if (!["ADM_GERAL", "ADM_LOCAL"].includes(data.actor.role)) throw new ApiError("Acesso restrito à administração.", 403);
        if (epoch !== this.epoch) throw new ApiError("A sessão foi encerrada.", 401);
        this.data = data; return data;
      })();
      this.refreshPromise = task;
      try { return await task; } finally { if (this.refreshPromise === task) this.refreshPromise = null; }
    }
    tick() { return this.refresh(); }
    async mutate(method, path, body, key) {
      const result = await this.request(method, path, body, key);
      // Força uma leitura posterior à gravação; não reutiliza requisição iniciada antes dela.
      if (this.refreshPromise) { try { await this.refreshPromise; } catch {} }
      try { await this.refresh(); }
      catch (error) { if (error.status === 401) throw error; throw new ApiError("A alteração foi recebida pelo servidor, mas a lista não pôde ser atualizada. Use Atualizar para conferir."); }
      this.pending.delete(method + ":" + path + ":" + (body === undefined ? "" : JSON.stringify(body)));
      return result;
    }
    edit(collection, p) {
      const { id, confirmPassword, ...body } = p;
      if (body.version !== undefined) body.version = body.version ? Number(body.version) : undefined;
      return this.mutate(id ? "PUT" : "POST", `admin/${collection}${id ? "/" + encodeURIComponent(id) : ""}`, body);
    }
    version(table, id) { return V.find(this.view(), table, id).version; }
    saveUnit(actorId, p) { return this.edit("units", p); }
    saveAdmin(actorId, p) {
      if ((!p.id || p.password) && p.password !== p.confirmPassword) throw new ApiError("As senhas não coincidem.");
      if (!p.password) { p = { ...p }; delete p.password; }
      return this.edit("administrators", p);
    }
    saveEvent(actorId, p) {
      const tz = V.find(this.view(), "units", p.unitId).timezone;
      const data = { ...p, capacity: Number(p.capacity), areaId: p.areaId || null };
      for (const k of ["start", "end", "regStart", "regEnd"]) data[k] = V.toUtc(p[k], tz);
      if (data.end <= data.start) throw new ApiError("O término precisa ser posterior ao início.");
      if (data.regStart > data.regEnd || data.regEnd > data.start) throw new ApiError("Confira o período de inscrições.");
      return this.edit("events", data);
    }
    cancelEvent(actorId, id, reason) { return this.mutate("POST", `admin/events/${encodeURIComponent(id)}/cancel`, { reason, version: this.version("events", id) }); }
    savePoll(actorId, p) {
      const tz = V.find(this.view(), "units", p.unitId).timezone;
      const start = V.toUtc(p.start, tz), end = V.toUtc(p.end, tz);
      if (end <= start) throw new ApiError("O término precisa ser posterior ao início.");
      if (p.options.length < 2 || p.options.length > 8 || new Set(p.options.map(V.norm)).size !== p.options.length)
        throw new ApiError("Informe de 2 a 8 opções diferentes.");
      const { initialStatus, ...body } = p;
      return this.mutate("POST", "admin/polls", { ...body, start, end, quorum: Number(p.quorum) });
    }
    closePoll(actorId, id) { return this.mutate("POST", `admin/polls/${encodeURIComponent(id)}/close`, { version: this.version("polls", id) }); }
    reviewGuest(actorId, id, decision, reason, version) { return this.mutate("POST", `admin/beneficiaries/${encodeURIComponent(id)}/review`, { decision, reason, version: Number(version) }); }
    suspend(actorId, id, reason) { return this.mutate("POST", `admin/contracts/${encodeURIComponent(id)}/suspend`, { reason, version: this.version("contracts", id) }); }
    reactivate(actorId, id) { return this.mutate("POST", `admin/contracts/${encodeURIComponent(id)}/reactivate`, { version: this.version("contracts", id) }); }
    reply(actorId, id, text, version) { return this.mutate("POST", `admin/tickets/${encodeURIComponent(id)}/responses`, { text, version: Number(version) }); }
    send(actorId, unitId, text, key) { return this.mutate("POST", `admin/units/${encodeURIComponent(unitId)}/messages`, { text: V.required(text, "a mensagem", 2000) }, key); }
    pin(actorId, id) {
      const message = V.find(this.view(), "messages", id);
      return this.mutate("PUT", `admin/messages/${encodeURIComponent(id)}/pin`, { pinned: !message.pinned, version: message.version });
    }
    savePlan(actorId, p) {
      const { installmentValue, cashValue, installments, ...rest } = p;
      const count = V.integer(installments, "Parcelas", 1, 36);
      const body = { ...rest, conditions: [
        { name: "Parcelado", method: "BOLETO", installments: count, totalCents: V.cents(installmentValue) * count },
        { name: "À vista", method: "PIX", installments: 1, totalCents: V.cents(cashValue) } ] };
      for (const k of ["quota", "dependentLimit", "guestLimit", "durationMonths", "graceDays", "futureReservationsLimit"]) body[k] = Number(body[k]);
      return this.edit("plans", body);
    }
    recordPayment(actorId, p) {
      const d = this.view();
      if (d.actor.role !== "ADM_GERAL") throw new ApiError("Esta ação é exclusiva do ADM Geral.", 403);
      const invoice = V.find(d, "invoices", p.invoiceId), contract = V.find(d, "contracts", invoice.contractId);
      const tz = V.find(d, "units", contract.unitId).timezone;
      return this.mutate("POST", `admin/invoices/${encodeURIComponent(invoice.id)}/payments`, {
        version: invoice.version, receivedAt: V.toUtc(p.receivedAt, tz),
        method: p.method, reference: V.required(p.reference, "a referência do comprovante", 200), note: p.note || ""
      });
    }
    archivePlan(actorId, id, version) { return this.mutate("POST", `admin/plans/${encodeURIComponent(id)}/archive`, { version: Number(version) }); }
  }
  root.VC.ApiError = ApiError; root.VC.ApiService = ApiService;
})(globalThis);
