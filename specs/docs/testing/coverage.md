# Test Inventory — ContosoUniversity

_Extracted on 2026-05-13. Catalogs all tests that exist in the project._

> **Scope:** This inventory covers the brownfield target `src/ContosoUniversity` only. The `src/AssetManager` Java/Spring Boot project is a separate workload and is out of scope for this brownfield extraction.

---

## Summary

| Metric | Value |
|---|---|
| Test frameworks detected | **None** |
| Test projects (`*.Tests.csproj` / `*Test.csproj`) | 0 |
| Test files (`*Test*.cs`, `*Spec*.cs`, `*.test.*`, `*.spec.*`) | 0 |
| Total automated test cases | 0 |
| Active tests | 0 |
| Skipped tests | 0 |
| Coverage reports in repository | 0 |
| Configured coverage thresholds | None |
| CI test stages | No CI configuration files present in `src/ContosoUniversity` |

The project has **no automated test suite**. There are no unit tests, no integration tests, no end-to-end tests, no contract tests, no smoke tests. Verification of behavior is performed manually using the in-app diagnostic pages and the manual test guide documented below.

---

## Test Framework Detection

Each test framework typically used with .NET / .NET Framework projects was checked. None are present.

| Framework | Detection method | Result |
|---|---|---|
| **xUnit** | NuGet packages in `packages.config`; `[Fact]` / `[Theory]` attributes in `*.cs` | Not present |
| **NUnit** | `NUnit` NuGet package; `[Test]` / `[TestFixture]` attributes | Not present |
| **MSTest** | `MSTest.TestFramework` / `Microsoft.VisualStudio.TestPlatform.*` packages; `[TestMethod]` / `[TestClass]` attributes | Not present |
| **Moq** | `Moq` NuGet package | Not present |
| **NSubstitute** | `NSubstitute` NuGet package | Not present |
| **FluentAssertions** | `FluentAssertions` NuGet package | Not present |
| **AutoFixture** | `AutoFixture` NuGet package | Not present |
| **SpecFlow** | `SpecFlow` NuGet package; `*.feature` files | Not present |
| **Selenium** | `Selenium.WebDriver` NuGet package | Not present |
| **Playwright (.NET)** | `Microsoft.Playwright` NuGet package; `playwright.config.*` | Not present |
| **Cypress** | `cypress.config.*`; `cypress/` directory | Not present |
| **coverlet** | `coverlet.collector` / `coverlet.msbuild` NuGet packages | Not present |
| **ReportGenerator** | `ReportGenerator` NuGet package | Not present |
| **NCrunch / dotCover** | Configuration files for NCrunch or dotCover | Not present |

`packages.config` was searched for the regex `xunit|nunit|MSTest|Microsoft.VisualStudio.TestPlatform|Moq|FluentAssertions|NSubstitute|Selenium|Playwright|SpecFlow|coverlet|NCrunch` — **zero matches**.

---

## Test File Search

Filesystem and content searches yielded no test files.

| Search | Result |
|---|---|
| Files matching `**/*Test*.cs` under the workspace | 1 file: `src/ContosoUniversity/Controllers/MessageQueueTestController.cs` — **this is a production controller**, not an automated test (see "False positives" below) |
| Files matching `**/*Spec*.cs` | 0 files |
| Files matching `**/*.test.*` | 0 files |
| Files matching `**/*.spec.*` | 0 files |
| Directories named `tests/`, `test/`, `e2e/`, `__tests__/`, `spec/` under `src/ContosoUniversity` | None |
| `*.cs` files containing `[Fact]`, `[Theory]`, `[Test]`, `[TestMethod]`, `[TestClass]`, or `[TestFixture]` | 0 matches |
| Project files (`*.csproj`) whose name contains `Test` | 0 matches |
| Solution `ContosoUniversity.sln` references to additional projects | Single project (`ContosoUniversity.csproj`); no test projects referenced |

### False positives flagged

| File | Why it surfaced | Why it is not a test |
|---|---|---|
| `src/ContosoUniversity/Controllers/MessageQueueTestController.cs` | Filename contains "Test" | This is an MVC controller compiled into the production application. Its actions render Razor views and exercise the in-process notification queue from the browser. It uses no test framework, has no test attributes, and is invoked at runtime by HTTP requests — see `specs/contracts/api/message-queue-test.yaml` |

---

## Test Cases

| Type | Active | Skipped | Todo | Focused |
|---|---|---|---|---|
| Unit | 0 | 0 | 0 | 0 |
| Integration | 0 | 0 | 0 | 0 |
| End-to-End | 0 | 0 | 0 | 0 |
| API / Contract | 0 | 0 | 0 | 0 |
| Component | 0 | 0 | 0 | 0 |
| Snapshot | 0 | 0 | 0 | 0 |
| Performance | 0 | 0 | 0 | 0 |
| Smoke | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** |

---

## Coverage Reports

No coverage reports were found in the repository.

| Coverage artifact | Location checked | Result |
|---|---|---|
| `coverage/` directory | workspace root and `src/ContosoUniversity/` | Not present |
| `htmlcov/`, `.coverage` | workspace root | Not present |
| `lcov.info`, `coverage.xml`, `cobertura.xml` | workspace root and `src/ContosoUniversity/` | Not present |
| `coverage-summary.json`, `coverage-final.json` | workspace root | Not present |
| Coverage tooling NuGet refs in `packages.config` | `src/ContosoUniversity/packages.config` | Not present |

No coverage thresholds are configured. No coverage upload is configured (no `codecov.yml`, no `.codecov.yml`, no Coveralls config).

---

## Test-to-Feature Mapping

Not applicable — there are no automated tests to map.

---

## Test Infrastructure

No test infrastructure is present:

- No shared test utilities, custom assertions, or test helpers.
- No fixtures or factory definitions.
- No `__mocks__/` or equivalent directory.
- No test database configuration distinct from the dev/runtime LocalDB instance.
- No test runner scripts in any `*.cmd` / `*.sh` file under `src/ContosoUniversity/scripts/` (the directory contains only `startapp.cmd`/`stopapp.cmd` for running the application — no test invocation).

---

## CI Integration

No CI configuration is present in the repository for the `src/ContosoUniversity` project:

| CI provider | Config file looked for | Result |
|---|---|---|
| GitHub Actions | `.github/workflows/*.yml` | None target ContosoUniversity test execution |
| Azure Pipelines | `azure-pipelines.yml`, `.azure-pipelines/` | Not present |
| GitLab CI | `.gitlab-ci.yml` | Not present |
| CircleCI | `.circleci/config.yml` | Not present |
| AppVeyor | `appveyor.yml` | Not present |
| Jenkins | `Jenkinsfile` | Not present |

No automated test execution is wired into any pipeline.

---

## Manual Verification Artifacts (Not Automated Tests)

The repository includes several Markdown documents that describe **manual** verification procedures. They are **not automated tests** but are catalogued here because they are the closest existing artifacts to a verification suite.

| File | Type | Description |
|---|---|---|
| `src/ContosoUniversity/SETUP_TESTING_GUIDE.md` | Manual end-user guide | Step-by-step browser instructions to verify the notification system after a build. Steps include "Press F5 to start debugging", "Click Students → Create New", "Watch for a green notification". Assumes Windows Authentication and an MSMQ installation that the runtime code does not actually require. |
| `src/ContosoUniversity/NOTIFICATION_SYSTEM_README.md` | Subsystem documentation | Describes the notification subsystem design and intended behavior |
| `src/ContosoUniversity/README_MessageQueue.md` | Subsystem documentation | Describes the message queue subsystem |
| `src/ContosoUniversity/TEACHING_MATERIAL_UPLOAD.md` | Feature documentation | Describes the file-upload feature on `Courses/Create` and `Courses/Edit` |
| `src/ContosoUniversity/PROMPTS.md` | Prompt log | Contains prior LLM prompts used during development |
| `src/ContosoUniversity/Controllers/MessageQueueTestController.cs` + `Views/MessageQueueTest/*` | In-app diagnostic UI | A browser-accessible page that exercises the in-process notification queue. The view is rendered to a human and outcomes are interpreted by eye — there are no assertions and no programmatic pass/fail signal. |

---

## Notes

- The application has **no `[TestController]` annotations** of any kind, **no test runner**, **no continuous integration test stage**, and **no recorded coverage measurement**.
- The MSBuild project file `ContosoUniversity.csproj` produces a single web application output and is not referenced by any test project.
- The solution file `ContosoUniversity.sln` lists only the `ContosoUniversity` project — no test project entries.
- All references in the workspace tree to "test" (the README files, the `MessageQueueTestController`, and the `Views/MessageQueueTest/` views) are about exercising the notification subsystem from a browser. They are part of the production application surface.
