using FluentAssertions;
using Powerset;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Powerset
{
    public class Recursive
    {
        static void PowerSetR(ISet<int> set, HashSet<ISet<int>> powerset)
        {
            if (set.Count == 0)
                return;

            if (!powerset.Contains(set, new SetContentsComparer()))
                powerset.Add(set);

            foreach (var x in set)
            {
                var without = set.Except(new[] { x });
                PowerSetR(new HashSet<int>(without), powerset);
            }
        }

        public static IEnumerable<ISet<int>> PowerSet(ISet<int> set)
        {
            var power = new HashSet<ISet<int>>();
            PowerSetR(set, power);
            power.Add(new HashSet<int>());
            var ghc = power.Select(x => System.Tuple.Create(x, x.GetHashCode()));
            return power;
        }
        private class SetContentsComparer : IEqualityComparer<ISet<int>>
        {
            public bool Equals(ISet<int> x, ISet<int> y) => x?.SetEquals(y) ?? false;

            public int GetHashCode(ISet<int> obj) => 1;
        }
    }

    public class Iterative
    {
        public static IEnumerable<ISet<T>> PowerSet<T>(ISet<T> set)
        {
            var array = set.ToArray();
            var numElements = array.Length;

            // The number of sets in a powerset is 2^numElements.

            // All numbers between 0 and 2^numElements are used as a bitmask.
            // Each set contains the elements of the input whose indices match
            // the high bits the bitmask.

            long totalCount = 0;
            var numPermutations = System.Math.Pow(2, numElements);
            for (int bitmask = 1; bitmask <= numPermutations; ++bitmask)
            {
                yield return new HashSet<T>(array.Where(IndexIsInBitmask));

                bool IndexIsInBitmask(T _, int i)
                {
                    ++totalCount;
                    return (bitmask & 1 << i) != 0;
                }
            }
        }
    }


    public static class FastSeq
    {
        public static IEnumerable<ISet<T>> PowerSet<T>(ISet<T> set)
        {
            var array = set.ToArray();
            var numElements = array.Length;

            // The number of sets in a powerset is 2^numElements.

            // All numbers between 0 and 2^numElements are used as a bitmask.
            // Each set contains the elements of the input whose indices match
            // the high bits the bitmask.

            long totalCount = 0;

            var seed = new HashSet<T>[1 << numElements];
            seed[0] = new HashSet<T> { };

            var aa = array
                .Select((x,i) => ((x, i)))
                .Aggregate(seed, (acc, x) =>
                {
                    var current = array[x.i];
                    var count = 1 << x.i;

                    for (int j = 0; j < count; j++)
                    {
                        var source = acc[j];
                        var destination = new HashSet<T>(source);
                        destination.Add(current);
                        acc[count + j] = destination;
                        totalCount += source.Count;
                    }
                    
                    return acc;
                });

            return aa;
        }
    }

    public class LoopdeLoop
    {
        public static IEnumerable<ISet<T>> FastPowerSet<T>(ISet<T> iseq)
        {
            var seq = iseq.ToArray();

            long totalcount = 0;

            var powerSet = new T[1 << seq.Length][];
            powerSet[0] = new T[0]; // starting only with empty set
            for (int i = 0; i < seq.Length; i++)
            {
                var cur = seq[i];
                int count = 1 << i; // doubling list each time
                for (int j = 0; j < count; j++)
                {
                    var source = powerSet[j];
                    var destination = powerSet[count + j] = new T[source.Length + 1];
                    for (int q = 0; q < source.Length; q++)
                    {
                        ++totalcount;  // 20,971,520 // 9,437,185
                        destination[q] = source[q];
                    }
                    destination[source.Length] = cur;
                }
            }
            return powerSet.Select(x => new HashSet<T>(x));
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            const int iterations = 5;
            var input = new HashSet<int>(Enumerable.Range(1, 20));


            RunAlgorithm(iterations, input, Iterative.PowerSet, "Iterative");
            RunAlgorithm(iterations, input, LoopdeLoop.FastPowerSet, "FastPowerSet");
            RunAlgorithm(iterations, input, FastSeq.PowerSet, "FastSeq");
        }

        private static void RunAlgorithm(int iterations, HashSet<int> input, Func<ISet<int>, IEnumerable<ISet<int>>> powerSet, string type)
        {
            Console.WriteLine($"{type}: ...");
            var iterative = RunAverage(iterations, input, powerSet);
            Console.WriteLine($"{type}: ... {iterative}ms");
            Console.WriteLine();
        }

        private static double RunAverage(int iterations, HashSet<int> input, Func<ISet<int>, IEnumerable<ISet<int>>> action)
        {
            var watch = new Stopwatch();

            return Enumerable.Range(0, iterations).Average(x =>
            {
                watch.Restart();
                var result = action(input).ToList();
                watch.Stop();
                return watch.ElapsedMilliseconds;
            });
        }
    }
}

namespace Tests
{
    public class TestPowerset
    {
        private IEnumerable<ISet<int>> MakePowerset(ISet<int> set) => FastSeq.PowerSet(set);
        //private IEnumerable<ISet<int>> MakePowerset(ISet<int> set) => Recursive.PowerSet(set);
        //private IEnumerable<ISet<int>> MakePowerset(ISet<int> set) => Iterative.PowerSet(set);

        [Fact]
        public void EmptySet()
        {
            var input = new HashSet<int> { };
            var result = MakePowerset(input);
            result.Should().BeEquivalentTo(new[] { input });
        }

        [Fact]
        public void SingleSet()
        {
            var input = new HashSet<int> { 1 };
            var result = MakePowerset(input);
            result.Should().BeEquivalentTo(new[] { new HashSet<int>(), new HashSet<int> { 1 } });
        }

        [Fact]
        public void DoubleSet()
        {
            var input = new HashSet<int> { 1, 2 };
            var result = MakePowerset(input);
            result.Should().BeEquivalentTo(new[]
            {
                new HashSet<int> { 1, 2 },
                new HashSet<int> { 1 },
                new HashSet<int> { 2 },
                new HashSet<int>(),
            });
        }

        [Fact]
        public void TripleSet()
        {
            var input = new HashSet<int> { 1, 2, 3 };
            var result = MakePowerset(input);
            result.Should().BeEquivalentTo(new[]
            {
                new HashSet<int> { 1, 2, 3 },
                new HashSet<int> { 1, 2 },
                new HashSet<int> { 1, 3 },
                new HashSet<int> { 2, 3 },
                new HashSet<int> { 1 },
                new HashSet<int> { 2 },
                new HashSet<int> { 3 },
                new HashSet<int>(),
            });
        }

        [Fact]
        public void Large()
        {
            const int size = 8;
            var input = new HashSet<int>(Enumerable.Range(1, size));
            var result = MakePowerset(input);
            result.Should().HaveCount((int)System.Math.Pow(2, size));
        }
    }
}