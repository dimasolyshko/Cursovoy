using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Client
{
    static void Main(string[] args)
    {
        string serverIP = "127.0.0.1"; // IP-адрес сервера
        int serverPort = 12345; // Порт сервера

        using (TcpClient client = new TcpClient(serverIP, serverPort))
        using (NetworkStream stream = client.GetStream())
        using (BinaryReader reader = new BinaryReader(stream))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            Console.WriteLine("Подключено к серверу.");

            string imagePath = "D:\\ImagesForProgramming\\image.jpg"; // Путь к изображению

            // Отправляем размер изображения на сервер
            byte[] imageData = File.ReadAllBytes(imagePath);
            writer.Write(imageData.Length);

            // Отправляем само изображение на сервер
            writer.Write(imageData);
            Console.WriteLine("Изображение отправлено на сервер.");

            // Получаем размер и перевернутое изображение от сервера
            int reversedImageSize = reader.ReadInt32();
            byte[] reversedImageData = reader.ReadBytes(reversedImageSize);

            // Сохраняем перевернутое изображение
            File.WriteAllBytes("D:\\ImagesForProgramming\\NewImage.jpg", reversedImageData);
            Console.WriteLine("Получено и сохранено перевернутое изображение.");
        }

        Console.WriteLine("Завершение клиента.");
    }
}
