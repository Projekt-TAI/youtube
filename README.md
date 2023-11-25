# TAI Backend

## Pre-requisites

- [Bento4 mp4 tools](https://www.bento4.com/), extract them under `tools/Bento4` with default `appsetings.json`, or configure the path there.
- Your facebook application ID and secret:
```sh
dotnet user-secrets init
dotnet user-secrets set "Authentication:Facebook:AppId" "XXXX"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "XXXX"
```

## Running

Application can be run with `dotnet run --urls=https://localhost:5001`.
