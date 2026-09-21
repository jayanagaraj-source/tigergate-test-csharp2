# TigerGate Demo Shop (C# / .NET 8)

A realistic e-commerce backend built as a **scan target for TigerGate**, seeded so that
every default scanner — **SCA, SAST, Secrets, IaC, SBOM, CBOM** — has real findings to report.

> **This repository is a security test fixture.** It contains intentionally vulnerable code,
> fake credentials, broken cryptography and misconfigured infrastructure. Every secret in it
> is invalid demo data. Nothing here should ever be deployed or copied into real code.

---

## Why this repo exists

A previous scan of `tigergate-test-csharp` reported the SBOM wrongly:

| Symptom | What the SBOM showed |
|---------|----------------------|
| Wrong manifest path | `/bin/Debug/net8.0/tigergate-test-csharp.deps.json` |
| Repository listed as a package | `tigergate-test-csharp` `1.0.0` |
| Duplicate of the same entry | `tigergate-test-csharp` `1.0.0.x` |
| Far too few packages | 3 packages total |

**Root cause:** the scanner read the **build output** `.deps.json` instead of the source
manifests. A `.deps.json` lists the project's own assemblies as libraries with
`"type": "project"`, so the repository itself surfaced as a package — twice, because the
package version and assembly version differ.

This repo is built to prove that is fixed. Critically, it **still contains a committed
`bin/Debug/net8.0/*.deps.json`** — without that file present, the test would be vacuous.

## The `bin/` trap fixture

`src/TigerGateDemo.Api/bin/Debug/net8.0/TigerGateDemo.Api.deps.json` is committed **on purpose**,
via an explicit negation in [`.gitignore`](.gitignore). It contains **85 libraries**, three of
which are `"type": "project"` self-references:

```
TigerGateDemo.Api/1.4.0    -> "type": "project"
TigerGateDemo.Core/1.4.0   -> "type": "project"
TigerGateDemo.Data/1.4.0   -> "type": "project"
```

**A correct scan must:**

- ❌ never report a package named `TigerGateDemo.Api`, `TigerGateDemo.Core` or `TigerGateDemo.Data`
- ❌ never report a manifest path under `bin/` or `obj/`
- ✅ report exactly the 46 declarations from the source manifests below

If the SBOM package count jumps well past 46, the scanner is still ingesting `deps.json`
transitives. If a project name appears as a package, the self-reference bug is still live.

---

## Layout

```
TigerGateDemo.sln
├── src/
│   ├── TigerGateDemo.Core      net8.0  — entities, pricing policy, abstractions
│   ├── TigerGateDemo.Data      net8.0  — EF Core, repositories, Dapper reporting
│   ├── TigerGateDemo.Api       net8.0  — ASP.NET Core Web API + SAST/CBOM fixtures
│   ├── TigerGateDemo.Worker    net8.0  — background stock reconciliation
│   └── TigerGateDemo.Legacy    net472  — packages.config project (NOT in the .sln)
├── tests/TigerGateDemo.Tests   net8.0  — xUnit suite (16 tests)
├── deploy/
│   ├── k8s/                            — Kubernetes manifests (IaC fixtures)
│   ├── terraform/                      — AWS stack (IaC fixtures)
│   └── certs/                          — throwaway RSA-1024 test key
├── Dockerfile, docker-compose.yml      — container IaC fixtures
└── .env, .env.example                  — secret-scanning fixtures
```

`TigerGateDemo.Legacy` is deliberately excluded from the `.sln` so the solution builds on
Linux/macOS. It is still a real project with a real `packages.config`, so a scanner that walks
the **file tree** finds it while one that walks the **solution graph** misses it.

## Build and test

```bash
dotnet build TigerGateDemo.sln     # succeeds, 46 NU1902/NU1903 vulnerability warnings
dotnet test  TigerGateDemo.sln     # 16/16 pass
dotnet run --project src/TigerGateDemo.Api   # http://localhost:5080/swagger
```

`dotnet build`'s own NuGet audit emitting **46 vulnerability warnings** is a useful independent
cross-check against TigerGate's SCA results.

---

## What each scanner should find

### SCA — vulnerable dependencies

Known-vulnerable versions pinned on purpose:

| Package | Version | Declared in |
|---------|---------|-------------|
| `Newtonsoft.Json` | `9.0.1`, `12.0.1` | Legacy, Core, Data, Worker |
| `SixLabors.ImageSharp` | `2.1.0` | Api |
| `Azure.Identity` | `1.7.0` | Api |
| `RestSharp` | `105.2.3`, `106.11.7` | Legacy, Api |
| `SharpZipLib` | `0.86.0`, `1.1.0` | Legacy, Worker |
| `Npgsql` | `4.0.10` | Data |
| `System.Text.Json` | `6.0.0` | Core, Api |
| `System.Security.Cryptography.Xml` | `4.5.0` | Worker |
| `Microsoft.Data.SqlClient` | `5.1.2` | Data |
| `log4net` | `2.0.8` | Legacy |
| `System.Net.Http` | `4.3.0` | Legacy |
| `EntityFramework` | `6.2.0` | Legacy |

### SAST — code vulnerabilities

| Class | Location |
|-------|----------|
| SQL injection (string concat) | `Controllers/LegacyAuthController.cs` — `Login` |
| SQL injection (dynamic `ORDER BY`) | `Controllers/LegacyAuthController.cs` — `Audit` |
| Open redirect | `Controllers/LegacyAuthController.cs` — `Continue` |
| Hardcoded JWT signing key | `Controllers/LegacyAuthController.cs` |
| Unsalted MD5 password hashing | `Controllers/LegacyAuthController.cs` — `Login` |
| Error/schema disclosure | `Controllers/LegacyAuthController.cs` — catch block |
| Command injection | `Services/ReportExportService.cs` — `ConvertToPdfAsync` |
| Path traversal (read + write) | `Services/ReportExportService.cs` |
| World-writable output files | `Services/ReportExportService.cs` — `WriteExportAsync` |
| XXE (DTD + `XmlUrlResolver`) | `Services/ReportExportService.cs` — `ParseSupplierFeed` |
| SSRF | `Services/ReportExportService.cs` — `FetchRemoteTemplateAsync` |
| Disabled TLS cert validation | `Services/ReportExportService.cs` — `FetchRemoteTemplateAsync` |
| Unsafe deserialization (`TypeNameHandling.All`) | `Services/ReportExportService.cs` |
| Insecure randomness | `Services/CryptoService.cs` — `GenerateResetToken` |

### Secrets

| Type | Location |
|------|----------|
| AWS access key + secret | `.env`, `deploy/terraform/main.tf`, `deploy/k8s/secret.yaml` |
| Stripe test key + webhook secret | `.env`, `appsettings.Production.json`, `deploy/k8s/secret.yaml` |
| GitHub PAT | `.env`, `Dockerfile` |
| Slack bot token | `.env` |
| SendGrid API key | `.env` |
| DB connection strings with passwords | `.env`, `appsettings.Production.json`, `docker-compose.yml`, `deploy/k8s/deployment.yaml`, `LegacyAuthController.cs` |
| Azure Storage account key | `appsettings.Production.json` |
| JWT signing key | `.env`, `appsettings.Production.json`, `LegacyAuthController.cs`, k8s + compose |
| RSA private key file | `deploy/certs/service-signing.key` |
| NuGet API key | `Dockerfile` |

`.env.example` holds the same keys with empty values and should **not** be flagged — a useful
false-positive check.

**A note on GitHub push protection.** This lives in a public GitHub repository, and GitHub's push
protection rejects any push containing a credential its high-confidence validators recognise.
Three fixture values had to be softened to get the repo pushed at all:

| Fixture | Was | Now | Why |
|---------|-----|-----|-----|
| Stripe key | `sk_live_...` | `sk_test_...` | Test-mode keys are non-sensitive by design and are not blocked |
| Slack token | `xoxb-<digits>-<digits>-<alnum>` | `xoxb-EXAMPLE-...-NOT-REAL` | GitHub's Slack detector requires numeric segments |
| SendGrid key | `SG.<22 chars>.<43 chars>` | `SG.FIXTURE.NOT_A_REAL_SENDGRID_KEY` | Segment lengths no longer match the detector |

Each keeps its vendor prefix, so a scanner matching on prefix still classifies it correctly, but
a validator checking full structure will not. **All the remaining secret fixtures are untouched**
and still trip high-confidence detection: the AWS key pair, the `ghp_` GitHub PAT, the Azure
Storage account key, the `whsec_` Stripe webhook secret, every database connection string, the
JWT signing keys, the NuGet API key, and the RSA private key in `deploy/certs/`.

If you mirror this repo somewhere without push protection and want the stronger fixtures back,
restore the three values above to their full vendor formats.

### IaC

| Target | Misconfigurations |
|--------|-------------------|
| `Dockerfile` | `:latest` base tags, runs as root, secrets in `ENV`/`ARG`, `chmod 777`, `EXPOSE 22`, no `HEALTHCHECK`, apt cache retained |
| `docker-compose.yml` | `privileged`, `network_mode: host`, `pid: host`, `SYS_ADMIN`/`NET_ADMIN`, unconfined seccomp + apparmor, docker socket mount, `/` mounted, `0.0.0.0` port binds, plaintext passwords |
| `deploy/k8s/deployment.yaml` | `hostNetwork`/`hostPID`/`hostIPC`, `privileged`, `runAsUser: 0`, privilege escalation, writable root FS, added capabilities, hostPath `/` + docker socket, no resource limits, no probes, `:latest` |
| `deploy/k8s/secret.yaml` | plaintext `stringData`, `cluster-admin` ClusterRoleBinding |
| `deploy/terraform/main.tf` | hardcoded AWS creds, public-read-write S3 + disabled public access block, `0.0.0.0/0` on 22/3389/1433, publicly accessible unencrypted RDS, no backups, unencrypted SQS, `Action: "*"` / `Resource: "*"` IAM policy |

### CBOM — cryptographic inventory

`src/TigerGateDemo.Api/Services/CryptoService.cs` is a single-file crypto inventory:

| Category | Primitives |
|----------|-----------|
| Hashes | MD5, SHA-1, SHA-256, SHA-512 |
| Symmetric | AES-256-CBC, AES-128-ECB, DES, TripleDES |
| Asymmetric | RSA-1024 (PKCS#1 v1.5), RSA-4096 (OAEP-SHA256), ECDSA P-384 |
| MAC | HMAC-SHA256, HMAC-MD5 |
| KDF | PBKDF2-SHA1 @ 1,000 iters (static salt), PBKDF2-SHA256 @ 210,000 iters |
| RNG | `System.Random` (insecure), `RandomNumberGenerator` (secure) |
| Key material | hardcoded AES key + static IV, hardcoded HMAC secret, RSA-1024 PEM in `deploy/certs/` |

Both strong and weak variants are present on purpose, so the CBOM is checked on
**classification**, not just detection.

### SBOM — package inventory

The ground truth follows. Regenerate with `python3 scripts/inventory.py`.

| # | Package | Version | Declared in | Line |
|---|---------|---------|-------------|------|
| 1 | `Azure.Identity` | `1.7.0` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 21 |
| 2 | `Dapper` | `2.0.90` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 20 |
| 3 | `EntityFramework` | `6.2.0` | `src/TigerGateDemo.Legacy/packages.config` | 9 |
| 4 | `FluentAssertions` | `6.12.0` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 19 |
| 5 | `FluentValidation` | `10.3.6` | `src/TigerGateDemo.Core/TigerGateDemo.Core.csproj` | 13 |
| 6 | `FluentValidation.AspNetCore` | `10.3.6` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 26 |
| 7 | `log4net` | `2.0.8` | `src/TigerGateDemo.Legacy/packages.config` | 3 |
| 8 | `Microsoft.AspNet.WebApi.Client` | `5.2.6` | `src/TigerGateDemo.Legacy/packages.config` | 6 |
| 9 | `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.0` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 20 |
| 10 | `Microsoft.Data.SqlClient` | `5.1.2` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 19 |
| 11 | `Microsoft.EntityFrameworkCore` | `8.0.0` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 15 |
| 12 | `Microsoft.EntityFrameworkCore.InMemory` | `8.0.0` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 17 |
| 13 | `Microsoft.EntityFrameworkCore.Relational` | `8.0.0` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 16 |
| 14 | `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.0` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 18 |
| 15 | `Microsoft.Extensions.Caching.Memory` | `8.0.0` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 23 |
| 16 | `Microsoft.Extensions.Hosting` | `8.0.0` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 17 |
| 17 | `Microsoft.Extensions.Logging.Abstractions` | `8.0.0` | `src/TigerGateDemo.Core/TigerGateDemo.Core.csproj` | 14 |
| 18 | `Microsoft.NET.Test.Sdk` | `17.8.0` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 16 |
| 19 | `Moq` | `4.18.4` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 20 |
| 20 | `Newtonsoft.Json` | `13.0.1` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 24 |
| 21 | `Newtonsoft.Json` | `12.0.1` | `src/TigerGateDemo.Core/TigerGateDemo.Core.csproj` | 11 |
| 22 | `Newtonsoft.Json` | `12.0.1` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 22 |
| 23 | `Newtonsoft.Json` | `9.0.1` | `src/TigerGateDemo.Legacy/packages.config` | 4 |
| 24 | `Newtonsoft.Json` | `12.0.1` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 23 |
| 25 | `Newtonsoft.Json` | `13.0.3` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 21 |
| 26 | `Npgsql` | `4.0.10` | `src/TigerGateDemo.Data/TigerGateDemo.Data.csproj` | 21 |
| 27 | `Polly` | `7.2.3` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 24 |
| 28 | `RestSharp` | `106.11.7` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 22 |
| 29 | `RestSharp` | `105.2.3` | `src/TigerGateDemo.Legacy/packages.config` | 7 |
| 30 | `Serilog.AspNetCore` | `6.0.1` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 17 |
| 31 | `Serilog.Extensions.Hosting` | `5.0.1` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 18 |
| 32 | `Serilog.Settings.Configuration` | `3.4.0` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 19 |
| 33 | `Serilog.Sinks.Console` | `4.0.1` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 18 |
| 34 | `Serilog.Sinks.Console` | `4.0.1` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 20 |
| 35 | `SharpZipLib` | `0.86.0` | `src/TigerGateDemo.Legacy/packages.config` | 8 |
| 36 | `SharpZipLib` | `1.1.0` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 21 |
| 37 | `SixLabors.ImageSharp` | `2.1.0` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 23 |
| 38 | `Swashbuckle.AspNetCore` | `6.5.0` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 16 |
| 39 | `System.IdentityModel.Tokens.Jwt` | `7.0.3` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 19 |
| 40 | `System.Net.Http` | `4.3.0` | `src/TigerGateDemo.Legacy/packages.config` | 5 |
| 41 | `System.Security.Cryptography.Xml` | `4.5.0` | `src/TigerGateDemo.Worker/TigerGateDemo.Worker.csproj` | 22 |
| 42 | `System.Text.Json` | `6.0.0` | `src/TigerGateDemo.Api/TigerGateDemo.Api.csproj` | 25 |
| 43 | `System.Text.Json` | `6.0.0` | `src/TigerGateDemo.Core/TigerGateDemo.Core.csproj` | 12 |
| 44 | `xunit` | `2.6.2` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 17 |
| 45 | `xunit.runner.visualstudio` | `2.5.4` | `tests/TigerGateDemo.Tests/TigerGateDemo.Tests.csproj` | 18 |
| 46 | `YamlDotNet` | `11.2.1` | `src/TigerGateDemo.Core/TigerGateDemo.Core.csproj` | 15 |

**Total package declarations: 46** &nbsp;&nbsp; **Distinct names: 37** &nbsp;&nbsp; **Multi-manifest packages: 5**

| Package | Times declared | Versions |
|---------|----------------|----------|
| `Newtonsoft.Json` | 6 | `12.0.1`, `13.0.1`, `13.0.3`, `9.0.1` |
| `RestSharp` | 2 | `105.2.3`, `106.11.7` |
| `Serilog.Sinks.Console` | 2 | `4.0.1` |
| `SharpZipLib` | 2 | `0.86.0`, `1.1.0` |
| `System.Text.Json` | 2 | `6.0.0` |

---

## Verification checklist

**The original bug**

- [ ] No package is named `TigerGateDemo.Api`, `TigerGateDemo.Core`, `TigerGateDemo.Data`, or after the repository.
- [ ] No finding's manifest path contains `bin/` or `obj/` — despite `TigerGateDemo.Api.deps.json` being committed.
- [ ] `Newtonsoft.Json` yields **6 entries across 4 versions** (`9.0.1`, `12.0.1`, `13.0.1`, `13.0.3`) — not 1 collapsed and not 6 identical rows.
- [ ] Total SBOM entries track the 46 source declarations, not the 85 libraries in `deps.json`.
- [ ] Every finding's file **and line** match the inventory table above.
- [ ] `src/TigerGateDemo.Legacy` is scanned even though it is absent from `TigerGateDemo.sln`.

**All scanners enabled**

- [ ] **SCA** — the 12 vulnerable packages above are flagged, roughly matching the 46 NuGet audit warnings.
- [ ] **SAST** — the 14 code findings above are reported at the right file and line.
- [ ] **Secrets** — the 11 secret types above are found; `.env.example` is *not* flagged.
- [ ] **IaC** — Dockerfile, compose, both k8s manifests and Terraform all produce findings.
- [ ] **SBOM** — 46 declarations, 37 distinct packages, correct manifest paths.
- [ ] **CBOM** — weak primitives (MD5, SHA-1, DES, 3DES, ECB, RSA-1024) separated from strong ones.
# tigergate-test-csharp2
# tigergate-test-csharp2
