using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Client
{
    static async Task Main(string[] args)
    {
        string serverUrl = "http://localhost:12345/";

        using (HttpClient client = new HttpClient())
        {
            Console.WriteLine("Подключено к серверу.");

            string imagePath = "D:\\ImagesForProgramming\\image.jpg"; // Путь к изображению

            byte[] imageData = File.ReadAllBytes(imagePath);
            int imageSize = imageData.Length;

            MemoryStream imageStream = new MemoryStream();
            imageStream.Write(BitConverter.GetBytes(imageSize), 0, sizeof(int));
            imageStream.Write(imageData, 0, imageSize);
            imageStream.Seek(0, SeekOrigin.Begin);

            StreamContent content = new StreamContent(imageStream);
            HttpResponseMessage response = await client.PostAsync(serverUrl, content);

            byte[] modifiedImageData = await response.Content.ReadAsByteArrayAsync();

            File.WriteAllBytes("D:\\ImagesForProgramming\\NewImageHTTP.jpg", modifiedImageData);
            Console.WriteLine("Получено и сохранено измененное изображение.");
        }

        Console.WriteLine("Завершение клиента.");
    }
}
