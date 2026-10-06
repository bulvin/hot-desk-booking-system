# Plan: Upgrade dependencies for .NET 10

Status: implemented and validated on 2026-10-06.

Roadmap: [Phase 3](../roadmap.md#phase-3-upgrade-dependencies-for-net-10).
See [requirements](requirements.md) and [validation](validation.md).

## 1. Review

- [x] Build and run existing tests to establish a baseline.
- [x] Review validation, MediatR, AutoMapper, Swagger, authentication, and test packages.
- [x] Check compatibility, support, security, and licensing using official sources.

## 2. Update

- [x] Choose whether to keep, upgrade, replace, or remove each reviewed package.
- [x] Apply changes in small steps and add focused regression tests.
- [x] Record selected versions, reasons, and source links below.

## 3. Verify

- [x] Complete the checks in [validation.md](validation.md).
- [x] Document the test and Docker smoke commands in the README.
- [x] Mark roadmap Phase 3 complete.

## Dependency decisions

Reviewed on 2026-10-06. Selected package licenses were also checked in the restored NuGet metadata.

| Area | Decision | Reason / source |
| --- | --- | --- |
| FluentValidation | Core and DI 12.1.1; remove ASP.NET integration | Keep the existing async pipeline. Integration was unused and deprecated. [Upgrade guide](https://docs.fluentvalidation.net/en/latest/upgrading-to-12.html). Apache-2.0. |
| MediatR | 12.4.1 to 12.5.0 | Preserve dispatch and the [Apache-2.0 license](https://github.com/LuckyPennySoftware/MediatR/blob/v12.5.0/LICENSE). Newer releases introduce [license-key configuration](https://www.nuget.org/packages/MediatR/14.2.0). |
| AutoMapper | Remove 13.0.1; use explicit DTO constructors | Three simple mapping call sites do not need a library. Removes the [security advisory](https://github.com/LuckyPennySoftware/AutoMapper/security/advisories/GHSA-rvv3-g6hj-g44x) and avoids adopting the [new licensing model](https://docs.automapper.io/en/latest/15.0-Upgrade-Guide.html). |
| Swagger | Both Swashbuckle packages to 10.2.3 | Retain used annotations; adapt schema and security configuration for [v10](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md). MIT. |
| Password hashing | BCrypt.Net-Next 4.0.3 to 4.2.0 | [Current .NET 10 support](https://www.nuget.org/packages/BCrypt.Net-Next/4.2.0); old hashes verified by regression test. MIT. |
| JWT | Keep 10.0.12 | Already aligned with the framework. Signature, expiration, login, and permissions verified. [Package](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.12). MIT. |
| Tests | xunit.v3 4.0.1; adapter 4.0.0 | Replace deprecated xUnit v2. Keep VSTest explicitly enabled for existing commands. [Migration guide](https://xunit.net/docs/getting-started/v3/migration). Apache-2.0. |
| Test tools | Test SDK 18.10.1; Moq 4.21.0; coverlet 10.1.0 | Current compatible versions; discovery and coverage verified. [SDK](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.10.1) / [Moq](https://www.nuget.org/packages/Moq) / [coverlet](https://www.nuget.org/packages/coverlet.collector/10.1.0). MIT / BSD-3-Clause / MIT. |

MediatR 12.5.0 is an intentional older-line choice, not a claim of ongoing upstream support. It has no reported audit findings and passes the regression checks. Reassess an upgrade or replacement during roadmap Phase 12 maintenance work.

The Application project now references the ASP.NET shared framework explicitly: its existing HTTP-context code previously obtained that reference through the removed validation integration. No booking rules or database schema changed.

Retain applicable license and copyright notices when distributing third-party components. The selected dependencies need no new commercial license configuration.
