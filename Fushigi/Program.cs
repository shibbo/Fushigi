using Fushigi.util;
using Fushigi.param;
using Fushigi.ui;

Console.WriteLine("Starting Fushigi v0.2...");
Console.WriteLine("Loading user settings...");
UserSettings.Load();
Console.WriteLine("Loading parameter database...");
ParamDB.Init();
Console.WriteLine("Loading area parameter loader...");
ParamLoader.Load();

MainWindow window = new MainWindow();

Console.WriteLine("Press the ENTER key to exit.");
Console.Read();