# Override Command

## Status: DEFERRED

This command is deferred until requirements are clearer.

## Placeholder

```
spire override
```

## Potential Future Scope

- Override specific resource settings (imageName, registry, paths)
- Add/override environment variables for resources
- Repository-scoped overrides vs global overrides

## Notes

When implementing, consider:
- How overrides interact with the simplified global-only configuration
- Whether overrides should be stored separately or merged into main config
- Override precedence rules
