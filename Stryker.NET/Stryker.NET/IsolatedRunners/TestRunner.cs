﻿using System;
using System.Diagnostics;
using Stryker.NET.Managers;

namespace Stryker.NET.IsolatedRunner
{
    public class TestRunner : ITestRunner
    {
        private readonly string _rootDirectory;
        private string _tempDirectory;
        private readonly string _command;

        public TestRunner(string rootDir)
        {
            _rootDirectory = rootDir;
            _tempDirectory = $"{_rootDirectory}\\stryker_temp";
            _command = "dotnet";
        }

        public void Test(string workingDirectory)
        {
            var arguments = $"test";
            _tempDirectory = workingDirectory;
            RunCommand(arguments);
        }

        private void RunCommand(string arguments)
        {
            var info = new ProcessStartInfo(_command, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = _tempDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = Process.Start(info);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
