#!/usr/bin/env bash
set -euo pipefail

REPO="do4k/gw2cli"
INSTALL_DIR="${GW2CLI_INSTALL_DIR:-$HOME/.local/bin}"
DOTNET_CHANNEL="11.0"
DOTNET_QUALITY="preview"

# ── Detect platform ─────────────────────────────────────────────────────────

OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
  Linux)  OS_NAME="linux" ;;
  Darwin) OS_NAME="osx" ;;
  *)
    echo "Unsupported OS: $OS. Download manually from https://github.com/${REPO}/releases"
    exit 1
    ;;
esac

case "$ARCH" in
  x86_64)        ARCH_NAME="x64" ;;
  arm64|aarch64) ARCH_NAME="arm64" ;;
  *)
    echo "Unsupported architecture: $ARCH"
    exit 1
    ;;
esac

RID="${OS_NAME}-${ARCH_NAME}"

# ── Check / install .NET runtime ─────────────────────────────────────────────

need_dotnet() {
  if ! command -v dotnet &>/dev/null; then return 0; fi
  local major
  major="$(dotnet --version 2>/dev/null | cut -d. -f1 || echo 0)"
  [ "$major" -lt 11 ]
}

if need_dotnet; then
  echo "Installing .NET ${DOTNET_CHANNEL} runtime..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- \
    --channel "$DOTNET_CHANNEL" \
    --quality "$DOTNET_QUALITY" \
    --runtime dotnet \
    --install-dir "$HOME/.dotnet"

  export PATH="$HOME/.dotnet:$PATH"
  export DOTNET_ROOT="$HOME/.dotnet"

  PROFILE=""
  if [ -f "$HOME/.zshrc" ]; then PROFILE="$HOME/.zshrc"
  elif [ -f "$HOME/.bashrc" ]; then PROFILE="$HOME/.bashrc"
  fi

  if [ -n "$PROFILE" ]; then
    if ! grep -q 'DOTNET_ROOT' "$PROFILE"; then
      printf '\nexport DOTNET_ROOT="$HOME/.dotnet"\nexport PATH="$HOME/.dotnet:$PATH"\n' >> "$PROFILE"
      echo "Added .NET to $PROFILE — restart your shell or run: source $PROFILE"
    fi
  fi
else
  echo ".NET $(dotnet --version) already installed."
fi

# ── Download framework-dependent binary ──────────────────────────────────────

echo "Fetching latest release tag..."
LATEST_TAG="$(curl -fsSL "https://api.github.com/repos/${REPO}/releases/latest" \
  | grep '"tag_name"' | head -1 | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')"

if [ -z "$LATEST_TAG" ]; then
  echo "Could not determine latest release. Check https://github.com/${REPO}/releases"
  exit 1
fi

BINARY="gw2-${RID}-fd"
URL="https://github.com/${REPO}/releases/download/${LATEST_TAG}/${BINARY}"

echo "Downloading gw2 ${LATEST_TAG} (framework-dependent, ${RID})..."
mkdir -p "$INSTALL_DIR"
curl -fsSL "$URL" -o "$INSTALL_DIR/gw2"
chmod +x "$INSTALL_DIR/gw2"

# ── Done ─────────────────────────────────────────────────────────────────────

echo ""
echo "Installed: $INSTALL_DIR/gw2"

if ! echo "$PATH" | grep -q "$INSTALL_DIR"; then
  echo "Note: $INSTALL_DIR is not in your PATH."
  echo "Add this to your shell profile:"
  echo "  export PATH=\"$INSTALL_DIR:\$PATH\""
fi

echo ""
echo "Run 'gw2 auth set <api-key>' to get started."
echo "Generate a key at: https://account.arena.net → Applications → New Key"
