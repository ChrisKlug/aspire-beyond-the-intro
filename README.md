# Demo code for my "Aspire Beyond the Introduction" presentation

This repo contains the source code for the demo used in my
"Aspire Beyond the Introduction" presentation. If you haven't
seen the talk, the code might not be as useful...

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev) (optional, but the repo is set up for `aspire run`)
- A container runtime, such as Docker Desktop or Podman. Neo4j and the "games" container run in it
- `ssh` on your path. The "Penguin Routing" demo uses it to open a public tunnel through [Pinggy](https://pinggy.io)
- The [Dev Tunnels CLI](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started), signed in with `devtunnel user login`
- macOS, if you want the "says what?" commands on the Neo4j resource to work. They call the built-in `say` command

## Running the demo

```bash
aspire run
```

Or run the `AspireDemo.AppHost` project with `dotnet run`. On first start, the dashboard asks for a Neo4j password. It can be anything except `neo4j`.

## Versioning

The latest version of the demo is always on main. Older versions are stored in the `vX.Y` branches, where `X.Y` is the version number.

# Contact

If you have any questions or comments, feel free to reach out on Twitter.
I'm available at [@ZeroKoll](https://www.twitter.com/ZeroKoll).
Or at chris(a)59north.com.