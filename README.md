# Clipboard Sync Monorepo (MVP Bootstrap)

This repository now includes the first executable implementation slice:

- protocol specification package
- crypto primitives package
- sync simulator package
- pairing state manager
- replay guard + sensitive content policy
- MQTT adapter abstraction with in-memory broker

And now includes second-slice implementation artifacts:

- pairing manager with approval and revocation events
- replay guard integrated in sync receive path
- simulator test script for regression checks
- platform app skeleton folders and MVP contracts docs

## Quick start

1. Bootstrap dependencies

```bash
npm run bootstrap
```

2. Build everything

```bash
npm run build
```

3. Validate protocol envelope sample

```bash
npm run validate:protocol
```

4. Run simulator demo

```bash
npm run sim:demo
```

5. Run simulator tests

```bash
npm run sim:test
npm run sim:test:network
```

6. Run app-core demo and tests

```bash
npm run app:demo
npm run app:test
```

7. Start built-in custom sync server (optional)

```bash
npm run server:start
```

The simulator demonstrates:

- message envelope creation
- encrypt + sign + verify + decrypt
- dedup handling
- retry publishing
- replay rejection (nonce + timestamp + sequence)
- policy-based sensitive content blocking
- weak-network retry and recovery verification

App-core demonstrates:

- reusable app orchestration layer
- transport-agnostic sync coordination
- trusted-device public-key verification
- invite-based pairing workflow with manual approval and auto-approve policy
- device revoke/remove and current space reset lifecycle operations
- log redaction to avoid plaintext leakage

Real broker support:

- MQTT real adapter: packages/sync-simulator/src/mqttReal.ts

WebDev + custom server:

- WebDev runtime options are available in app-core options and env template
- Custom sync server requires `/start-service` endpoint and health check support

Release and packaging:

- one-click release helper: `npm run release:all`
- local + cloud helper: `npm run release:all:cloud`
- packaging for iOS/macOS requires macOS + Xcode projects
- packaging status and outputs: docs/release/installable-packages.md
- cloud packaging workflow: docs/release/cloud-packaging.md

Asset drawing tools:

- free options list: docs/design/free-drawing-tools.md

## Docs

- architecture contracts: docs/architecture/mvp-contracts.md
- security baseline: docs/security/threat-model.md
- milestones: docs/architecture/milestones.md
- cross-platform UI status schema: docs/architecture/ui-status-schema.md

## Config and CI

- env template: config/env.example
- CI validation script: scripts/ci/validate.ps1
- GitHub Actions workflow: .github/workflows/ci.yml
- one-shot CI command: npm run ci:validate
