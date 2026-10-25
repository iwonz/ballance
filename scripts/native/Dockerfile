FROM ubuntu@sha256:0a9887931b0e38af2e3487ed39e29436b0535c4f3f52e57745baaf79d1898956

RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    ca-certificates clang-14 git python3 g++-mingw-w64-x86-64-posix wine64 \
    && rm -rf /var/lib/apt/lists/*

ENV CXX=clang++-14
ENV WINEDEBUG=-all
ENV WINEPREFIX=/tmp/ballance-wine
WORKDIR /repo
