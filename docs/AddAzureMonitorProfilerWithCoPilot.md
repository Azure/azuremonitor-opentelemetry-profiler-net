# Enable the Profiler with optix

The easiest way to enable the Azure Monitor OpenTelemetry Profiler is to let **optix** —
the [Code Optimizations skills for GitHub Copilot CLI](https://github.com/microsoft/code-optimizations-skills) —
do it for you. optix ships a dedicated **enable-profiler** setup skill that detects your
platform and walks you through the changes.

## Why?

- Fast setup (≈2–3 minutes)
- Consistent and low risk — the skill knows the current best practices
- Works for both the Azure Monitor OpenTelemetry distro and the Application Insights SDK

## Prerequisites

- [GitHub Copilot CLI](https://docs.github.com/en/copilot/github-copilot-in-the-cli) installed and authenticated (`copilot auth login`)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and logged in (`az login`)

## Do it

1. Add the optix marketplace and install the skills:

    ```sh
    copilot plugin marketplace add microsoft/code-optimizations-skills
    copilot plugin install optix@microsoft-optix
    ```

    _Tip: to install only the Setup skills, use `copilot plugin install optix-setup@microsoft-optix`._

2. Ask Copilot to enable the profiler:

    ```sh
    copilot "Help me enable the Application Insights Profiler"
    ```

3. Review the changes optix proposes, apply them, and commit.

## Next

Once the profiler is running, optix can also help you act on the data — explore errors,
inspect profiler hot paths, and get code-level optimization recommendations. See the
[optix skills catalog](https://github.com/microsoft/code-optimizations-skills#readme).

Or [return to the Readme](../README.md).
