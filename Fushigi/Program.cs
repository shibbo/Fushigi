using Fushigi.util;
using Fushigi.param;
using Fushigi.ui;

Console.WriteLine("Starting Fushigi...");
Console.WriteLine("Loading user settings...");
UserSettings.Load();
Console.WriteLine("Loading parameter database...");
ParamDB.Init();
Console.WriteLine("Loading area parameter loader...");
ParamLoader.Load();

MainWindow window = new MainWindow();
Console.Read();