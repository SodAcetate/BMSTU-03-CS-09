namespace Lab_09_1{
    class Program{

        // API_call мало чем отличается от 6 лабы
        
        public class API_call{
            public static async Task<double?> GetAveragePrice(string stockName){
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/{stockName}";
                var parameters = $"?period1={DateTimeOffset.Now.AddYears(-1).ToUnixTimeSeconds()}&period2={DateTimeOffset.Now.ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                // Здесь мы делаем запрос, пока не получим ответ
                HttpResponseMessage? response = null;
                while (response == null ){
                    
                    try{
                        response = await client.GetAsync(parameters);
                    }
                    catch(System.Net.Http.HttpRequestException){
                        Console.WriteLine($"{stockName} : connection troubles, tryin' again");
                    }

                }
                
                // Считываем ответ как стрингу
                string rawResponse = await response.Content.ReadAsStringAsync();
                
                // Разделяем её на массив строк
                string[] data = rawResponse.Split('\n');
                // Считаем среднее

                double sum = 0;
                int count = 0;
                foreach (string line in data){
                    // try, потому что не по всем акциям есть норм данные - в таком случае их не получится спарсить
                    try{
                        // Пробуем делить строку запятыми
                        string[] dayData = line.Split(',');
                        // Нам нужны значения High и Low - в строке они стоят 3 и 4 соответственно. Достаём их, считаем среднее и плюсуем к сумме
                        sum += ( Convert.ToDouble(dayData[2]) + Convert.ToDouble(dayData[3]) ) / 2;
                        // Не забываем увеличить счётчик успешно считанных дней
                        count++;
                    }
                    // Это исключение появляется, когда по акции вообще нет данных
                    catch(IndexOutOfRangeException){
                        Console.WriteLine($"{stockName} : data seems to be missing");
                    }
                    // Это исключение появляется для заголовочной строки и данных null
                    catch(FormatException){}
                }

                // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
                if (count != 0) return sum / count;
                else return null;

            } 
        }
        
        // Объявляем мьютекс, чтобы безопасно писать в файл
        static Mutex mutOut = new();
        // Счётчик параллельных потоков
        static int ThreadCount = 0;
        // Функция для получения и записи данных об одной акции
        public static void GetAndWriteData(string name, StreamWriter outputWriter){
                
                // Получаем среднее значение цены
                double? avgValue = API_call.GetAveragePrice(name).GetAwaiter().GetResult();
                // Выводим на консоль (Вообще выводы необязательны, но я пока оставлю)
                Console.WriteLine($"{name} : {avgValue}");

                // Ждём освобождения мьютекса и лочим его
                mutOut.WaitOne();
                // Пишем название акции и цену в поток, используемый для записи в файл
                outputWriter.WriteLine($"{name} : {(avgValue==null? "null" : avgValue)}");
                // Передаём содержимое потока в файл
                outputWriter.Flush();
                // Освобождаем мьютекс
                mutOut.ReleaseMutex();
                // Уменьшаем счётчик потоков
                ThreadCount--;
        }

        // Асинхронная функция, запускающая функцию GetAndWriteData асинхронно
        public static async Task<Task> GetAndWriteDataAsync(string name, StreamWriter outputWriter){
            return Task.Run(() => GetAndWriteData(name, outputWriter));
        }

        static void Main(){

            // Открываем наши файлы через using
            using (FileStream input = File.Open("ticker.txt", FileMode.Open), output = File.Open("avg.txt", FileMode.Create) ){
                // Создаём потоки для чтения и записи
                StreamReader inputReader = new StreamReader(input);
                StreamWriter outputWriter = new StreamWriter(output);
                
                // Считываем строку (имя акции) и асинхронно вызываем метод получения и записи данных
                while (!inputReader.EndOfStream){
                    string name = inputReader.ReadLineAsync().GetAwaiter().GetResult();
                    // Увеличиваем счётчик потоков
                    ThreadCount++;
                    GetAndWriteDataAsync(name, outputWriter);
                    // Без задержки не работает/работает плохо. Полагаю, траблы со стороны сети
                    Thread.Sleep(100);
                }

                // Ждём завершения всех потоков
                while (ThreadCount > 0){}

                // Закрываем потоки от греха подальше
                inputReader.Close();
                outputWriter.Close();
            }


        }
    }

}