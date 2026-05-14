// cucumber.cjs — Cucumber.js configuration for spec2cloud integration tests
// Targets: legacy ContosoUniversity MVC 5 app on IIS Express (https://localhost:44300)
//          and (later) the ASP.NET Core 8 rewrite on Kestrel (https://localhost:7000).
//
// Run: npm run test:integration
// Prereqs: legacy app must be started via `src/ContosoUniversity/scripts/startapp.cmd`
//          (IIS Express bound to https://localhost:44300).

module.exports = {
  default: {
    paths: ['tests/integration/features/**/*.feature'],
    import: ['tests/integration/steps/**/*.ts', 'tests/integration/support/**/*.ts'],
    format: [
      'progress-bar',
      'summary',
      'json:tests/integration/.reports/cucumber-report.json',
      'html:tests/integration/.reports/cucumber-report.html'
    ],
    formatOptions: { snippetInterface: 'async-await' }
  }
};
