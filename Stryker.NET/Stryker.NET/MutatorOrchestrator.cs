﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.NET.Mutators;
using System.Collections.Generic;
using System;

namespace Stryker.NET
{
    public class MutatorOrchestrator
    {

        public List<IMutator> Mutators { get; private set; }

        public MutatorOrchestrator()
        {
            Mutators = new List<IMutator> { new BinaryExpressionMutator() };
        }

        public IEnumerable<Mutant> Mutate(IEnumerable<string> files)
        {
            var mutants = new List<Mutant>();

            foreach (var file in files)
            {
                var root = GetSyntaxTreeRootFromFile(file);
                var nodes = new List<SyntaxNode>();
                CollectNodes(root, nodes);

                foreach (var node in nodes)
                {
                    foreach (var mutator in Mutators)
                    {
                        var mutatedNodes = mutator.ApplyMutations(node);
                        var newMutants = GenerateMutants(mutatedNodes, mutator.Name, root, node, file);
                        mutants.AddRange(newMutants);
                    }
                }
            }

            return mutants;
        }

        private IEnumerable<Mutant> GenerateMutants(IEnumerable<SyntaxNode> mutatedNodes, string mutatorName, CompilationUnitSyntax root, SyntaxNode node, string file)
        {
            var mutants = new List<Mutant>();
            if (mutatedNodes != null)
            {
                foreach (var mutatedNode in mutatedNodes)
                {
                    var mutatedCode = root.ReplaceNode(node, mutatedNode).ToFullString();
                    mutants.Add(new Mutant(mutatorName, file, mutatedCode, node.ToFullString(), mutatedNode.ToFullString(), node.Span));
                }
            }
            return mutants;
        }

        private CompilationUnitSyntax GetSyntaxTreeRootFromFile(string file)
        {
            var originalCode = System.IO.File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(originalCode);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            return root;
        }

        private void CollectNodes(SyntaxNode node, IList<SyntaxNode> nodes)
        {
            nodes.Add(node);
            foreach (var child in node.ChildNodes())
            {
                CollectNodes(child, nodes);
            }
        }
    }
}