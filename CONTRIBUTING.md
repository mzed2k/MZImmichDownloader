# Contributing to MZImmichDownloader

Thank you for your interest in contributing to MZImmichDownloader!

Contributions of all kinds are welcome, including:

- Bug reports
- Bug fixes
- Performance improvements
- Documentation updates
- Feature suggestions
- Code cleanups and refactoring
- Compatibility improvements for newer Immich releases

## Reporting Issues

When creating an issue, please include:

- MZImmichDownloader version
- Immich version
- Windows version
- Relevant configuration (without API keys)
- Console output or error messages
- Steps required to reproduce the issue

## Development Guidelines

### Code Style

Please follow the existing coding style:

- Prefer readability over cleverness
- Keep dependencies to a minimum
- Comment non-obvious code
- Use meaningful variable and method names
- Maintain compatibility with .NET Framework 4.x whenever possible

### Logging

Use the existing logging methods:

```vb
LogDebug(...)
LogNotice(...)
LogWarning(...)
LogSuccess(...)
LogError(...)
```

Do not write directly to the console unless absolutely necessary.

### Security

Never commit:

- API keys
- Personal server URLs
- Credentials
- Private configuration files

Please verify that no sensitive information is included in commits, pull requests, screenshots, or logs.

## Pull Requests

Before submitting a pull request:

- Verify the application compiles successfully
- Test against an Immich instance
- Ensure no existing functionality is broken
- Update documentation if required

Pull requests should contain a clear description of:

- What has changed
- Why the change is useful
- Any potential side effects

## Feature Requests

Feature requests are welcome.

Please describe:

- The use case
- The expected behaviour
- Why the feature would be beneficial to other users

## Project Goals

MZImmichDownloader focuses on:

- Fast downloads
- Reliability
- Simple deployment
- Low dependency footprint
- Easy automation
- Compatibility with Immich

Features that significantly increase complexity or introduce large external dependencies may not be accepted.

## License

By contributing to this project, you agree that your contributions will be licensed under the GNU General Public License v3.0 (GPL-3.0).
``