
namespace Lab_09_1{
    class Program{

        public static int countIn = 0;
        public static int countOut = 0;

        public class API_call{
            public static async Task<double> GetAveragePrice(string stockName){
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/{stockName}";
                var parameters = $"?period1={DateTimeOffset.Now.AddYears(-1).ToUnixTimeSeconds()}&period2={DateTimeOffset.Now.ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);
                // запрос
                HttpResponseMessage response = null;
                //
                while (response == null ){
                    
                    try{
                        response = await client.GetAsync(parameters);
                    }
                    catch(System.Net.Http.HttpRequestException){
                        Console.WriteLine($"{stockName} : connection troubles, tryin' again");
                    }
                }
                //
                string rawResponse = "";
                if (response.IsSuccessStatusCode){
                    rawResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(rawResponse);
                    //result.Raw = rawResponse;
                }
                

                //rawResponse = rawResponse.Remove(0,rawResponse.IndexOf('\n')+1);
                string[] data = rawResponse.Split('\n');
                //Console.WriteLine(data.Length);
                double sum = 0;
                int count = 0;
                foreach (string line in data){
                    try{
                        string[] dayData = line.Split(',');
                        sum += ( Convert.ToDouble(dayData[2]) + Convert.ToDouble(dayData[3]) ) / 2;
                        count++;
                    }
                    catch(IndexOutOfRangeException){
                        Console.WriteLine($"{stockName} data seems to be missing");
                    }
                    catch(FormatException){}
                }

                countIn++;

                //Console.WriteLine(sum / data.Length);
                if (count != 0) return sum / count;
                else return 0;

            } 
        }

        public static void WriteToFile(StreamWriter outputWriter, string text){
            //using (StreamWriter outputWriter = new StreamWriter(file)){
                outputWriter.WriteLine(text);
                countOut++;
            //}
        }

        static Mutex mutOut = new();
        static Mutex mutIn = new();
        public static async void GetData(string name, StreamWriter outputWriter){
            //while (!inputReader.EndOfStream){
                    //Console.WriteLine("Iteration BEGIN");
                    
                    double avgValue = API_call.GetAveragePrice(name).GetAwaiter().GetResult();
                   

                    Console.WriteLine($"{name} : {avgValue}");


                    mutOut.WaitOne();

                    WriteToFile(outputWriter, $"{name}:{avgValue}");
                    outputWriter.Flush();

                    mutOut.ReleaseMutex();

                    //Console.WriteLine("Iteration END");
                //}
        }


        public static async Task<Task> GetDataAsync(string name, StreamWriter outputWriter){
            return Task.Run(() => GetData(name, outputWriter));
        }

        static void Main(){

            using (FileStream input = File.Open("ticker.txt", FileMode.Open), output = File.Open("avg.txt", FileMode.Create) ){
                StreamReader inputReader = new StreamReader(input);
                StreamWriter outputWriter = new StreamWriter(output);
                
                //string name;
                //double? avgValue;
                while (!inputReader.EndOfStream){

                    //mutIn.WaitOne();
                    string name = inputReader.ReadLineAsync().GetAwaiter().GetResult();
                    //mutIn.ReleaseMutex();
                    GetDataAsync(name, outputWriter);

                    Thread.Sleep(100);

                    //GetDataAsync(inputReader, outputWriter);
                }
            }

            Console.WriteLine(countIn + " | " + countOut);

        }
    }

}