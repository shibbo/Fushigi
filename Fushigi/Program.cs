using Fushigi.util;
using Fushigi.param;
using Fushigi.ui;

FileStream outputStream = new FileStream("output.log", FileMode.Create);
var consoleWriter = new StreamWriter(outputStream);
consoleWriter.AutoFlush = true;
Console.SetOut(consoleWriter);
Console.SetError(consoleWriter);

AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

Console.WriteLine("Starting Fushigi v0.5...");
Console.WriteLine("Loading user settings...");
UserSettings.Load();
Console.WriteLine("Loading parameter database...");
ParamDB.Init();
Console.WriteLine("Loading area parameter loader...");
ParamLoader.Load();

MainWindow window = new MainWindow();

outputStream.Close();

void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
{
    Exception? ex = e.ExceptionObject as Exception;
    if (ex != null)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
    }

    Environment.Exit(1);
}
