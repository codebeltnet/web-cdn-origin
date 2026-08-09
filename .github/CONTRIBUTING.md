# Contributing to Static Content Provider

Thank you for your interest in contributing!

## How to Contribute

1. Fork the repository.
2. Create a feature branch.
3. Commit your changes following the project's commit conventions.
4. Push to your branch and open a pull request against `main`.

## Development Setup

```bash
dotnet restore Codebelt.Cdn.Origin.slnx
dotnet build Codebelt.Cdn.Origin.slnx
dotnet test Codebelt.Cdn.Origin.slnx
```

## Code Standards

- Target `net10.0` and use modern minimal hosting.
- Prefer ASP.NET Core and the BCL over new dependencies. New dependencies require justification (see `AGENTS.md`).
- Use file-scoped namespaces and follow the existing code style (enforced via `.editorconfig`; verify with `dotnet format --severity info --verify-no-changes`).
- All public APIs must have XML documentation comments.
- Behavioural changes to the HTTP contract require functional tests and a `README.md` update.

## Pull Request Guidelines

- Keep pull requests focused on a single concern.
- Maintain 100% line and branch coverage for application-owned decision logic.
- Update `CHANGELOG.md`.
- Ensure all CI checks pass before requesting review.
