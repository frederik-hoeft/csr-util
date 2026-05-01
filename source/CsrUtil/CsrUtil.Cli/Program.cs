using ConsoleAppFramework;
using CsrUtil.Cli;

ConsoleApp.ConsoleAppBuilder app = ConsoleApp.Create();
app.Add<CsrCommands>();
await app.RunAsync(args);
