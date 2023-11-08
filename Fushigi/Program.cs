using Fushigi.util;
using Fushigi.param;
using Fushigi.ui;

FileStream outputStream = new FileStream("output.log", FileMode.Create);
var consoleWriter = new StreamWriter(outputStream);
consoleWriter.AutoFlush = true;
Console.SetOut(consoleWriter);
Console.SetError(consoleWriter);

Console.WriteLine("Starting Fushigi v0.2...");
Console.WriteLine("Loading user settings...");
UserSettings.Load();
Console.WriteLine("Loading parameter database...");
ParamDB.Init();
Console.WriteLine("Loading area parameter loader...");
ParamLoader.Load();

MainWindow window = new MainWindow();

outputStream.Close();