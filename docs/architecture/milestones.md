# Milestones and Acceptance

## Milestone 0: Protocol and Security Baseline

Deliverables:
- protocol envelope and validation
- crypto primitives and signing
- replay guard model

Acceptance:
- protocol validation passes
- simulator encrypt/decrypt flow passes

## Milestone 1: MVP Core Engine

Deliverables:
- sync engine with retry/dedup/replay checks
- pairing manager with approval/revocation
- sensitive content policy

Acceptance:
- simulator demo passes
- simulator tests pass
- revocation event increments key version

## Milestone 2: Platform Integrations

Deliverables:
- Windows/Android/iOS/macOS service adapters (skeleton completed)
- secure storage adapters (contract completed, platform binding pending)
- status and trusted-device views (app-core status model completed, native UI binding pending)

Acceptance:
- cross-device sync succeeds on at least 3 real platforms (pending physical device run)
- iOS fallback path works in foreground mode (pending iOS app runtime integration)

## Milestone 3: Stability and Extensions

Deliverables:
- mqtt real broker adapter (implemented in sync-simulator)
- weak network recovery scenarios (implemented in simulator network tests)
- richer content types after policy review (pending)

Acceptance:
- reconnect and idempotency test matrix passes (simulator + app-core test set)
- no plaintext payload in logs (logger redaction tests)

## Completion note

All plan items that are implementable in this repository and environment are now completed.
Remaining items require real platform runtime projects, signing, and device execution outside the current headless TypeScript workspace.
