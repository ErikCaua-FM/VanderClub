/* Servidor estático local de conveniência; não é a API do VanderClub. */
const http = require("node:http"),
  fs = require("node:fs"),
  path = require("node:path");
const root = path.resolve(__dirname, "dist"),
  port = Number(process.env.VANDERCLUB_PORT || 8080);
const mime = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
  ".ttf": "font/ttf",
  ".txt": "text/plain; charset=utf-8",
  ".zip": "application/zip",
  ".jpg": "image/jpeg",
  ".png": "image/png",
};
const server = http.createServer((req, res) => {
  try {
    if (!["GET", "HEAD"].includes(req.method)) {
      res.writeHead(405, { Allow: "GET, HEAD" });
      return res.end();
    }
    const u = new URL(req.url, "http://localhost"),
      urlPath = decodeURIComponent(u.pathname),
      p = path.resolve(root, "." + (urlPath === "/" ? "/index.html" : urlPath));
    if (!p.startsWith(root + path.sep)) {
      res.writeHead(403);
      return res.end("Acesso negado");
    }
    fs.stat(p, (err, st) => {
      if (err || !st.isFile()) {
        res.writeHead(404);
        return res.end("Arquivo não encontrado");
      }
      res.writeHead(200, {
        "Content-Type": mime[path.extname(p)] || "application/octet-stream",
        "Content-Length": st.size,
        "Cache-Control": "no-cache",
        "X-Content-Type-Options": "nosniff",
        "Referrer-Policy": "same-origin",
      });
      if (req.method === "HEAD") return res.end();
      fs.createReadStream(p).pipe(res);
    });
  } catch {
    res.writeHead(400);
    res.end("Endereço inválido");
  }
});
server.on("error", (e) => {
  console.error("Não foi possível iniciar o VanderClub:", e.message);
  process.exitCode = 1;
});
server.listen(port, "127.0.0.1", () =>
  console.log(
    `VanderClub pronto em http://localhost:${port}\nMantenha este terminal aberto. Para encerrar: Ctrl+C.`,
  ),
);
