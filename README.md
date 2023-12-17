# Fushigi
An editor for Super Mario Bros. Wonder. Currently WIP.

# How to build
## Dependencies
Make sure you have all of the following installed:
[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[Git](https://git-scm.com/downloads)

## Instructions
> [!WARNING]  
> DO NOT HAVE ANY SELF-CREATED FILES NAMED "Fushigi" ON YOUR DESKTOP. THEY WILL BE DELETED.

If all you want to do is run the latest version of the editor do this  
if you want to update the editor just rerun this again

Open a CMD or terminal and run the following:
```
cd Desktop
rm -rf Fushigi
git clone https://github.com/shibbo/Fushigi.git
cd Fushigi/Fushigi
dotnet run -c Release
```

To run it once its been downloaded all you have to do then is this
```
cd Desktop/Fushigi/Fushigi
dotnet run -c Release
```

You will now have either "Fushigi.exe" (if you are on Windows) or "Fushigi" (if you are not on windows) located on your desktop at Fushigi/Fushigi/bin/Debug/net8.0/

# Credit
Line Awesome icon font provided by [icons8](https://icons8.com/line-awesome)

Ryujinx ASTC decoder by Ac_K. [Ryujinx repo](https://github.com/Ryujinx/Ryujinx)

