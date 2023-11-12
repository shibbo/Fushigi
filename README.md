# Fushigi
An editor for Super Mario Bros. Wonder. Currently WIP.

# Credit
Line Awesome icon font provided by [icons8](https://icons8.com/line-awesome)

Ryujinx ASTC decoder by Ac_K. [Ryujinx repo](https://github.com/Ryujinx/Ryujinx)

# How to build
## Dependencies
Make sure you have all of the following installed:
[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[Git](https://git-scm.com/downloads)

## Instructions
> [!WARNING]  
> DO NOT HAVE ANY SELF-CREATED FILES NAMED "Fushigi" ON YOUR DESKTOP. THEY WILL BE DELETED.
Open a CMD or terminal and run the following:
```
cd Desktop
rm -rf Fushigi
git clone Fushigi
cd Fushigi
dotnet add Fushigi/Fushigi.csproj package NativeFileDialogSharp --version 0.6.0-alpha
dotnet add Fushigi/Fushigi.csproj package Newtonsoft.Json 
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Core
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Input
dotnet add Fushigi/Fushigi.csproj package Silk.NET.Core
dotnet add Fushigi/Fushigi.csproj package Silk.NET.GLFW
dotnet add Fushigi/Fushigi.csproj package Silk.NET.OpenGL.Extensions.ImGui
dotnet add Fushigi/Fushigi.csproj package StbImageSharp 
dotnet add Fushigi/Fushigi.csproj package ZstdSharp.Port
dotnet restore Fushigi/Fushigi.sln
dotnet build Fushigi/Fushigi.sln --no-restore
```

You will now have either "Fushigi.exe" (if you are on Windows) or "Fushigi" (if you are not on windows) located on your desktop at Fushigi/Fushigi/bin/Debug/net8.0/