# Frontend

The standalone React/TypeScript frontend lives in `frontend/`. It uses Vite,
React Router, TanStack Query, Keycloak Authorization Code with PKCE, Tailwind CSS 4,
and daisyUI 5. Existing dependency versions are recorded in `package-lock.json`;
this reorganization adds no dependencies.

## Structure

Use a lightweight feature-based structure inspired by vertical slices:

```text
frontend/src/
  main.tsx                # Browser entry point and provider initialization
  app/                    # App shell, route composition, global stylesheet
  features/
    desks/                # Desk search, booking, and desk-change selection
    reservations/         # Personal history and cancellation
    administration/       # Office and desk administration
  shared/
    api/                  # HTTP client, Problem Details handling, API contracts
    auth/                 # Keycloak session and query-cache lifecycle
    lib/                  # Shared UTC calendar rules
    ui/                   # Reusable form, feedback, dialog, and office controls
  tests/                  # Cross-feature workflow tests and test setup
```

Keep feature-specific queries, mutations, state, and components beside their
workflow. Extract them into smaller files within that feature when needed.
`app` composes features; shared modules must not import features or the app.
Reservations intentionally reuses the desk-selection workflow for desk changes.
Avoid generic services/repositories and frontend copies of the backend's four
layers. The ASP.NET API retains its existing architecture and remains authoritative
for validation, authorization, availability, and booking cutoffs.

## UI conventions

Read `.agents/skills/daisyui/SKILL.md` and its applicable references before editing
JSX. Use daisyUI 5 component classes and semantic colors (`base-*`, `error`,
`success`). Inputs and selects have borders by default; do not use the old
`input-bordered` or `select-bordered` classes. Prefer component classes and Tailwind
utilities over global component CSS overrides. Use the default button variant;
reserve primary emphasis for a deliberate main action. Keep visible labels,
keyboard interaction, native dialogs, loading feedback, and error messages.

## Local development

Start the API, PostgreSQL, Keycloak, and Mailpit using the repository's existing
local configuration. Then, using Node.js 24:

```powershell
cd frontend
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:5173`. The Vite server proxies `/api` to
`http://localhost:8080`; for a directly running API, set `API_PROXY_TARGET` to
`http://localhost:5080` in `.env.local`. The example file lists public Keycloak
settings. Never place a client secret in browser configuration. Keycloak must
allow this frontend's origin and `/auth/callback` redirect.

Run `npm run typecheck`, `npm run lint`, `npm test`, `npm run build`, and
`npm run format:check` from `frontend/`. CI runs the same checks.

The frontend runs directly through Vite for local development. A separate static
web-server container can serve its build output later; production hosting and
deployment remain outside this change.

## Scope and contracts

The UI supports desk search and booking, personal reservation history, desk
changes, cancellation, and administrator office/desk management. Location browsing
and personal reservation listing use the approved authenticated read APIs.
Company-wide reservation browsing remains deferred. Hide administrator navigation
for employees, and enforce permissions in the API. Reservation dates remain
inclusive `YYYY-MM-DD` values on the UTC calendar; do not convert them to local
timestamps. Keep cached authenticated data scoped to the current session.
