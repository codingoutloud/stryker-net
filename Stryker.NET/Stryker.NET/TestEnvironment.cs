using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stryker.NET.Core;
using Stryker.NET.Core.Event;
using Stryker.NET.IsolatedRunner;
using Stryker.NET.Managers;
using Stryker.NET.Reporters;

namespace Stryker.NET
{
    public class TestEnvironment : IDisposable
    {
        private string _tempDir;
        private readonly string _rootdir;
        private readonly string _environmentName;
        private readonly IReporter _reporter;
        private readonly ITestRunner _testRunner;
        private readonly IDirectoryManager _directoryManager;
        
        private event MutantTestedDelegate MutantTested = delegate { };
        private event AllMutantsTestedDelegate AllMutantsTested = delegate { };

        public Queue<Mutant> Mutants = new Queue<Mutant>();

        public TestEnvironment(
            IMutantTestedHandler mutantTestedHandler,
            IDirectoryManager directoryManager,
            IReporter reporter,
            string rootdir,
            string tempDir,
            string environmentName)
        {
            _directoryManager = directoryManager;
            _rootdir = rootdir;
            _tempDir = rootdir + "\\" + tempDir + "\\" + environmentName;
            _environmentName = environmentName;
            _reporter = reporter;
            _testRunner = new TestRunner(_tempDir);

            MutantTested += new MutantTestedDelegate(mutantTestedHandler.OnMutantTested);
            MutantTested += new MutantTestedDelegate(_reporter.OnMutantTested);
            AllMutantsTested += new AllMutantsTestedDelegate(_reporter.OnAllMutantsTested);
        }

        public void PrepareEnvironment()
        {
            // copy all files to dedicated temp dir
            _directoryManager.CopyRoot(_rootdir, _tempDir);
        }

        public void RunTests()
        {
            if (_testRunner == null)
            {
                throw new Exception("A test runner was not specified");
            }
            var mutatorOrchestrator = new MutatorOrchestrator();
            
            Mutant currentMutant = null;
            while (Mutants.Count > 0)
            {
                //Console.WriteLine($"{_environmentName}: New testrun started");
                currentMutant = Mutants.Dequeue();
                // overwrite temp code file with mutated code
                File.WriteAllText(_tempDir + "\\" + _environmentName + "\\" + currentMutant.FilePath, currentMutant.MutatedCode, Encoding.Unicode);

                // run unit tests
                _testRunner.Test();

                // create and store mutant result
                var status = MutantStatus.Killed;
                var mutantResult = new MutantResult(
                    currentMutant.FilePath,
                    currentMutant.MutatorName,
                    status,
                    currentMutant.MutatedCode,
                    currentMutant.OriginalFragment,
                    currentMutant.MutatedFragment,
                    null,
                    new Location(
                        new Position(currentMutant.LinePosition.Line, currentMutant.LinePosition.Character),
                        new Position(currentMutant.LinePosition.Line, currentMutant.LinePosition.Character) //TODO: get correct mutated line position
                        ),
                    new Range<int>(0, 1)); //TODO: get correct mutated range

                // notify 'mutant tested' observers
                MutantTested(mutantResult);

                // restore file to original state so the next test will have a resetted environment
                var originalCode = mutatorOrchestrator.Restore(currentMutant);
                File.WriteAllText(_tempDir + "\\" + _environmentName + "\\" + currentMutant.FilePath, originalCode, Encoding.Unicode);
            }
        }

        public void Dispose()
        {
            _directoryManager.RemoveDirectory(_tempDir);
        }
    }
}
