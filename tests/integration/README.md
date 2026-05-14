# spec2cloud Integration Tests

Cucumber.js + Node.js HTTP-based integration tests for the ContosoUniversity
modernization project.

## Stack

- **Runner**: [Cucumber.js v11](https://github.com/cucumber/cucumber-js)
- **Language**: TypeScript via [tsx](https://github.com/privatenumber/tsx) (ESM)
- **HTTP**: `curl.exe` (Windows built-in, Schannel/SSPI build) — invoked via
  `node:child_process`. We shell out to curl because the legacy app requires
  Windows Authentication and Node's built-in `fetch` does not negotiate NTLM.
  Once `rw-001` brings up the ASP.NET Core 8 rewrite (which authenticates via
  cookies / Entra ID instead of Windows Auth), new tests targeting the rewrite
  can switch to `fetch` directly.
- **Browser tests**: not yet wired — Playwright will be added in increment **rw-002+**
  when the first server-rendered Razor page on Kestrel needs DOM-level assertions.

## Layout

```
tests/integration/
├── features/                    # Gherkin .feature files
│   └── sec-001-csrf-mark-as-read.feature
├── steps/                       # TypeScript step definitions
│   └── notifications-csrf.steps.ts
└── support/                     # Cucumber World + hooks
    ├── world.ts
    └── hooks.ts
```

## Conventions

- One `.feature` file per spec2cloud increment ID (e.g. `sec-001-*.feature`,
  `rw-003-*.feature`). Tag with `@<increment-id>` so the suite can be filtered.
- Tags:
  - `@legacy` — exercises the legacy ASP.NET MVC 5 endpoint at `https://localhost:44300`.
  - `@rewrite` — exercises the ASP.NET Core 8 rewrite at `https://localhost:7000`.
  - `@feature-F-NNN` — links the scenario back to its FRD.
  - `@existing-behavior` — green-baseline scenario (must pass against current code).
  - `@security`, `@modernization` — increment category.
  - `@sec-high-NNN`, `@sec-med-NNN` — links to a security finding.
  - `@red-baseline` — scenario is expected to FAIL today (proves a bug).
  - `@green-after-fix` — scenario will pass once the corresponding fix lands.

## Prerequisites

1. **Node.js 22+** (matches the dev container baseline).
2. **`curl.exe` 8.x** (built into Windows 10/11 — no install needed).
3. **Legacy app running on IIS Express** at `https://localhost:44300`. There is
   no startup script for ContosoUniversity — start it one of two ways:
   - Open `src\ContosoUniversity\ContosoUniversity.sln` in Visual Studio 2022
     and press **F5** (recommended for first-time setup; warms up SQL Server
     LocalDB and applies any pending EF migrations).
   - Or launch IIS Express directly:
     ```pwsh
     & "$env:ProgramFiles\IIS Express\iisexpress.exe" `
         /config:"$PWD\src\ContosoUniversity\.vs\ContosoUniversity\config\applicationhost.config" `
         /site:ContosoUniversity
     ```
   The first run takes ~30s to compile + warm up the SQL Server LocalDB.
4. **Windows Authentication is required** by the legacy app
   (`<location path="ContosoUniversity">` in `applicationhost.config` sets
   `anonymousAuthentication=disabled`, `windowsAuthentication=enabled`). The
   test runner uses `curl.exe --ntlm --user :` to negotiate NTLM via SSPI with
   the current Windows user's default credentials — no manual credential
   handling is required.
5. **Self-signed cert is accepted** automatically — `curl.exe -k` plus
   `NODE_TLS_REJECT_UNAUTHORIZED=0` in `support/hooks.ts`. This applies to the
   test process only; production traffic must validate certs.

## Install + Run

```pwsh
# from the repository root
npm install

# run all integration scenarios
npm run test:integration

# run only the CSRF / sec-001 scenarios
npm run test:integration:csrf
```

## Configuration

| Env var          | Default                      | Purpose                                    |
| ---------------- | ---------------------------- | ------------------------------------------ |
| `LEGACY_BASE_URL` | `https://localhost:44300`    | Override the base URL used by `World`.    |

## Exit codes

- `0` — all scenarios passed (or matched their `@red-baseline` expectation).
- non-zero — at least one scenario failed.

## CI placeholder

CI wiring lands with `rw-001` (scaffold + CI workflow). Until then this suite is
intended to be run locally before opening a PR for any sec-* or rw-* increment.
