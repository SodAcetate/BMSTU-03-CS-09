using System.Text.RegularExpressions;

namespace Lab_09_2{
    class Program{
        
        static string API_KEY = "Make an \"API_KEY\" file with your key in it";

        public struct City{

            public string Name {get; set;}
            public double Latitude {get; set;}
            public double Longitude {get; set;}

        }

        public struct Weather{
            public string Country {get; set;}
            public string Name {get; set;}
            public double Temp {get; set;}
            public string Description {get; set;}
        }

        public class API_call{
            public static async Task<Weather> GetWeather(City city){
                
                var url = $"https://api.openweathermap.org/data/2.5/weather";
                var parameters = $"?lat={city.Latitude}&lon={city.Longitude}&appid={API_KEY}";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                // Здесь мы делаем запрос, пока не получим ответ
                HttpResponseMessage? response = new();
                
                Console.WriteLine("Fetching data...");
                try{
                    response = await client.GetAsync(parameters);
                }
                catch(System.Net.Http.HttpRequestException){
                    Console.WriteLine($"{city.Name} : Connection troubles, try again");
                }
                
                // Считываем ответ как стрингу
                Weather result = new();
                
                if (response.IsSuccessStatusCode){
                    string rawResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(jsonString);
                    
                    Regex rx = new Regex("(?<=\"country\":\")[^\"]+(?=\")");
                    result.Country = rx.Match(rawResponse).ToString();
                    rx = new Regex("(?<=\"name\":\")[^\"]+(?=\")");
                    result.Name = rx.Match(rawResponse).ToString();
                    rx = new Regex("(?<=\"temp\":)[^\"]+(?=,)");
                    result.Temp = Math.Round(Convert.ToDouble(rx.Match(rawResponse).ToString())-273);
                    rx = new Regex("(?<=\"description\":\")[^\"]+(?=\")");
                    result.Description = rx.Match(rawResponse).ToString();
                    //Console.WriteLine($"\n{result.Country}, {result.Name}: {result.Temp}, {result.Description}\n");

                }

                // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
                Console.WriteLine($"{city.Name} : {result.Temp} degrees, {result.Description}");
                return result;

            } 
        }


        public static City[] options = new City[0];
        static bool resume = true;
        public static void ConsoleRun(){
            string input = Console.ReadLine();

            switch (input){
                case("quit"):{
                    resume = false;
                    break;
                }
                case("options"):{
                    foreach (City entry in options){
                        Console.WriteLine($"> {entry.Name}");
                    }
                    break;
                }
                default :{
                    try{
                        City newCity = options.Where(data => data.Name == input).First(); 
                        API_call.GetWeather(newCity);
                    }
                    catch{
                        Console.WriteLine($"No \"{input}\" city found");
                    }
                    break;
                }
            }

            if (resume) ConsoleRun();

        }
        
        static void Main(){

            // Пытаемся считать ключ из файла
            try{
                API_KEY = File.ReadAllText("API_KEY");
            }
            catch (FileNotFoundException){
                Console.WriteLine("!!! No API key found !!! Calls will receive empty data");
            }

            // Считываем данные из файла
            using (FileStream input = File.Open("city.txt", FileMode.Open)){
                StreamReader reader = new(input);

                string[] line;
                City entry = new();

                while (!reader.EndOfStream){

                    line = reader.ReadLine().Replace("\t"," ").Split(" ");
                    entry.Name = line[0];
                    for (int i = 1 ; i < line.Length - 2 ; i ++) entry.Name += $" {line[i]}";
                    entry.Latitude = Convert.ToDouble(line[line.Length - 2].Replace(",",""));
                    entry.Longitude = Convert.ToDouble(line[line.Length - 1]);
                    options = options.Append(entry).ToArray();
                }

                reader.Close();
            }

            // Инструкция и запуск цикла выполнения
            Console.WriteLine("Usage: \tType in a city name to get weather data \n"+
                                "\tType \"options\" to see the city list \n"+
                                "\tType \"quit\" to, well, quit \n"+
                                "\tType some gibberish to do nothing \n");
            ConsoleRun();

        }
    
    }       
}