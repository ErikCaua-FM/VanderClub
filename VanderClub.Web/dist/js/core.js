/* VanderClub: formatação e validações de interface. Regras de autorização pertencem à API. */
(function (root, factory) {
  const api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.VC = api;
})(globalThis, function () {
  "use strict";
  const clone = (x) => JSON.parse(JSON.stringify(x));
  const norm = (x) =>
    String(x ?? "")
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase()
      .trim();
  const digits = (x) => String(x ?? "").replace(/\D/g, "");
  const fail = (m) => {
    throw new Error(m);
  };
  const uid = (p) =>
    p +
    "_" +
    (globalThis.crypto.randomUUID?.() ||
      Array.from(crypto.getRandomValues(new Uint8Array(16)), (x) =>
        x.toString(16).padStart(2, "0"),
      ).join(""));
  const required = (v, label, max = 250) => {
    v = String(v ?? "").trim();
    if (!v) fail("Preencha " + label + ".");
    if (v.length > max) fail(label + ": máximo de " + max + " caracteres.");
    return v;
  };
  const email = (v) => {
    v = required(v, "o e-mail", 254).toLowerCase();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v))
      fail("Informe um e-mail válido.");
    return v;
  };
  const phone = (v) => {
    v = digits(v);
    if (v.length < 10 || v.length > 13) fail("Informe um telefone com DDD.");
    return v;
  };
  const cpf = (v) => {
    v = digits(v);
    if (!/^\d{11}$/.test(v) || new Set(v).size === 1)
      fail("Informe um CPF válido.");
    for (let n = 9; n < 11; n++) {
      let s = 0;
      for (let i = 0; i < n; i++) s += Number(v[i]) * (n + 1 - i);
      let d = s % 11 < 2 ? 0 : 11 - (s % 11);
      if (d !== +v[n]) fail("Os dígitos do CPF não conferem.");
    }
    return v;
  };
  const integer = (v, label, min = 0, max = 100000) => {
    v = Number(v);
    if (!Number.isInteger(v) || v < min || v > max)
      fail(label + " deve estar entre " + min + " e " + max + ".");
    return v;
  };
  const date = (v) => {
    if (
      !/^\d{4}-\d{2}-\d{2}$/.test(v ?? "") ||
      !Number.isFinite(Date.parse(v + "T12:00:00Z")) ||
      new Date(v + "T12:00:00Z").toISOString().slice(0, 10) !== v
    )
      fail("Informe uma data válida.");
    return v;
  };
  const zone = (z) => {
    try {
      new Intl.DateTimeFormat("pt-BR", { timeZone: z }).format(new Date());
      return z;
    } catch {
      fail("Fuso horário inválido.");
    }
  };
  const parts = (iso, z = "America/Sao_Paulo") =>
    Object.fromEntries(
      new Intl.DateTimeFormat("en-CA", {
        timeZone: z,
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hourCycle: "h23",
      })
        .formatToParts(new Date(iso))
        .filter((p) => p.type !== "literal")
        .map((p) => [p.type, p.value]),
    );
  const localInput = (iso, z) => {
    const p = parts(iso, z);
    return `${p.year}-${p.month}-${p.day}T${p.hour}:${p.minute}`;
  };
  const today = (now = new Date().toISOString(), z) =>
    localInput(now, z).slice(0, 10);
  function toUtc(wall, z = "America/Sao_Paulo") {
    if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(wall ?? ""))
      fail("Preencha a data e o horário.");
    date(wall.slice(0, 10));
    if (+wall.slice(11, 13) > 23 || +wall.slice(14, 16) > 59)
      fail("Horário inválido.");
    const target = Date.parse(wall + ":00Z");
    let value = target;
    for (let i = 0; i < 3; i++) {
      let shown = Date.parse(
        localInput(new Date(value).toISOString(), z) + ":00Z",
      );
      value += target - shown;
    }
    const out = new Date(value).toISOString();
    if (localInput(out, z) !== wall)
      fail("Este horário não existe no fuso da unidade.");
    return out;
  }
  const addDays = (d, n) =>
    new Date(Date.parse(d + "T12:00:00Z") + n * 86400000)
      .toISOString()
      .slice(0, 10);
  function addMonths(d, n) {
    const dt = new Date(d + "T12:00:00Z"),
      day = dt.getUTCDate();
    dt.setUTCDate(1);
    dt.setUTCMonth(dt.getUTCMonth() + n);
    const last = new Date(
      Date.UTC(dt.getUTCFullYear(), dt.getUTCMonth() + 1, 0),
    ).getUTCDate();
    dt.setUTCDate(Math.min(day, last));
    return dt.toISOString().slice(0, 10);
  }
  const money = (n) =>
    new Intl.NumberFormat("pt-BR", {
      style: "currency",
      currency: "BRL",
    }).format(n / 100);
  function cents(v) {
    v = String(v ?? "").trim();
    if (!/^\d+(?:[.,]\d{1,2})?$/.test(v))
      fail("Informe o valor sem separador de milhar, como 149,90.");
    const [a, b = ""] = v.replace(",", ".").split(".");
    return integer(
      Number(a) * 100 + Number(b.padEnd(2, "0")),
      "Valor em centavos",
      1,
      100000000,
    );
  }
  const find = (d, table, id) =>
    d[table].find((x) => x.id === id) || fail("Registro não encontrado.");
  const invoiceStatus = (i, day) =>
    i.paidAt ? "Paga" : i.due < day ? "Atrasada" : "Pendente";
  const contractFor = (d, p) => find(d, "contracts", p.contractId);
  const pendingFor = (d, cid) =>
    d.people.filter(
      (p) =>
        p.contractId === cid &&
        p.type !== "Titular" &&
        p.approval === "Pendente",
    );
  const overdueFor = (d, cid, now) => {
    const c = find(d, "contracts", cid),
      u = find(d, "units", c.unitId);
    return d.invoices.filter(
      (i) =>
        i.contractId === cid &&
        invoiceStatus(i, today(now, u.timezone)) === "Atrasada",
    );
  };
  const liveContract = (c, day) =>
    c.status === "Ativo" && c.start <= day && c.end > day;
  const pollStatus = (p, now) =>
    p.closedAt || now >= p.end
      ? "Encerrada"
      : now < p.start
        ? "Agendada"
        : "Aberta";
  function personFields(p, now, adult = false) {
    const birth = date(p.birth);
    if (birth > today(now) || birth < addMonths(today(now), -1440))
      fail("Confira a data de nascimento.");
    if (adult && birth > addMonths(today(now), -216))
      fail("O administrador deve ter pelo menos 18 anos.");
    const uf = required(p.uf, "a UF").toUpperCase();
    if (
      !"AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO"
        .split(" ")
        .includes(uf)
    )
      fail("Selecione uma UF válida.");
    const cep = digits(p.cep);
    if (cep.length !== 8) fail("O CEP deve ter 8 dígitos.");
    return {
      name: required(p.name, "o nome completo", 200),
      cpf: cpf(p.cpf),
      birth,
      phone: phone(p.phone),
      email: email(p.email),
      street: required(p.street, "o logradouro"),
      number: required(p.number, "o número", 20),
      district: required(p.district, "o bairro", 100),
      city: required(p.city, "a cidade", 150),
      uf,
      cep,
      complement: String(p.complement ?? "")
        .trim()
        .slice(0, 100),
      gender: String(p.gender ?? "").slice(0, 30),
      maritalStatus: String(p.maritalStatus ?? "").slice(0, 30),
    };
  }
  return { clone, norm, digits, required, email, phone, cpf, integer, date, zone, localInput, toUtc, today, addDays, addMonths, money, cents, uid, invoiceStatus, contractFor, pendingFor, overdueFor, liveContract, pollStatus, personFields, find };
});
