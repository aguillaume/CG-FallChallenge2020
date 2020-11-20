using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;
using System.IO;

namespace Tests
{
    
    [TestClass()]
    public class GameTests
    {
        private static Random rand = new Random();
        Cast _waterWater = new Cast { Castable = true, Delta = new int[] { 2, 0, 0, 0 }, Id = 1 };
        Cast _waterToEarth = new Cast { Castable = true, Delta = new int[] { -1, 1, 0, 0 }, Id = 2 };
        Cast _earthToFire = new Cast { Castable = true, Delta = new int[] { 0, -1, 1, 0 }, Id = 3 };
        Cast _fireToAir = new Cast { Castable = true, Delta = new int[] { 0, 0, -1, 1 }, Id = 4 };

        Learn _F_WEE = new Learn { Id = 5, TomeIndex = 0, Repetable = false, TaxCount = 0, Delta = new int[] { 1, 2, -1, 0 } };
        Learn _EEE_FFF = new Learn { Id = 6, TomeIndex = 1, Repetable = false, TaxCount = 0, Delta = new int[] { 0, -3, 3, 0 } };
        Learn _WE_A = new Learn { Id = 7, TomeIndex = 2, Repetable = false, TaxCount = 0, Delta = new int[] { -1, -1, 0, 1 } };
        Learn _A_WWEE = new Learn { Id = 8, TomeIndex = 3, Repetable = false, TaxCount = 0, Delta = new int[] { 2, 2, 0, -1 } };
        Learn _EEE_WWFF = new Learn { Id = 9, TomeIndex = 4, Repetable = false, TaxCount = 0, Delta = new int[] { 2, -3, 2, 0 } };
        Learn _F_WWWWE = new Learn { Id = 10, TomeIndex = 5, Repetable = false, TaxCount = 0, Delta = new int[] { 4, 1, -1, 0 } };

        Brew _brewFFFAA = new Brew { Id = rand.Next(), Price = 17, Delta = new int[] { 0, 0, -3, -2 } };

        [TestMethod()]
        public void GetValidStatesTest()
        {
            var g = new Game();
            var state = new State
            {
                ActionTaken = null,
                Casts = new Dictionary<int, Cast>
                {
                    {1,
                    new Cast
                    {
                        Castable = true,
                        Delta = new int[] { 2, 0, 0, 0 },
                        Id = 1
                    } },
                    { 2, new Cast
                    {
                        Castable = true,
                        Delta = new int[] { -1, 1, 0, 0 },
                        Id = 2
                    } }
                },
                Inventory = new int[4] { 3, 0, 0, 0 }
            };
            var a = g.GetValidStates(state);
        }

        [TestMethod()]
        public void GetBestActionTest_RootCanBrew()
        {
            var g = new Game();
            var state = new State
            {
                ActionTaken = null,
                Casts = new Dictionary<int, Cast> { { _waterWater.Id, _waterWater }, { _waterToEarth.Id, _waterToEarth }, { _earthToFire.Id, _earthToFire }, { _fireToAir.Id, _fireToAir } },
                Inventory = new int[4] { 3, 0, 0, 0 }
            };

            var goal = new Brew { Id = rand.Next(), Price = 17, Delta = new int[] { 3, 0, 0, 0 } };


            var t = new TreeNode<State>(state);
            var quickestToBrew = g.GetBestAction(t, goal);
        }

        [TestMethod()]
        public void GetBestActionTest_FourEarth()
        {
            var g = new Game();
            var state = new State
            {
                ActionTaken = null,
                Casts = new Dictionary<int, Cast>{ { _waterWater.Id, _waterWater }, { _waterToEarth.Id, _waterToEarth } , { _earthToFire.Id, _earthToFire }, { _fireToAir .Id, _fireToAir } },
                Tome = new List<Learn> { _F_WEE, _EEE_FFF, _WE_A, _A_WWEE, _EEE_WWFF, _F_WWWWE },
                Inventory = new int[4] { 3, 0, 0, 0 }
            };

            var goal = new Brew { Id = rand.Next(), Price = 17, Delta = new int[] { 0, 4, 0, 0 } };


            var t = new TreeNode<State>(state);
            var quickestToBrew = g.GetBestAction(t, goal);
            Console.WriteLine(quickestToBrew.Count);
            Console.WriteLine(quickestToBrew.Select(q => q.Value.ToString() + $" nodeTally:{q.Tally}" + $"bestTally{q.Parent?.Children.Max(c => c.Tally)}").Aggregate((a, b) => a + "\n" + b));
            Console.WriteLine(t.Depth);
            Console.WriteLine(t.NumberOfChildren);

            //var sb = new StringBuilder();
            //PrintTree(t, "", true, sb);
            //File.WriteAllText(@"tree.txt", sb.ToString());

        }

        [TestMethod()]
        public void EqualityTest()
        {
            var state1 = new State
            {
                ActionTaken = null,
                Casts = new Dictionary<int, Cast> { { _waterWater.Id, _waterWater }, { _waterToEarth.Id, _waterToEarth }, { _earthToFire.Id, _earthToFire }, { _fireToAir.Id, _fireToAir } },
                Tome = new List<Learn> { _F_WEE, _EEE_FFF, _WE_A, _A_WWEE, _EEE_WWFF, _F_WWWWE },
                Inventory = new int[4] { 3, 0, 0, 0 }
            };

            var state2 = state1;
            Assert.AreEqual(state1, state2);

            var state3 = state1.Clone();
            Assert.AreEqual(state3, state2);

            var set = new HashSet<State>();

            Assert.IsTrue(set.Add(state1));
            Assert.IsFalse(set.Add(state2));
            Assert.IsFalse(set.Add(state3));
        }

        public static void PrintTree(TreeNode<State> tree, string indent, bool last, StringBuilder sb)
        {
            
            sb.AppendLine(indent + "+- " + tree.Value);
            indent += last ? "   " : "|  ";

            for (int i = 0; i < tree.Children.Count; i++)
            {
                PrintTree(tree.Children[i], indent, i == tree.Children.Count - 1, sb);
            }

        }
    }
}