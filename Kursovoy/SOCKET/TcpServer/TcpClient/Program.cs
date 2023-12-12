using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Client
{
    static void Main(string[] args)
    {
        try
        {
            while (true)
            {
                string serverIP = "127.0.0.1"; // IP-адрес сервера
                int serverPort = 12345; // Порт сервера
                using (TcpClient client = new TcpClient(serverIP, serverPort))
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    string imagePath = "D:\\ImagesForProgramming\\image.jpg"; // Путь к изображению

                    // Отправляем номер операции
                    int operation = 0; // Изменение яркости (может быть любая операция: 1, 2, 3 или 4)
                    Console.WriteLine("Введите номер операции, где \n1. Повернуть изображение на 180 градусов\n2.Увеличить изображение\n3.Добавить яркость\n4.Добавить шума\n5.Выполнить всё");
                    operation = Convert.ToInt32(Console.ReadLine());
                    writer.Write(operation);

                    // Отправляем размер изображения на сервер
                    byte[] imageData = File.ReadAllBytes(imagePath);
                    writer.Write(imageData.Length);

                    // Отправляем само изображение на сервер
                    writer.Write(imageData);
                    Console.WriteLine("Изображение и операция отправлены на сервер.");

                    // Ждем ответа от сервера перед отправкой следующего запроса
                    Console.WriteLine("Ожидание ответа от сервера...");
                    int modifiedImageSize = reader.ReadInt32();
                    byte[] modifiedImageData = reader.ReadBytes(modifiedImageSize);

                    // Сохраняем обработанное изображение
                    File.WriteAllBytes("D:\\ImagesForProgramming\\NewImageTCP.jpg", modifiedImageData);
                    Console.WriteLine("Получено и сохранено обработанное изображение.");

                }
            }    
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
            Console.ReadLine();
        }
    }
}
