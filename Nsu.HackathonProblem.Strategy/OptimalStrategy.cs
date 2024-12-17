using Nsu.HackathonProblem.Contracts;

public class OptimalStrategy : ITeamBuildingStrategy
{
    public IEnumerable<Team> BuildTeams(IEnumerable<Employee> teamLeads, IEnumerable<Employee> juniors,
        IEnumerable<Wishlist> teamLeadsWishlists, IEnumerable<Wishlist> juniorsWishlists)
    {
        var teamLeadsList = teamLeads.ToList();
        var juniorsList = juniors.ToList();
        var teamLeadsWishlistsList = teamLeadsWishlists.ToList();
        var juniorsWishlistsList = juniorsWishlists.ToList();
        
        var desireMatrix = CreateDesireMatrix(teamLeadsList, juniorsList, teamLeadsWishlistsList, juniorsWishlistsList);
        var optimalAssignments = SolveWithPriorityQueue(desireMatrix);

        var teams = new List<Team>();
        for (var i = 0; i < optimalAssignments.Count; i++)
        {
            var teamLead = teamLeadsList[i];
            var junior = juniorsList[optimalAssignments[i]];
            teams.Add(new Team(teamLead, junior));
        }

        return teams;
    }

    private int[,] CreateDesireMatrix(List<Employee> teamLeads, List<Employee> juniors,
        List<Wishlist> teamLeadsWishlists, List<Wishlist> juniorsWishlists)
    {
        if (teamLeads.Count != juniors.Count) 
        {
            throw new ArgumentException("teamLeads.Count != juniors.Count");
        }
        var n = teamLeads.Count;
        var costMatrix = new int[n, n];

        for (var i = 0; i < n; i++)
        {
            var teamLead = teamLeads[i];
            var teamLeadWishlist = teamLeadsWishlists.FirstOrDefault(w => w.EmployeeId == teamLead.Id);
            for (var j = 0; j < n; j++)
            {
                var junior = juniors[j];
                var juniorWishlist = juniorsWishlists.FirstOrDefault(w => w.EmployeeId == junior.Id);

                var teamLeadSatisfaction = teamLeadWishlist != null ? 
                    (teamLeadWishlist.DesiredEmployees.Length - Array.IndexOf(teamLeadWishlist.DesiredEmployees, junior.Id)) / (double)teamLeadWishlist.DesiredEmployees.Length : 0;
                var juniorSatisfaction = juniorWishlist != null ? 
                    (juniorWishlist.DesiredEmployees.Length - Array.IndexOf(juniorWishlist.DesiredEmployees, teamLead.Id)) / (double)juniorWishlist.DesiredEmployees.Length : 0;
                
                costMatrix[i, j] = (int)((1.0 / (teamLeadSatisfaction + juniorSatisfaction + 1)) * 1000);
            }
        }

        return costMatrix;
    }

    private List<int> SolveWithPriorityQueue(int[,] costMatrix)
    {
        var n = costMatrix.GetLength(0);
        var result = new List<int>(new int[n]);
        var assignedJuniors = new HashSet<int>();
        var priorityQueue = Enumerable.Range(0, n)
            .OrderBy(i => Enumerable.Range(0, n).Min(j => costMatrix[i, j]))
            .ToList();

        foreach (var i in priorityQueue)
        {
            var bestCost = int.MaxValue;
            var bestJuniorIndex = -1;
            for (var j = 0; j < n; j++) {
                if (assignedJuniors.Contains(j) || costMatrix[i, j] >= bestCost) continue;
                bestCost = costMatrix[i, j];
                bestJuniorIndex = j;
            }
            if (bestJuniorIndex == -1) continue;
            result[i] = bestJuniorIndex;
            assignedJuniors.Add(bestJuniorIndex);
        }
        return LocalOptimization(costMatrix, result);
    }

    private List<int> LocalOptimization(int[,] costMatrix, List<int> result)
    {
        var n = costMatrix.GetLength(0);
        var continued = true;
        var iterations = 1000; 
        var iteration = 0;

        while (continued && iteration < iterations)
        {
            continued = false;
            iteration++;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++) {
                    if (i == j) continue;
                    var newResult = new List<int>(result);
                    var temp = newResult[i];
                    newResult[i] = j;
                    newResult[j] = temp;

                    if (EvaluateCost(costMatrix, newResult) < EvaluateCost(costMatrix, result))
                    {
                        result = newResult;
                        continued = true;
                    }
                }
            }
        }
        return result;
    }

    private int EvaluateCost(int[,] costMatrix, List<int> result) {
        return result.Select((t, i) => costMatrix[i, t]).Sum();
    }
}