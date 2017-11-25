using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stryker.NET.IsolatedRunner;
using Stryker.NET.Managers;
using Stryker.NET.Reporters;

namespace Stryker.NET
{
    public class TestEnvironment : IDisposable
    {
        private Mutant _mutant;
        private string _tempDir;
        private readonly string _rootdir;
        private readonly IReporter _reporter;
        private readonly ITestRunner _testRunner;
        private readonly IDirectoryManager _directoryManager;

        public TestEnvironment(Mutant mutant,
            IDirectoryManager directoryManager,
            IReporter reporter,
            string rootdir,
            string tempRootDir)
        {
            _mutant = mutant;
            _directoryManager = directoryManager;
            _rootdir = rootdir;
            _tempDir = tempRootDir + "\\" + Guid.NewGuid();
            _testRunner = new TestRunner(_rootdir);
        }

        public void PrepareEnvironment()
        {
            // copy all files to dedicated temp dir
            _directoryManager.CopyRoot(_rootdir, _tempDir);
            // overwrite temp code file with mutated code
            File.WriteAllText(_tempDir + "\\" + _mutant.FilePath, _mutant.MutatedCode, Encoding.Unicode);
        }

        public void RunTest()
        {
            if (_testRunner == null)
            {
                throw new Exception("A test runner was not specified");
            }
           
            // run unit tests
            _testRunner.Test();
            _reporter.Report(_mutant);
        }

        public void Dispose()
        {
            _directoryManager.RemoveDirectory(_tempDir);
        }
    }
}
