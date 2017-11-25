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
    public class TestEnvironment : BaseTestEnvironment
    {
        private readonly IReporter _reporter;
        
        private event MutantTestedDelegate MutantTested = delegate { };
        private event AllMutantsTestedDelegate AllMutantsTested = delegate { };

        public Queue<Mutant> Mutants = new Queue<Mutant>();

        public TestEnvironment(
            IMutantTestedHandler mutantTestedHandler,
            IDirectoryManager directoryManager,
            IReporter reporter,
            string rootdir,
            string tempDir,
            string environmentName) : base(directoryManager, rootdir, tempDir, environmentName)
        {
            _reporter = reporter;

            MutantTested += new MutantTestedDelegate(mutantTestedHandler.OnMutantTested);
            MutantTested += new MutantTestedDelegate(_reporter.OnMutantTested);
            AllMutantsTested += new AllMutantsTestedDelegate(_reporter.OnAllMutantsTested);
        }

        public override bool RunTests()
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
                var testSucces = _testRunner.Test();

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

            return true;
        }
    }
}
