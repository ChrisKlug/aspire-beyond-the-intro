FROM debian:bookworm-slim

# A grab bag of terminal games for the WithTerminal demo. All are event-driven:
# they repaint the handful of cells that changed, on input or on a tick, rather
# than pushing a full frame at 35fps the way doom-ascii does. Measured over an
# idle 10s, vitetris emits ~61 B/s against doom-ascii's ~4.6 MB/s.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        nethack-console \
        vitetris \
        bastet \
        ncurses-term \
    && rm -rf /var/lib/apt/lists/*

# Debian's /etc/profile rebuilds PATH for a login shell and omits /usr/games for
# root, so an ENV PATH here would be discarded. Link into /usr/local/bin, which
# survives, and expose vitetris under the name people will actually guess.
RUN ln -s /usr/games/nethack /usr/local/bin/nethack \
    && ln -s /usr/games/vitetris /usr/local/bin/tetris \
    && ln -s /usr/games/vitetris /usr/local/bin/vitetris \
    && ln -s /usr/games/bastet /usr/local/bin/bastet

# Unlike doom-ascii, all of these read the terminal size and adapt, so there is
# no framebuffer to size and no scaling to tune.
ENV TERM=xterm-256color \
    HOME=/root \
    NETHACKOPTIONS="name:Aspire,color,showexp"

# Greet whoever attaches with the menu, so nothing has to be memorised on stage.
RUN printf '%s\n' \
    'echo' \
    'echo "  nethack   the dungeon crawl"' \
    'echo "  tetris    vitetris, colour + menus"' \
    'echo "  bastet    Tetris whose AI picks your worst piece"' \
    'echo' \
    > /etc/profile.d/games.sh

WORKDIR /root

# Aspire's WithTerminal attaches its PTY to the container's own process, so the
# shell is the terminal program; type a game name once attached.
ENTRYPOINT ["/bin/bash"]
CMD ["-l"]
