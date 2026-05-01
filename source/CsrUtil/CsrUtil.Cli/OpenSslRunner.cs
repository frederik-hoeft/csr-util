using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CsrUtil.Cli;

internal static class OpenSslRunner
{
    public static async Task RunAsync(string opensslPath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new(opensslPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = false
        };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Failed to start '{opensslPath}'. Make sure OpenSSL is installed and on PATH, or pass --openssl-path.", ex);
        }

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(static state =>
        {
            Process runningProcess = (Process)state!;
            try
            {
                if (!runningProcess.HasExited)
                {
                    runningProcess.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore races with natural process exit.
            }
        }, process);

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode == 0)
        {
            return;
        }

        StringBuilder message = new();
        message.Append(opensslPath);
        message.Append(' ');
        message.Append(string.Join(' ', arguments));
        message.Append(" failed with exit code ");
        message.Append(process.ExitCode);
        message.Append('.');

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            message.AppendLine();
            message.Append(stderr.Trim());
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            message.AppendLine();
            message.Append(stdout.Trim());
        }

        throw new InvalidOperationException(message.ToString());
    }
}
