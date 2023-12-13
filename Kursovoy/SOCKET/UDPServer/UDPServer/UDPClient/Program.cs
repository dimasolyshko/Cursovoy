using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Client
{
    static void Main(string[] args)
    {
        try
        {
            string serverIP = "127.0.0.1"; // IP-адрес сервера
            int serverPort = 12345; // Порт сервера
            UdpClient udpClient = new UdpClient();

            using (MemoryStream stream = new MemoryStream())
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

                byte[] dataToSend = stream.ToArray();
                udpClient.Send(dataToSend, dataToSend.Length, serverIP, serverPort);

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpClient.Receive(ref remoteEndPoint);

                using (MemoryStream modifiedImageStream = new MemoryStream(receivedData))
                {
                    Image modifiedImage = Image.FromStream(modifiedImageStream);
                    modifiedImage.Save("D:\\ImagesForProgramming\\NewImageUDP.jpg", ImageFormat.Jpeg);
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
