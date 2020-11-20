using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Resources;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        Game game = new Game();
        game.Run(args);
    }
}

public class Game
{
    public List<Brew> Brews;
    public Dictionary<int, Cast> Casts;
    public List<Learn> Tome;
    public int[] myInv;
    public Stopwatch mainSw = new Stopwatch();

    public void Run(string[] args)
    {
        string[] inputs;
        int turnCount = 0;
        // game loop
        while (true)
        {
            mainSw.Restart();
            turnCount++;
            Brews = new List<Brew>();
            Casts = new Dictionary<int, Cast>();
            Tome = new List<Learn>();
            int actionCount = int.Parse(Console.ReadLine()); // the number of spells and recipes in play
            //Console.Error.WriteLine(actionCount);

            Action actionIdToTake = new Action();
            for (int i = 0; i < actionCount; i++)
            {
                var lineToRead = Console.ReadLine();
                //Console.Error.WriteLine(lineToRead);
                inputs = lineToRead.Split(' ');
                // Console.Error.WriteLine($"inputs: {string.Join(" ", inputs)}");
                int actionId = int.Parse(inputs[0]); // the unique ID of this spell or recipe
                string actionType = inputs[1]; // in the first league: BREW; later: CAST, OPPONENT_CAST, LEARN, BREW
                int delta0 = int.Parse(inputs[2]); // tier-0 ingredient change
                int delta1 = int.Parse(inputs[3]); // tier-1 ingredient change
                int delta2 = int.Parse(inputs[4]); // tier-2 ingredient change
                int delta3 = int.Parse(inputs[5]); // tier-3 ingredient change
                int price = int.Parse(inputs[6]); // the price in rupees if this is a potion
                int tomeIndex = int.Parse(inputs[7]); // in the first two leagues: always 0; later: the index in the tome if this is a tome spell, equal to the read-ahead tax
                int taxCount = int.Parse(inputs[8]); // in the first two leagues: always 0; later: the amount of taxed tier-0 ingredients you gain from learning this spell
                bool castable = inputs[9] != "0"; // in the first league: always 0; later: 1 if this is a castable player spell
                bool repeatable = inputs[10] != "0"; // for the first two leagues: always 0; later: 1 if this is a repeatable player spell

                if (actionType == "BREW")
                {
                    Brews.Add(new Brew { Id = actionId, Delta = new int[] { delta0 * -1, delta1 * -1, delta2 * -1, delta3 * -1 }, Price = price });
                }

                if (actionType == "CAST")
                {
                    Casts.Add(actionId, new Cast { Id = actionId, Delta = new int[] { delta0, delta1, delta2, delta3 }, Castable = castable });
                }

                if (actionType == "LEARN")
                {
                    var tomeSpell = new Learn
                    {
                        Id = actionId,
                        Delta = new int[] { delta0, delta1, delta2, delta3 },
                        TomeIndex = tomeIndex,
                        TaxCount = taxCount,
                        Repetable = repeatable
                    };
                    Tome.Add(tomeSpell);
                }
            }
            for (int i = 0; i < 2; i++)
            {
                var lineToRead = Console.ReadLine();

                inputs = lineToRead.Split(' ');
                int inv0 = int.Parse(inputs[0]); // tier-0 ingredients in inventory
                int inv1 = int.Parse(inputs[1]);
                int inv2 = int.Parse(inputs[2]);
                int inv3 = int.Parse(inputs[3]);
                int score = int.Parse(inputs[4]); // amount of rupees
                if (i == 0) myInv = new int[] { inv0, inv1, inv2, inv3 };
            }

            Console.Error.WriteLine($"mainSw after all input: {mainSw.ElapsedMilliseconds}");

            // Check if there is something good to learn
            var spellToLearn = GetGreatSpell();
            if (turnCount < 9 && spellToLearn != null)
            {
                // can grab it? 
                if (CanPayLearnTax(spellToLearn, myInv))
                {
                    actionIdToTake = new Action { ActionType = "LEARN", ActionId = spellToLearn.Id };
                    Console.WriteLine($"{actionIdToTake}");
                    continue;
                }
                // Get res to learn
                actionIdToTake = GetResource(spellToLearn, myInv);
                if (actionIdToTake != null)
                {
                    Console.WriteLine($"{actionIdToTake}");
                    continue;
                }
            }


            Brew bestBrew = GetMostProfitableBrew();

            var startState = new State
            {
                Casts = Casts,
                Inventory = myInv,
                Tome = Tome
            };

            var root = new TreeNode<State>(startState);
            Console.Error.WriteLine($"mainSw before GetBestAction: {mainSw.ElapsedMilliseconds}");
            var quickestToBrew = GetBestAction(root, bestBrew);
            Console.Error.WriteLine($"mainSw after GetBestAction: {mainSw.ElapsedMilliseconds}");

            actionIdToTake = quickestToBrew.Last().Value.ActionTaken;

            Console.Error.WriteLine($"bestBrew: {bestBrew}");
            Console.Error.WriteLine($"quickestToBrew.Count {quickestToBrew.Count}");
            Console.Error.WriteLine($"{quickestToBrew.Select(q => q.Value.ToString() + $" nodeTally:{q.Tally}" + $"bestTally{q.Parent?.Children.Max(c => c.Tally)}").Aggregate((a, b) => a + "\n" + b)}");
            Console.Error.WriteLine($"root.Depth {root.Depth}");
            Console.Error.WriteLine($"root.NumberOfChildren {root.NumberOfChildren}");


            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");


            // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
            Console.Error.WriteLine($"mainSw at end: {mainSw.ElapsedMilliseconds}");

            Console.WriteLine($"{actionIdToTake}");
        }
    }

    public Queue<TreeNode<State>> GetBestAction(TreeNode<State> root, Brew goal)
    {
        root.Value.Goal = goal;
        var result = new Queue<TreeNode<State>>();
        var stateCahce = new HashSet<State>();
        var keepGoing = true;
        var nodesToVisit = new Queue<TreeNode<State>>();
        nodesToVisit.Enqueue(root);
        stateCahce.Add(root.Value);
        var bestNodeSoFar = root;

        var sp = new Stopwatch();
        sp.Start();
        while (keepGoing && sp.ElapsedMilliseconds < 40)
        {
            if (!nodesToVisit.TryDequeue(out var currentNode)) break;

            if(CanMakeBrew(goal, currentNode.Value.Inventory))
            {
                currentNode.Value.ActionTaken = new Action { ActionType = "BREW", ActionId = goal.Id }; 
                result.Enqueue(currentNode);
                
                break;
            }

            if (!currentNode.Children.Any())
            {
                var options = GetValidStates(currentNode.Value);
                foreach (var opt in options)
                {
                    opt.Goal = goal;
                    if(currentNode.Parent != null && currentNode.Value.ActionTaken.ActionType == "REST") // trim children if REST
                    {
                        if (opt.ActionTaken.ActionType == "CAST" && // Only keep casts that were exhausted before REST otherwise REST was pointless
                            currentNode.Parent.Value.Casts[opt.ActionTaken.ActionId].Castable == true) continue;
                    }
                    if(stateCahce.Add(opt)) currentNode.AddChildren(opt);
                }
            }

            foreach (var child in currentNode.Children)
            {
                if(CanMakeBrew(goal, child.Value.Inventory))
                {
                    bestNodeSoFar = child;
                    keepGoing = false;
                    break;
                }
                else
                {
                    if (child.Value.Tally > bestNodeSoFar.Value.Tally) bestNodeSoFar = child;
                }
                nodesToVisit.Enqueue(child);
            }
        }

        var currentChild = bestNodeSoFar;
        while (currentChild.Parent != null)
        {
            result.Enqueue(currentChild);
            currentChild = currentChild.Parent;
        }
        sp.Stop();
        Console.Error.WriteLine($"StopWatch: {sp.ElapsedMilliseconds}");

        return result;
    }

    public State[] GetValidStates(State state)
    {
        var result = new List<State>();
        // casts
        foreach (var cast in state.Casts)
        {
            if(IsCastable(cast.Value, state.Inventory))
            {
                var newState = state.Clone(); // Copy item
                newState.Casts[cast.Key].Castable = false; // set spell to exhausted
                for (int i = 0; i < newState.Inventory.Length; i++) // update inventory from cast delta
                {
                    newState.Inventory[i] += cast.Value.Delta[i];
                }
                newState.ActionTaken = new Action { ActionType = "CAST", ActionId = cast.Key }; // set CAST action
                result.Add(newState);
            }
        }
        // rest
        if(state.Casts.Any(c => !c.Value.Castable))
        {
            var restState = state.Clone();
            foreach (var cast in restState.Casts)
            {
                cast.Value.Castable = true;
            }
            restState.ActionTaken = new Action { ActionType = "REST", ActionId = -1 }; // set REST action

            result.Add(restState);
        }

        // Learn
        foreach (var spell in state.Tome)
        {
            if(CanPayLearnTax(spell, state.Inventory))
            {
                var newState = state.Clone();
                var index = newState.Tome.FindIndex(t => t.Id == spell.Id);
                newState.Tome.RemoveAt(index);
                newState.Inventory[0] -= spell.TomeIndex;
                newState.Casts.Add(spell.Id, new Cast { Id = spell.Id, Castable = true, Delta = spell.Delta });
                newState.ActionTaken = new Action { ActionType = "LEARN", ActionId = spell.Id }; // set LEARN action
                result.Add(newState);
            } 
        }

        return result.ToArray();
    }

    public bool IsCastable(Cast cast, int[] inv)
    {
        if (!cast.Castable) return false;
        if (!HaveResForCast(cast, inv)) return false;
        if (!HaveInvSpace(cast, inv)) return false;
        return true;
    }

    public bool CanMakeBrew(Brew brew, int[] inv)
    {
        return
            inv[0] >= brew.Delta[0] &&
            inv[1] >= brew.Delta[1] &&
            inv[2] >= brew.Delta[2] &&
            inv[3] >= brew.Delta[3];
    }

    public Cast FindBestSpell(IEnumerable<Cast> spells)
    {
        return spells.OrderByDescending(s => s.ResEfficency).First();
    }

    public int GetHighestTierMissingRes(Cast cast)
    {
        for (int i = 3; i >= 0; i--)
        {
            if (cast.Delta[i] < 0 && // negative means this is a cost
                ((cast.Delta[i] * -1) - myInv[i]) > 0) // cost is higher than res in inv
                return i;
        }
        throw new Exception($"GetHighestTierMissingRes NO MISSING RES from {cast}. Inv: {Extensions.MyToString(myInv)}");
    }

    public bool HaveResForCast(Cast cast, int[] inv)
    {
        for (var i = 0; i < 4; i++)
        {
            // not a cost of cast
            if (cast.Delta[i] >= 0) continue;
            // if spell cost and I have enough or more of that res the can cast
            if (cast.Delta[i] < 0 && ((cast.Delta[i] * -1) - inv[i]) <= 0)
            {
                continue;
            }
            return false;
        }

        return true;
    }

    public bool HaveInvSpace(Cast cast, int[] inv)
    {
        int maxSpace = 10;
        int freeSpace = maxSpace - inv.Aggregate((a, b) => a + b);
        return freeSpace >= cast.InvSpaceNeeded;
    }

    public Brew GetMostProfitableBrew()
    {
        //var res = Brews.FirstOrDefault(b => CanMakeBrew(b)); // THis means I might not make the best value potion.... Is it worth it? 
        //if (res != null) return res;

        return Brews.OrderByDescending(b => b.Price).ThenByDescending(b => b.Profit).First();
    }

    public Learn GetGreatSpell()
    {
        var spellToLean = Tome
            .Where(t => t.SpellStrength >= 3)?
            .OrderByDescending(t => t.SpellStrength)?
            .FirstOrDefault();
        if (spellToLean != null) Console.Error.WriteLine($"GetGreatSpell spellToLean: {spellToLean}");
        return spellToLean;
    }

    public bool CanPayLearnTax(Learn spell, int[] inv)
    {
        return inv[0] >= spell.TomeIndex;
    }
    public Action GetResource(Learn spell, int[] inv)
    {
        var requiredRes = spell.TomeIndex - myInv[0];
        // can I get enough res by learning for free? 
        if (Tome[0].TaxCount >= requiredRes) return new Action { ActionType = "LEARN", ActionId = Tome[0].Id };
        // can I cast res
        var spells = Casts.Where(c => c.Value.Delta[0] > 0 && c.Value.Castable && HaveResForCast(c.Value, inv) && HaveInvSpace(c.Value, inv));
        if (spells.Count() == 0) return null;
        else if (spells.Count() == 1) return new Action { ActionType = "CAST", ActionId = spells.First().Value.Id };
        else
        {
            var bestSpell = spells.OrderByDescending(s => s.Value.Delta[0]).First();
            return new Action { ActionType = "CAST", ActionId = bestSpell.Value.Id };
        }
    }
}

public class State : IEquatable<State>, ITally
{
    public Dictionary<int, Cast> Casts { get; set; }
    public List<Learn> Tome { get; set; }
    public int[] Inventory { get; set; }
    public Action ActionTaken { get; set; }

    public Brew Goal { get; set; }

    public float Tally
    {
        get
        {
            float invTallyWeight = 0.5F;
            float goalTallyWeight = 0.5F;
            var invTally = Inventory.Sum();
            var goalTally = 0;
            for (int i = 0; i < 4; i++)
            {
                if(Goal.Delta[i] > 0)
                {
                    goalTally += Inventory[i] > Goal.Delta[i] ? Goal.Delta[i] : Inventory[i];
                }
            }
            return invTally * invTallyWeight + goalTally * goalTallyWeight;
        }
    }

    public State Clone()
    {
        var casts = new Dictionary<int, Cast>();
        foreach (var cast in Casts)
        {
            casts.Add(cast.Key, cast.Value.Clone());
        }

        var tome = new List<Learn>();
        foreach (var spell in Tome)
        {
            tome.Add(spell.Clone());
        }

        var inventory = new int[4];
        for (int i = 0; i < Inventory.Length; i++)
        {
            inventory[i] = Inventory[i];
        }

        return new State
        {
            Casts = casts,
            Inventory = inventory,
            Tome = tome
        };
    }

    public override string ToString()
    {
        return $"Action: {ActionTaken}, Inv: [{Inventory[0]},{Inventory[1]},{Inventory[2]},{Inventory[3]}], Casts: [{Casts.Select(a => a.Value.ToString()).Aggregate((a, b) => $"{a}, {b}")}], Tally: {Tally}";
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as State);
    }

    public bool Equals(State other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        for (int i = 0; i < 4; i++)
        {
            if (Inventory[i] != other.Inventory[i]) return false;
        }

        if (Casts.Count != other.Casts.Count) return false;
        if (Tome.Count != other.Tome.Count) return false;

        foreach (var cast in Casts)
        {
            if (other.Casts.TryGetValue(cast.Key, out var otherCast) &&
                otherCast.Castable != cast.Value.Castable) return false;
        }

        foreach (var cast in Tome)
        {
            if (!other.Tome.Contains(cast)) return false;
        }
        
        return true;
    }

    public override int GetHashCode()
    {
        return Inventory.Aggregate((a, b) => a + b).GetHashCode() * 17;
    }

    public static bool operator ==(State lhs, State rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(State lhs, State rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable

}

public class TreeNode<T> where T : ITally
{
    private readonly List<TreeNode<T>> _children = new List<TreeNode<T>>();

    public TreeNode(T value)
    {
        Value = value;
    }

    public TreeNode<T> this[int i]
    {
        get { return _children[i]; }
    }

    public TreeNode<T> Parent { get; private set; }

    public T Value { get; }

    public ReadOnlyCollection<TreeNode<T>> Children
    {
        get { return _children.AsReadOnly(); }
    }

    public TreeNode<T> AddChild(T value)
    {
        var node = new TreeNode<T>(value) { Parent = this };
        _children.Add(node);
        return node;
    }

    public TreeNode<T>[] AddChildren(params T[] values)
    {
        return values.Select(AddChild).ToArray();
    }

    public bool RemoveChild(TreeNode<T> node)
    {
        return _children.Remove(node);
    }

    public void Traverse(Action<T> action)
    {
        action(Value);
        foreach (var child in _children)
            child.Traverse(action);
    }

    public IEnumerable<T> Flatten()
    {
        return new[] { Value }.Concat(_children.SelectMany(x => x.Flatten()));
    }

    public int NumberOfChildren
    {
        get
        {
            var tot = (Parent == null) ? 0 : 1;
            foreach (var child in Children)
            {
                tot += child.NumberOfChildren;
            }

            return tot;
        }
    }

    public int Depth
    {
        get
        {
            return Children.Any() ? Children.Select(c => c.Depth).Max() + 1 : 1;
        }
    }

    public float Tally
    {
        get
        {
            return Children.Any() ? (Children.Sum(c => c.Tally) / Children.Count) + Value.Tally : Value.Tally;
        }
    }
}

public interface ITally
{
    public float Tally { get; }
}

public class BaseToString
{
    private PropertyInfo[] _PropertyInfos = null;

    public override string ToString()
    {
        if (_PropertyInfos == null)
            _PropertyInfos = this.GetType().GetProperties();

        var sb = new StringBuilder();

        foreach (var info in _PropertyInfos)
        {
            var value = info.GetValue(this, null) ?? "(null)";
            if (value.GetType() == typeof(int[]))
            {
                sb.AppendLine(info.Name + ": " + ((int[])value).MyToString());
            }
            else
            {
                sb.AppendLine(info.Name + ": " + value.ToString());
            }
        }

        return sb.ToString();
    }
}

public class BaseItem : BaseToString
{
    public int Id { get; set; }
    public int[] Delta { get; set; }


}

public class Brew : BaseItem
{
    public int Price { get; set; }
    public int Cost
    {
        get
        {
            return Delta[0] + Delta[1] * 2 + Delta[2] * 3 + Delta[3] * 4;
        }
    }
    public float Profit
    {
        get
        {
            return Price / (float)Cost;
        }
    }
}

public class Cast : BaseItem, IEquatable<Cast>
{
    public bool Castable { get; set; }
    public int ResEfficency
    {
        get
        {
            return Delta[0] + Delta[1] * 2 + Delta[2] * 3 + Delta[3] * 4;
        }
    }

    public int InvSpaceNeeded
    {
        get
        {
            return Delta.Aggregate((a, b) => a + b);
        }
    }

    internal Cast Clone()
    {
        int[] delta = new int[] { Delta[0], Delta[1], Delta[2], Delta[3] };

        return new Cast
        {
            Castable = Castable,
            Delta = delta,
            Id = Id
        };
    }

    public override string ToString()
    {
        return $"{Id} E:{Castable}";
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Cast);
    }

    public bool Equals(Cast other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        if (Castable != other.Castable) return false;

        if (Id != other.Id) return false;
        return true;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() * 17;
    }

    public static bool operator ==(Cast lhs, Cast rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Cast lhs, Cast rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable
}

public class Learn : BaseItem, IEquatable<Learn>
{
    public int TomeIndex { get; set; }
    public int TaxCount { get; set; }
    public bool Repetable { get; set; }
    public int SpellStrength
    {
        get
        {
            return Delta[0] + Delta[1] * 2 + Delta[2] * 3 + Delta[3] * 4;
        }
    }

    internal Learn Clone()
    {
        int[] delta = new int[] { Delta[0], Delta[1], Delta[2], Delta[3] };

        return new Learn
        {
            Id = Id,
            Delta = delta,
            TomeIndex = TomeIndex,
            TaxCount = TaxCount,
            Repetable = Repetable
        };
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Learn);
    }

    public bool Equals(Learn other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        if (TomeIndex != other.TomeIndex) return false;
        if (TaxCount != other.TaxCount) return false;

        if (Id != other.Id) return false;
        return true;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() * 17;
    }

    public static bool operator ==(Learn lhs, Learn rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Learn lhs, Learn rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable
}

public static class Extensions
{
    public static string MyToString(this int[] array)
    {
        return string.Join(" ", array);
    }
}

public class Action : BaseToString
{
    public string ActionType { get; set; }
    public int ActionId { get; set; }

    public override string ToString()
    {
        return $"{ActionType} {ActionId}";
    }
}
