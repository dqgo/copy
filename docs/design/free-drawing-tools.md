# Free Drawing APIs and Tools (Icons, Images, Visual Assets)

## Recommended free options

1. Iconify (open icon ecosystem)
- Type: icon aggregation API + libraries
- Cost: free
- Use cases: app icon drafts, action icons, platform-specific symbol sets
- Notes: check icon set license per pack

2. SVG Repo
- Type: free SVG icons/illustrations
- Cost: free
- Use cases: rapid icon sourcing and editing
- Notes: verify license tags before commercial use

3. unDraw
- Type: free customizable illustrations
- Cost: free
- Use cases: onboarding illustrations, empty states
- Notes: color theme customization built in

4. DiceBear
- Type: avatar generation API
- Cost: free / open source
- Use cases: device avatar, profile placeholder
- API example: https://api.dicebear.com/9.x/shapes/svg?seed=device-a

5. Excalidraw + Mermaid + Figma Free
- Type: diagramming and UI asset workflow
- Cost: free tiers available
- Use cases: architecture diagrams, feature flows, icon tracing

## Suggested practical pipeline

1. Draft icons in Figma Free or Excalidraw.
2. Export SVG and optimize via SVGO.
3. Convert app-icon sizes using ImageMagick or platform asset tools.
4. Keep source-of-truth in a single assets folder with license notes.

## License checklist

- Record original source URL for each imported asset.
- Record license type (MIT/CC0/attribution required).
- Avoid mixing incompatible licenses in app store bundles.
