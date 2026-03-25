# Threat Model (MVP)

## Assets

- Clipboard plaintext
- Workspace key
- Device private keys
- Device trust list

## Threats

- Unauthorized device joins
- Replay attack
- Topic guessing/enumeration
- Broker-side traffic observation
- Sensitive data accidental sync
- Lost device still trusted

## Controls

- Pairing request signatures
- Envelope signatures and AEAD encryption
- Replay guard: timestamp window + nonce cache + sequence monotonic check
- Workspace-scoped topic with salt
- Sensitive-content blocking policy (OTP/card/ID/password hints)
- Device revocation + key version bump

## Logging

- Never log plaintext clipboard
- Never log workspace key or private key material
- Keep only envelope metadata and result codes
