// scripts/start.js
const { spawn } = require("node:child_process");

const port = process.env.PORT || "3000";

const child = spawn(
  "npx next start -p " + port,
  {
    stdio: "inherit",
    shell: true,        // ★ これが重要
    env: process.env
  }
);

child.on("exit", (code) => process.exit(code ?? 0));
