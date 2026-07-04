; ============================================================
; BS_CAD_TOOLS_LOAD.lsp
; Unified loader for BS-CAD-Tools
; Version: v0.3-bridge
; Purpose: Entry file used by BS_OS CAD Bridge
; ============================================================
;
; Usage in AutoCAD:
;   APPLOAD -> select this file
; or from command line:
;   (load "path/to/BS_CAD_TOOLS_LOAD.lsp")
;
; This file is intentionally conservative.
; It does not redefine existing commands directly.
; It provides a stable loader entry for future BS_OS integration.
;
; ============================================================

(defun BS:Loader-Message (msg)
  (princ (strcat "\n[BS-CAD-Tools] " msg))
)

(defun BS:Safe-Load (filePath / result)
  (if (and filePath (findfile filePath))
    (progn
      (setq result (vl-catch-all-apply 'load (list filePath)))
      (if (vl-catch-all-error-p result)
        (BS:Loader-Message (strcat "Failed to load: " filePath))
        (BS:Loader-Message (strcat "Loaded: " filePath))
      )
    )
    (BS:Loader-Message (strcat "Skipped missing file: " filePath))
  )
  (princ)
)

(defun BS:Repo-Root (/ currentFile)
  ; In this basic version, use the current drawing support path behavior.
  ; Future versions may inject the absolute repo path from BS_OS CAD Bridge.
  ""
)

(defun C:BS_TOOLS_STATUS ()
  (BS:Loader-Message "Unified loader is installed.")
  (BS:Loader-Message "Expected commands: BS_LAYER, BS_CHECK, BS_FIX_LAYER, BS_FIX_MISSING, BS_TEMPLATE_CHECK")
  (BS:Loader-Message "If commands are missing, connect actual command files in BS_CAD_TOOLS_LOAD.lsp.")
  (princ)
)

(defun C:BS_RELOAD_TOOLS ()
  (BS:Loader-Message "Reload requested.")
  (BS:Load-All)
  (princ)
)

(defun BS:Load-All ()
  (BS:Loader-Message "Starting unified loader v0.3-bridge...")

  ; ------------------------------------------------------------
  ; Future real load list
  ; Add actual file paths here after repository structure is fixed.
  ; Examples:
  ; (BS:Safe-Load "src/BS_LAYER.lsp")
  ; (BS:Safe-Load "src/BS_CHECK.lsp")
  ; (BS:Safe-Load "src/BS_FIX_LAYER.lsp")
  ; (BS:Safe-Load "src/BS_FIX_MISSING.lsp")
  ; (BS:Safe-Load "src/BS_TEMPLATE_CHECK.lsp")
  ; ------------------------------------------------------------

  (BS:Loader-Message "Loader finished. Run BS_TOOLS_STATUS to check status.")
  (princ)
)

(BS:Load-All)
(princ)
