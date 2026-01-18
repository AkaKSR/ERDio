#!/usr/bin/env bash
set -euo pipefail

# Publish framework-dependent single-file Release build (no runtime bundled)
# Usage:
#   ./build_singlefile_fd.sh            # uses default RID win-x64
#   RID=win-x86 ./build_singlefile_fd.sh

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
RID="${RID:-all}"

# Allow convenient aliases and multi-builds. Usage examples:
#   RID=win-x64 ./build_singlefile_fd.sh
#   RID=x86 ./build_singlefile_fd.sh        # maps to win-x86
#   RID=all ./build_singlefile_fd.sh        # builds win-x64, win-x86, win-arm64
#   RID=win-x64,win-x86 ./build_singlefile_fd.sh

map_rid() {
  case "$1" in
    x64) echo "win-x64" ;;
    x86) echo "win-x86" ;;
    arm64) echo "win-arm64" ;;
    all) echo "win-x64 win-x86 win-arm64" ;;
    *) echo "$1" ;;
  esac
}

# Build list of RIDs to process
RIDS_TO_BUILD=()
if [[ "$RID" == *","* ]]; then
  IFS=',' read -r -a parts <<< "$RID"
  for p in "${parts[@]}"; do
    for mapped in $(map_rid "$p"); do
      RIDS_TO_BUILD+=("$mapped")
    done
  done
else
  for mapped in $(map_rid "$RID"); do
    RIDS_TO_BUILD+=("$mapped")
  done
fi

for rid in "${RIDS_TO_BUILD[@]}"; do
  OUTDIR="$ROOT_DIR/publish/$rid/singlefile-fd"
  echo "Publishing framework-dependent single-file (RID=$rid) to: $OUTDIR"

  dotnet publish "$ROOT_DIR" \
    -c Release \
    -r "$rid" \
    -p:PublishSingleFile=true \
    -p:SelfContained=false \
    -p:PublishTrimmed=false \
    -p:OutputType=WinExe \
    -o "$OUTDIR"

  echo "Publish complete for $rid. Listing output directory:" 
  ls -la "$OUTDIR" || true
  
  # Rename produced EXE to include RID suffix (e.g. ERDio_win-x64.exe)
  exe_file=$(ls "$OUTDIR"/*.exe 2>/dev/null | head -n 1 || true)
  if [ -n "$exe_file" ] && [ -f "$exe_file" ]; then
    exe_base=$(basename "$exe_file")
    name_no_ext="${exe_base%.*}"
    new_exe_name="${name_no_ext}_${rid}.exe"
    mv -f "$exe_file" "$OUTDIR/$new_exe_name" || true
    echo "Renamed: $exe_base -> $new_exe_name"

    # If a matching PDB exists (same base), rename it similarly
    pdb_file="$OUTDIR/${name_no_ext}.pdb"
    if [ -f "$pdb_file" ]; then
      new_pdb_name="${name_no_ext}_${rid}.pdb"
      mv -f "$pdb_file" "$OUTDIR/$new_pdb_name" || true
      echo "Renamed PDB: $(basename "$pdb_file") -> $new_pdb_name"
    fi
  else
    echo "No EXE found in $OUTDIR to rename."
  fi

  # --- Now create a self-contained single-file build (runtime included) ---
  OUTDIR_SC="$ROOT_DIR/publish/$rid/singlefile-sc"
  echo "Publishing self-contained single-file (RID=$rid) to: $OUTDIR_SC"

  dotnet publish "$ROOT_DIR" \
    -c Release \
    -r "$rid" \
    -p:PublishSingleFile=true \
    -p:SelfContained=true \
    -p:PublishTrimmed=false \
    -p:OutputType=WinExe \
    -o "$OUTDIR_SC"

  echo "Self-contained publish complete for $rid. Listing output directory:" 
  ls -la "$OUTDIR_SC" || true

  exe_file_sc=$(ls "$OUTDIR_SC"/*.exe 2>/dev/null | head -n 1 || true)
  if [ -n "$exe_file_sc" ] && [ -f "$exe_file_sc" ]; then
    exe_base_sc=$(basename "$exe_file_sc")
    name_no_ext_sc="${exe_base_sc%.*}"
    new_exe_name_sc="${name_no_ext_sc}_${rid}_sc.exe"
    mv -f "$exe_file_sc" "$OUTDIR_SC/$new_exe_name_sc" || true
    echo "Renamed: $exe_base_sc -> $new_exe_name_sc"

    pdb_file_sc="$OUTDIR_SC/${name_no_ext_sc}.pdb"
    if [ -f "$pdb_file_sc" ]; then
      new_pdb_name_sc="${name_no_ext_sc}_${rid}_sc.pdb"
      mv -f "$pdb_file_sc" "$OUTDIR_SC/$new_pdb_name_sc" || true
      echo "Renamed PDB: $(basename "$pdb_file_sc") -> $new_pdb_name_sc"
    fi
  else
    echo "No EXE found in $OUTDIR_SC to rename."
  fi
done

echo "Done. To run without showing a console window, launch the EXE from Explorer or use 'start' / 'Start-Process' to detach from an open console." 
