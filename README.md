# YACA Plugin Switcher

A lightweight Windows utility for managing and switching between YACA TeamSpeak 3 plugin builds.

> **Status:** 1.0.0 stable baseline — .NET 10 / Windows x64.

## Automated releases

Version tags matching `vMAJOR.MINOR.PATCH` trigger the GitHub Actions release workflow. It builds the self-contained Windows x64 single-file executable, runs static preflight and package validation, generates a SHA-256 checksum, and publishes the ready-to-use ZIP to the GitHub Release.

The workflow can also be started manually from the Actions tab with a release tag.
