using ScottPlot;
using System.Data;

namespace MonteCarloSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!CheckInputParameters(args, out string mode, out int numberOfSimulations, out int periodsPerRound, out int itemsToComplete, out string historicFile))
                return;
            
            List<int> pastDeliveredItems = LoadHistoric(historicFile);
            if (pastDeliveredItems.Count==0)
                return;

            // Run Monte Carlo simulations
            List<int> simulationResults = RunSimulations(mode, numberOfSimulations, periodsPerRound, pastDeliveredItems, itemsToComplete);
            
            // Create bar chart and save as image
            CreateBarChart(simulationResults, "MonteCarloResults.png");

            // Calculate and print percentiles
            PrintPercentiles(simulationResults);
        }

        static List<int> HowLong(int itemsToComplete, int numberOfSimulations, List<int> pastDeliveredItems){
            List<int> simulationResults = new List<int>();
            Random rand = new Random();

            // Run Monte Carlo simulations
            for (int i = 0; i < numberOfSimulations; i++){
                int totalDelivered = 0;
                int numberOfPeriods=0;
                while (totalDelivered<itemsToComplete){
                    int randomIndex = rand.Next(pastDeliveredItems.Count);
                    totalDelivered += pastDeliveredItems[randomIndex];
                    numberOfPeriods++;
                }
                simulationResults.Add(numberOfPeriods);
            }
            
            // Sort results Ascending because the less iterations the better --> handy to calculate percentiles
            simulationResults = simulationResults.OrderBy(i=>i).ToList();
            return simulationResults;
        }

        static List<int> HowMany(int numberOfSimulations, int periodsPerRound, List<int> pastDeliveredItems){
            List<int> simulationResults = new List<int>();
            Random rand = new Random();

            // Run Monte Carlo simulations
            for (int i = 0; i < numberOfSimulations; i++){
                int totalDelivered = 0;
                for (int j = 0; j < periodsPerRound; j++){
                    int randomIndex = rand.Next(pastDeliveredItems.Count);
                    totalDelivered += pastDeliveredItems[randomIndex];
                }
                simulationResults.Add(totalDelivered);
            }
            
            // Sort results Descending because the more items the better --> handy to calculate percentiles
            simulationResults = simulationResults.OrderByDescending(i=>i).ToList();
            return simulationResults;
        }

        static bool CheckInputParameters(string[] args, out string mode, out int numberOfSimulations, out int periodsPerRound, out int itemsToComplete, out string historicFile){
            numberOfSimulations=10000; // default value
            periodsPerRound =1; // default value
            itemsToComplete =0;
            mode = "";
            historicFile = "";
            if (args.Length!=4){
                Console.WriteLine("Usage:");
                Console.WriteLine("----------");
                Console.WriteLine("dotnet run howmany [history input file] [number of simulations] [number of periods]");
                Console.WriteLine("OR");
                Console.WriteLine("dotnet run howlong [history input file] [number of simulations] [items to complete]");
                Console.WriteLine("");
                return false;
            }
            
            mode = args[0].ToUpper();
            switch (mode){
                case "HOWMANY":
                    if (!int.TryParse(args[2], out numberOfSimulations)){
                        Console.WriteLine("Wrong value for parameter 'Number of Simulations'");
                        return false;
                    }
                    
                    if (!int.TryParse(args[3], out periodsPerRound)){
                        Console.WriteLine("Wrong value for parameter 'Number of Periods in each scenario'");
                        return false;
                    }
                    break;
                case "HOWLONG":
                    if (!int.TryParse(args[2], out numberOfSimulations)){
                        Console.WriteLine("Wrong value for parameter 'Number of Simulations'");
                        return false;
                    }

                    if (!int.TryParse(args[3], out itemsToComplete)){
                        Console.WriteLine("Wrong value for parameter 'Items to complete'");
                        return false;
                    }
                    break;

                default :
                    Console.WriteLine("First parameter should be 'howmany' or 'howlong'");
                    return false;
            }

            historicFile = args[1];
            if (!File.Exists(historicFile)){
                Console.WriteLine("Cannot find the historic file '{0}'", historicFile);
                return false;
            }
            return true;
        }

        static List<int> RunSimulations(string mode, int numberOfSimulations, int periodsPerRound, List<int> pastDeliveredItems, int itemsToComplete){
            if (mode=="HOWMANY")
                return HowMany(numberOfSimulations, periodsPerRound, pastDeliveredItems);
            else
                return HowLong(itemsToComplete, numberOfSimulations, pastDeliveredItems);
        }

        static void PrintPercentiles(List<int> simulationResults){
            Console.WriteLine($"50th Percentile: {GetPercentile(simulationResults, 50)}");
            Console.WriteLine($"75th Percentile: {GetPercentile(simulationResults, 75)}");
            Console.WriteLine($"80th Percentile: {GetPercentile(simulationResults, 80)}");
            Console.WriteLine($"85th Percentile: {GetPercentile(simulationResults, 85)}");
            Console.WriteLine($"90th Percentile: {GetPercentile(simulationResults, 90)}");
        }
        static int GetPercentile(List<int> sortedResults, double percentile)
        {
            int index = (int)Math.Ceiling((percentile / 100.0) * sortedResults.Count) - 1;
            return sortedResults[index];
        }

        private static List<int> LoadHistoric(string historicFile){
            
            // Read historical data
            List<int> pastDeliveredItems = new List<int>();
            
            try{
                StreamReader reader = new StreamReader(historicFile);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line?.Split(',');
                    var isNumeric = int.TryParse(values?[1], out int deliveredItems);
                    
                    if (isNumeric) {
                        pastDeliveredItems.Add(deliveredItems);
                    }
                }
            }
            catch (Exception){
                Console.WriteLine("Error opening input file.");
            }
            return pastDeliveredItems;
        }
        static void CreateBarChart(List<int> results, string filePath)
        {
            // Count frequency of each result
            var frequency = results.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            // Prepare the data for the chart
            var values = new List<double>();
            var positions = new List<double>();
            var labels = new List<string>();

            int i = 0;
            foreach (var item in frequency.OrderBy(x => x.Key)){
                values.Add(item.Value);
                positions.Add(i);
                labels.Add(item.Key.ToString());
                i++;
            }

            // Create the plot
            var plt = new Plot();
            plt.Resize(3000, 2000);

            // Add bar chart
            plt.AddBar(values.ToArray(), positions.ToArray());

            // Set the labels
            plt.XTicks(positions.ToArray(), labels.ToArray());
            plt.XAxis.TickLabelStyle(rotation: 45);
            plt.XLabel("Total Delivered Items");
            plt.YLabel("Frequency");
            plt.Title("Monte Carlo Simulation Results");
            plt.XAxis.TickLabelStyle(fontSize: 20);

            // Save the plot as a PNG file
            plt.SaveFig(filePath);

            Console.WriteLine($"Chart saved to {filePath}");
        }
    }
}
