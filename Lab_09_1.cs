
namespace Lab_09_1{
    class Program{

        public struct Stock{
            public string Raw {get; set;}
        }

        public class API_call{
            public static async Task<double?> GetAveragePrice(string stockName){
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/{stockName}";
                var parameters = $"?period1={DateTimeOffset.Now.AddYears(-1).ToUnixTimeSeconds()}&period2={DateTimeOffset.Now.ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                HttpResponseMessage response = await client.GetAsync(parameters);
                
                string rawResponse = "";

                //Stock result = new Stock();
                if (response.IsSuccessStatusCode){
                    rawResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(rawResponse);
                    //result.Raw = rawResponse;
                }
                

                rawResponse = rawResponse.Remove(0,rawResponse.IndexOf('\n')+1);
                string[] data = rawResponse.Split('\n');
                //Console.WriteLine(data.Length);
                double? sum = 0;
                foreach (string line in data){
                    if (line.Length > 0){
                        string[] dayData = line.Split(',');
                        if (dayData[2] != "null")
                        sum += ( Convert.ToDouble(dayData[2]) + Convert.ToDouble(dayData[3]) ) / 2;
                    }
                }
                //Console.WriteLine(sum / data.Length);

                return sum / data.Length;

            } 
        }

        static void Main(){

            using (FileStream input = File.Open("ticker.txt", FileMode.Open), output = File.Open("avg.txt", FileMode.Create)){
                StreamReader inputReader = new StreamReader(input);
                StreamWriter outputWriter = new StreamWriter(output);
                string name;
                double? avgValue;
                while (!inputReader.EndOfStream){
                    name = inputReader.ReadLine();
                    Console.Write(name + " : ");
                    avgValue = API_call.GetAveragePrice(name).GetAwaiter().GetResult();
                    Console.WriteLine(avgValue);
                    outputWriter.WriteLine($"{name}:{avgValue}");
                }

            }

        }
    }

}