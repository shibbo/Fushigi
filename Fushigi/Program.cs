using Fushigi.util;
using Fushigi.param;
using Fushigi.ui;

FileStream outputStream = new FileStream("output.log", FileMode.Create);
var consoleWriter = new StreamWriter(outputStream);
consoleWriter.AutoFlush = true;
Console.SetOut(consoleWriter);
Console.SetError(consoleWriter);

Console.WriteLine("Starting Fushigi v0.5...");
Console.WriteLine("Loading user settings...");
UserSettings.Load();
Console.WriteLine("Loading parameter database...");
ParamDB.Init();
Console.WriteLine("Loading area parameter loader...");
ParamLoader.Load();

Console.WriteLine("Checking for imgui.ini");
if (!Path.Exists("imgui.ini"))
{
  Console.WriteLine("Creating imgui.ini...");
  File.WriteAllText("imgui.ini", File.ReadAllText(Path.Combine("res", "imgui-default.ini")));
  Console.WriteLine("Created!");
};

MainWindow window = new MainWindow();

outputStream.Close();