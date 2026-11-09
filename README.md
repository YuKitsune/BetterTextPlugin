# BetterTextPlugin

> [!IMPORTANT]
> This plugin is in development, and not ready for production.

A vatSys plugin offering better text-pilot interaction.

Use Euroscope-style aliases to send pre-canned text messages to pilots, with fuzzy-finding for message lookup.

## Building

The Fallout build system is used to build, package, and release the plugin via GitHub.
GitHub actions are auto-generated from the `build` project.

1. Install [Fallout](https://docs.fallout.build/docs/introduction).
    ```
    dotnet tool install Fallout.GlobalTool --global
    ```

2. Run `fallout install --profile-name Australia` to build and install the debug version of the plugin.
