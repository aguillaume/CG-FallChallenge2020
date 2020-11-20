using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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

class Game
{
    public List<Brew> Brews;
    public List<Cast> Casts;
    public List<Learn> Tome;
    public int[] myInv;

    public void Run(string[] args)
    {
        string[] inputs;
        int turnCount = 0;
        // game loop
        while (true)
        {
            turnCount++;
            Brews = new List<Brew>();
            Casts = new List<Cast>();
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
                    Casts.Add(new Cast { Id = actionId, Delta = new int[] { delta0, delta1, delta2, delta3 }, Castable = castable });
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
                //Console.Error.WriteLine(lineToRead);

                inputs = lineToRead.Split(' ');
                int inv0 = int.Parse(inputs[0]); // tier-0 ingredients in inventory
                int inv1 = int.Parse(inputs[1]);
                int inv2 = int.Parse(inputs[2]);
                int inv3 = int.Parse(inputs[3]);
                int score = int.Parse(inputs[4]); // amount of rupees
                if (i == 0) myInv = new int[] { inv0, inv1, inv2, inv3 };
            }

            Brew bestBrew = GetMostProfitableBrew();

            Console.Error.WriteLine($"bestBrew: {bestBrew}");

            if (CanMakeBrew(bestBrew))
            {
                actionIdToTake = new Action { ActionType = "BREW", ActionId = bestBrew.Id };
            }
            else
            {
                // Check if there is something good to learn
                var spellToLearn = GetGreatSpell();
                if (spellToLearn != null)
                {
                    // can grab it? 
                    if (CanPayLearnTax(spellToLearn))
                    {
                        actionIdToTake = new Action { ActionType = "LEARN", ActionId = spellToLearn.Id };
                        Console.WriteLine($"{actionIdToTake}");
                        continue;
                    }
                    // Get res to learn
                    actionIdToTake = GetResource(spellToLearn);
                    if (actionIdToTake != null)
                    {
                        Console.WriteLine($"{actionIdToTake}");
                        continue;
                    }
                }

                // Try to get Res for the best brew
                var bestBrewAction = GetResource(bestBrew);
                Console.Error.WriteLine($"bestBrewAction: {bestBrewAction}");
                if (bestBrewAction != null)
                {
                    actionIdToTake = bestBrewAction;
                }
                else
                {
                    actionIdToTake = new Action { ActionType = "REST" };
                }
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");


            // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
            Console.WriteLine($"{actionIdToTake}");
        }
    }

    public bool CanMakeBrew(Brew brew)
    {
        return
            myInv[0] >= brew.Delta[0] &&
            myInv[1] >= brew.Delta[1] &&
            myInv[2] >= brew.Delta[2] &&
            myInv[3] >= brew.Delta[3];
    }

    public Action GetResource(Brew brew)
    {
        Console.Error.WriteLine($"GetResource Brew");
        var requiredRes = new int[4];
        requiredRes[0] = brew.Delta[0] - myInv[0];
        requiredRes[1] = brew.Delta[1] - myInv[1];
        requiredRes[2] = brew.Delta[2] - myInv[2];
        requiredRes[3] = brew.Delta[3] - myInv[3];

        for (var i = 3; i >= 0; i--)
        {
            // already have enough of this res
            if (requiredRes[i] <= 0) continue;
            // find spell that produces this res and can be cast
            var action = GetResource(i, true);
            if (action != null) return action;
        }
        // no spell to cast
        return null;
    }

    private HashSet<Cast> visited;

    // 0 >> Tier-0 // 1 >> Tier-1 // 2 >> Tier-2 // 3 >> Tier-3
    public Action GetResource(int res, bool start = false)
    {

        if (start) visited = new HashSet<Cast>();
        // Find spell that produce this res and are not exhausted and has not been visited in a spell chain.
        // I want to maximize the number of casts before having to rest. Each turn resting is a turn lost. 
        var spells = Casts.Where(c => c.Delta[res] > 0 && c.Castable && !visited.Contains(c));
        if (spells.Count() == 0) // I have no spell to produce this res. WHAT!?
        {
            Console.Error.WriteLine($"GetResource Res. No spell to produce this res {res} ?!");
            return null;
        }

        // Some can be cast now
        // -- filter by res efficiency
        var castableNow = spells.Where(s => HaveResForCast(s));
        if (castableNow.Any())
        {

            var castableAndHaveSpace = castableNow.Where(s => HaveInvSpace(s));
            if (castableAndHaveSpace.Any())
            {

                var selectedCast = FindBestSpell(castableAndHaveSpace);
                return new Action { ActionType = "CAST", ActionId = selectedCast.Id };
            }

            var unblockInvCast = Casts
                .Where(c => c.Castable && !visited.Contains(c) && HaveResForCast(c) && HaveInvSpace(c))?
                .OrderByDescending(c => c.InvSpaceNeeded)?
                .FirstOrDefault();

            if (unblockInvCast == null) return null;
            return new Action { ActionType = "CAST", ActionId = unblockInvCast.Id };
        }

        // Some might be missing res
        // -- spend x turns to get the required res
        else
        {
            var selectedCast = FindBestSpell(spells);
            var missingRes = GetHighestTierMissingRes(selectedCast);
            visited.Add(selectedCast);
            return GetResource(missingRes);
        }
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

    public bool HaveResForCast(Cast cast)
    {
        for (var i = 0; i < 4; i++)
        {
            // not a cost or production
            if (cast.Delta[i] >= 0) continue;
            // if spell cost and I have enough or more of that res the can cast
            if (cast.Delta[i] < 0 && ((cast.Delta[i] * -1) - myInv[i]) <= 0)
            {
                continue;
            }
            return false;
        }

        return true;
    }

    public bool HaveInvSpace(Cast cast)
    {
        int maxSpace = 10;
        int freeSpace = maxSpace - myInv.Aggregate((a, b) => a + b);
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

    public bool CanPayLearnTax(Learn spell)
    {
        return myInv[0] >= spell.TomeIndex;
    }

    public Action GetResource(Learn spell)
    {
        var requiredRes = spell.TomeIndex - myInv[0];
        // can I get enough res by learning for free? 
        if (Tome[0].TaxCount >= requiredRes) return new Action { ActionType = "LEARN", ActionId = Tome[0].Id };
        // can I cast res
        var spells = Casts.Where(c => c.Delta[0] > 0 && c.Castable && HaveResForCast(c) && HaveInvSpace(c));
        if (spells.Count() == 0) return null;
        else if (spells.Count() == 1) return new Action { ActionType = "CAST", ActionId = spells.First().Id };
        else
        {
            var bestSpell = spells.OrderByDescending(s => s.Delta[0]).First();
            return new Action { ActionType = "CAST", ActionId = bestSpell.Id };
        }
    }

}

class BaseToString 
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


class BaseItem : BaseToString
{
    public int Id { get; set; }
    public int[] Delta { get; set; }

    
}

class Brew : BaseItem
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

class Cast : BaseItem
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
}

class Learn : BaseItem
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
}

public static class Extensions
{
    public static string MyToString(this int[] array)
    {
        return string.Join(" ", array);
    }
}

class Action : BaseToString
{
    public string ActionType { get; set; }
    public int ActionId { get; set; }

    public override string ToString()
    {
        return $"{ActionType} {ActionId}";
    }
}
