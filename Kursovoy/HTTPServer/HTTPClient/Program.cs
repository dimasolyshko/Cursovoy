using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Client
{
    static async Task Main(string[] args)
    {
        try
        {
            while (true)
            {
                string serverUrl = "http://localhost:12345/";

                using (HttpClient client = new HttpClient())
                {
                    string imagePath = "D:\\ImagesForProgramming\\image.jpg"; // Путь к изображению

                    byte[] imageData = File.ReadAllBytes(imagePath);
                    int imageSize = imageData.Length;

                    Console.WriteLine("Введите номер операции:\n1. Повернуть изображение на 180 градусов\n2. Увеличить изображение\n3. Добавить яркость\n4. Добавить шум\n5. Выполнить всё");
                    int operation = int.Parse(Console.ReadLine());

                    //Измерение общего времени отправки и получения изображения
                    Stopwatch mainStopwatch = Stopwatch.StartNew();

                    // Создание потока с данными изображения и операцией
                    MemoryStream imageStream = new MemoryStream();
                    imageStream.Write(BitConverter.GetBytes(operation), 0, sizeof(int));
                    imageStream.Write(BitConverter.GetBytes(imageSize), 0, sizeof(int));
                    imageStream.Write(imageData, 0, imageSize);
                    imageStream.Seek(0, SeekOrigin.Begin);

                    StreamContent content = new StreamContent(imageStream);
                    HttpResponseMessage response = await client.PostAsync(serverUrl, content);

                    byte[] modifiedImageData = await response.Content.ReadAsByteArrayAsync();

                    mainStopwatch.Stop();

                    long mainDelayMilliseconds = mainStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Общее время отправки и получения изображения: {mainDelayMilliseconds} мс");

                    File.WriteAllBytes("D:\\ImagesForProgramming\\NewImageHTTP.jpg", modifiedImageData);
                    Console.WriteLine("Получено и сохранено измененное изображение.");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
}
