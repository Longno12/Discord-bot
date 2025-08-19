using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyAwesomeBot.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

public class Program
{
    private const string ServiceName = "MyAwesomeDiscordBot";

    public static async Task Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (args.Length > 0)
            {
                var command = args[0].ToLower();
                if (command == "install" || command == "uninstall")
                {
                    if (!IsRunningAsAdmin())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("You must run this command as an Administrator to install or uninstall the service.");
                        Console.ResetColor();
                        return;
                    }

                    if (command == "install")
                    {
                        await InstallServiceAsync();
                    }
                    else // uninstall
                    {
                        await UninstallServiceAsync();
                    }
                    return;
                }
            }
        }
        var hostBuilder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<BotService>();
            });
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hostBuilder.UseWindowsService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            hostBuilder.UseSystemd();
        }
        var host = hostBuilder.Build();
        await host.RunAsync();
    }

    private static async Task InstallServiceAsync()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.WriteLine("Could not determine application path.");
            return;
        }
        Console.WriteLine($"Installing service '{ServiceName}'...");
        var process = Process.Start("sc.exe", $"create \"{ServiceName}\" binPath=\"{exePath}\" start=auto");
        await process.WaitForExitAsync();
        if (process.ExitCode == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Service installed successfully!");
            Console.WriteLine($"To start it, run: sc start \"{ServiceName}\"");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Service installation failed with exit code: {process.ExitCode}");
            Console.ResetColor();
        }
    }

    private static async Task UninstallServiceAsync()
    {
        Console.WriteLine($"Uninstalling service '{ServiceName}'...");
        var stopProcess = Process.Start("sc.exe", $"stop \"{ServiceName}\"");
        await stopProcess.WaitForExitAsync();
        var deleteProcess = Process.Start("sc.exe", $"delete \"{ServiceName}\"");
        await deleteProcess.WaitForExitAsync();
        if (deleteProcess.ExitCode == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Service uninstalled successfully!");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Service uninstallation failed. It might not exist. Exit code: {deleteProcess.ExitCode}");
            Console.ResetColor();
        }
    }

    private static bool IsRunningAsAdmin()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}