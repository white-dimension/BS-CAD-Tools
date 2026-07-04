# BS-CAD-Tools / BS_OS Bridge

## Version

```text
v0.3-bridge
```

## Purpose

This document defines how BS-CAD-Tools connects to BS_OS.

BS_OS expects one stable loader file in this repository:

```text
BS_CAD_TOOLS_LOAD.lsp
```

## Current Loader

Current loader file:

```text
BS_CAD_TOOLS_LOAD.lsp
```

It provides:

- `BS_TOOLS_STATUS`
- `BS_RELOAD_TOOLS`
- `BS:Safe-Load`
- `BS:Load-All`

## Manual Load Test

In AutoCAD:

```text
APPLOAD
```

Select:

```text
BS_CAD_TOOLS_LOAD.lsp
```

Then run:

```text
BS_TOOLS_STATUS
```

Expected output:

```text
[BS-CAD-Tools] Unified loader is installed.
```

## Future Real Load List

After command files are finalized, connect them in `BS:Load-All`:

```lisp
(BS:Safe-Load "src/BS_LAYER.lsp")
(BS:Safe-Load "src/BS_CHECK.lsp")
(BS:Safe-Load "src/BS_FIX_LAYER.lsp")
(BS:Safe-Load "src/BS_FIX_MISSING.lsp")
(BS:Safe-Load "src/BS_TEMPLATE_CHECK.lsp")
```

## Target Commands

The final loader should expose:

```text
BS_LAYER
BS_CHECK
BS_FIX_LAYER
BS_FIX_MISSING
BS_TEMPLATE_CHECK
```

## Relationship

```text
BS_OS
↓
config/cad_bridge.json
↓
BS-CAD-Tools/BS_CAD_TOOLS_LOAD.lsp
↓
AutoCAD commands
```

## Current Status

```text
Status: bridge entry created
Real command loading: pending repository command file mapping
```
