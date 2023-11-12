# Fushigi
An editor for Super Mario Bros. Wonder. Currently WIP.

# Credit
Line Awesome icon font provided by [icons8](https://icons8.com/line-awesome)

Ryujinx ASTC decoder by Ac_K. [Ryujinx repo](https://github.com/Ryujinx/Ryujinx)

# Build
Run this
```
dotnet add Fushigi/Fushigi.csproj package NativeFileDialogSharp --version 0.6.0-alpha
dotnet add Fushigi/Fushigi.csproj package Newtonsoft.Json 
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Core
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Input
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Core
dotnet add Fushigi/Fushigi.csproj package Silk.NET.GLFW
dotnet add Fushigi/Fushigi.csproj package Silk.NET.OpenGL.Extensions.ImGui
dotnet add Fushigi/Fushigi.csproj package StbImageSharp 
dotnet add Fushigi/Fushigi.csproj package ZstdSharp.Port
```

Then run the following depending on which operating system you are using:

### Windows
```
dotnet restore Fushigi/Fushigi.sln
dotnet build Fushigi/Fushigi.sln --no-restore
```

### Linux x64
```
dotnet restore -r linux-x64 Fushigi/Fushigi.sln
dotnet publish Fushigi/Fushigi.sln -r linux-x64 --no-restore
```

### MacOS x64 (Intel)
```
dotnet restore -r osx-x64 Fushigi/Fushigi.sln
dotnet publish Fushigi/Fushigi.sln -r osx-x64 --no-restore
```

### MacOS Silicon (M1, M2, etc.)
```
dotnet restore -r osx-arm64 Fushigi/Fushigi.sln
dotnet publish Fushigi/Fushigi.sln -r osx-arm64 --no-restore
```