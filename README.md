# Fushigi
An editor for Super Mario Bros. Wonder. Currently WIP.

# How to build
## Dependencies
Make sure you have all of the following installed:
[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[Git](https://git-scm.com/downloads)

## Instructions
Open a CMD or terminal and run the following:
```
git clone https://github.com/shibbo/Fushigi
cd Fushigi
dotnet-8 publish -c Release
```

The build will be create din Fushigi/Fushigi/bin/Release/net8.0/. If you don't want the build to include the dotnet framework, use  `--no-self-contained`. If you want to make a Debug build, use `-c Debug` instead of `-c Release`.

# Credit
Line Awesome icon font provided by [icons8](https://icons8.com/line-awesome)

Ryujinx ASTC decoder by Ac_K. [Ryujinx repo](https://github.com/Ryujinx/Ryujinx)
