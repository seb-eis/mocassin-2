# MOCASSIN development image
#
# Purpose:
# - Reproducible base image for a MOCASSIN coding agent running under the
#   opencode container system.
# - The same toolchain can also be installed on a normal workstation or
#   container for direct development; opencode is not required to use the
#   tools themselves.
#
# Source:     https://github.com/seb-eis/mocassin-2
# Base image: opencode:base, built from
#             https://github.com/seb-eis/opencode-container
#             (variants/base.containerfile; run `make base` in that checkout).
#
# This image does not modify the opencode-container repository.
#
# Build the base image from the opencode-container repository:
#
#   git clone https://github.com/seb-eis/opencode-container
#   cd opencode-container
#   make base
#
# Then build this image. It has no COPY/ADD step and no build-context
# requirement, so the build works from any directory and with any
# context, including the opencode-container Makefile. For example, from
# the MOCASSIN repository root:
#
#   podman build -f docs/opencode/mocassin.containerfile -t opencode:mocassin .
#
# The image installs the full development toolchain because opencode:base
# provides Node and the common toolbox, but not .NET, Rust, Python, GUI
# libraries, or documentation tools.
#
# Optional release/CI tools (actionlint, gh, syft, grype, cosign, pandoc,
# mermaid-cli) are intentionally not installed. Add them when Step 31 starts.

ARG BASE_IMAGE=opencode:base
FROM ${BASE_IMAGE}

LABEL org.opencontainers.image.title="MOCASSIN development image"
LABEL org.opencontainers.image.description="Coding-agent and direct-development toolchain for MOCASSIN"
LABEL org.opencontainers.image.source="https://github.com/seb-eis/mocassin-2"
LABEL org.opencontainers.image.base.name="https://github.com/seb-eis/opencode-container"

# --------------------------------------------------
# System packages: build tools, diagnostics, Python,
# .NET runtime dependencies, and Linux GUI/headless libs.
# --------------------------------------------------
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        build-essential gdb pkg-config strace ltrace \
        cmake ninja-build \
        python3 python3-pip python3-venv \
        clang lld llvm \
        linux-perf valgrind \
        libssl-dev zlib1g-dev \
        libicu76 libssl3t64 libgssapi-krb5-2 libunwind8 zlib1g \
        fontconfig fonts-dejavu-core fonts-liberation \
        libfontconfig1 libfreetype6 libharfbuzz0b libpng16-16 libglib2.0-0 \
        libx11-6 libx11-xcb1 libxcb1 libxext6 libxrender1 libxfixes3 \
        libxrandr2 libxi6 libxcursor1 libxinerama1 libxcomposite1 \
        libxdamage1 libxtst6 libxss1 libxkbcommon0 libxkbcommon-x11-0 \
        libxxf86vm1 libxshmfence1 libice6 libsm6 \
        libgl1 libegl1 libgles2 libglx-mesa0 libegl-mesa0 \
        libgl1-mesa-dri libgbm1 libdrm2 mesa-utils \
        xvfb xauth x11-utils \
        libdbus-1-3 libgtk-3-0 xdg-utils \
        libwayland-client0 libwayland-cursor0 libwayland-egl1 \
        libvulkan1 vulkan-tools mesa-vulkan-drivers \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# --------------------------------------------------
# User-local toolchain directories. Create every path
# that is later chowned to the runtime user.
# --------------------------------------------------
RUN mkdir -p \
        /home/node/.cargo \
        /home/node/.rustup \
        /home/node/.dotnet \
        /home/node/.venvs/mocassin \
    && chown -R node:node \
        /home/node/.cargo \
        /home/node/.rustup \
        /home/node/.dotnet \
        /home/node/.venvs

USER node

ENV CARGO_HOME=/home/node/.cargo \
    RUSTUP_HOME=/home/node/.rustup \
    DOTNET_ROOT=/home/node/.dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH="/home/node/.venvs/mocassin/bin:/home/node/.cargo/bin:/home/node/.dotnet:/home/node/.dotnet/tools:${PATH}"

# --------------------------------------------------
# Rust: stable toolchain used for production, plus
# nightly components for fuzzing and Miri, and the
# LLVM tools used by cargo-llvm-cov.
# --------------------------------------------------
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs \
        | sh -s -- -y --default-toolchain 1.99.1 --profile default \
    && rustup component add rustfmt clippy llvm-tools-preview rust-src \
    && rustup toolchain install nightly --profile minimal \
    && rustup component add --toolchain nightly llvm-tools-preview rust-src miri

# --------------------------------------------------
# .NET SDK: user-local, pinned patch version.
# The image installs 11.0.100 and nothing else, so
# `dotnet --version` resolves the same pin as the
# repository's global.json without copying it in.
# --------------------------------------------------
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh \
        --version 11.0.100 \
        --install-dir "$DOTNET_ROOT" \
        --no-path \
    && rm -f /tmp/dotnet-install.sh \
    && dotnet --version

# --------------------------------------------------
# .NET documentation and coverage tools.
# dotnet tool install writes shims to ~/.dotnet/tools,
# which is already on PATH above.
# --------------------------------------------------
RUN dotnet tool install --global docfx --version 2.81.0 \
    && dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.6.0

# --------------------------------------------------
# Python: one venv for docs, tests, packaging, and lint.
# Debian marks system Python as externally managed, so
# do not use pip install --user or --break-system-packages.
# --------------------------------------------------
RUN python3 -m venv "$HOME/.venvs/mocassin" \
    && "$HOME/.venvs/mocassin/bin/pip" install --no-cache-dir \
        'zensical==0.1.1' \
        'mkdocstrings[python]==1.0.8' \
        'pytest==9.2.0' \
        'pytest-cov==7.1.0' \
        'hypothesis==6.170.14' \
        'numpy==2.5.4' \
        'msgpack==1.2.4' \
        'build==1.6.1' \
        'twine==7.0.0' \
        'mypy==2.4.1' \
        'ruff==0.17.3'

# --------------------------------------------------
# Rust validation, coverage, fuzzing, and profiling
# helpers. cargo-binstall prefers prebuilt binaries and
# falls back to cargo install when needed.
#
# On Linux, fuzz builds must use a glibc target:
#   cargo +nightly fuzz build --target x86_64-unknown-linux-gnu
# The cargo-fuzz default musl target is incompatible with ASAN.
#
# perf is installed but needs PERFMON/host sysctl access. In the
# default opencode container, use Valgrind/Callgrind instead.
# --------------------------------------------------
RUN cargo install cargo-binstall --version 1.26.2 --locked \
    && install_tool() { \
        cargo binstall --no-confirm "$1" \
        || cargo install "${1%@*}" --version "${1#*@}" --locked; \
    } \
    && install_tool cargo-deny@0.20.2 \
    && install_tool cargo-fuzz@0.13.2 \
    && install_tool cargo-llvm-cov@0.9.1 \
    && install_tool cargo-insta@1.49.0 \
    && install_tool cargo-nextest@0.9.150 \
    && install_tool flamegraph@0.6.15

# --------------------------------------------------
# Verification as the runtime user.
# Self-contained: no repository file is copied in, so
# the checks prove the image contents on their own.
# --------------------------------------------------
RUN set -eux; \
    dotnet --version; \
    dotnet --list-sdks; \
    test "$(dotnet --list-sdks | wc -l)" -eq 1; \
    dotnet --list-sdks | grep -qE '^11\.0\.100 \['; \
    ! dotnet --list-sdks | grep -qE '^10\.'; \
    test "$(command -v dotnet)" = "$DOTNET_ROOT/dotnet"; \
    test -x "$HOME/.dotnet/tools/docfx"; \
    rustc --version; \
    cargo --version; \
    cargo fmt --version; \
    cargo clippy --version; \
    cargo deny --version; \
    cargo fuzz --version; \
    cargo llvm-cov --version; \
    cargo nextest --version; \
    cargo insta --version; \
    cargo flamegraph --version; \
    cargo +nightly miri --version; \
    docfx --version; \
    zensical --version; \
    python -c 'import mkdocstrings, mkdocstrings_handlers.python'; \
    pytest --version; \
    ruff --version; \
    python -m build --version; \
    fc-cache -f; \
    tmp="$(mktemp -d)"; \
    trap 'rm -rf "$tmp"' EXIT; \
    LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a glxinfo -B >/dev/null

# Guard: root must not own anything in the runtime user's home.
RUN test -z "$(find /home/node -user root -print -quit)"

WORKDIR /workspace

ENTRYPOINT ["opencode"]
