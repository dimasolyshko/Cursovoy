using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class UDPServer
{
    static async Task Main(string[] args)
    {
        int serverPort = 12345; // Порт сервера
        int packetCount = 0; // Счетчик отправленных пакетов
        long totalDataSize = 0; // Общий размер переданных данных

        UdpClient udpListener = new UdpClient(serverPort);

        Console.WriteLine("Сервер запущен. Ожидание сообщения...");

        while (true)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            byte[] receivedData = udpListener.Receive(ref remoteEndPoint);

            using (MemoryStream stream = new MemoryStream(receivedData))
            using (BinaryReader reader = new BinaryReader(stream))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                //Переменная для задержки Отправки
                Stopwatch SendStopwatch = Stopwatch.StartNew();

                // Получаем номер операции
                int operation = reader.ReadInt32();

                // Получаем размер изображения
                byte[] sizeBytes = new byte[4];
                stream.Read(sizeBytes, 0, sizeBytes.Length);
                int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                // Получаем изображение
                byte[] imageData = new byte[imageSize];
                int bytesRead = stream.Read(imageData, 0, imageData.Length);

                // Обрабатываем изображение в зависимости от номера операции
                Image modifiedImage = ProcessImage(imageData, operation);

                using (MemoryStream modifiedImageStream = new MemoryStream())
                {              
                    modifiedImage.Save(modifiedImageStream, ImageFormat.Jpeg);
                    byte[] modifiedImageData = modifiedImageStream.ToArray();

                    SendStopwatch.Start();
                    //передача обновленного изображения
                    udpListener.Send(modifiedImageData, modifiedImageData.Length, remoteEndPoint);

                    SendStopwatch.Stop();
                    Console.WriteLine("Измененное изображение отправлено клиенту.");
                }
                long receiveDelayMilliseconds = SendStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Задержка приема: {receiveDelayMilliseconds} мс");

                // Обновляем статистику
                packetCount++;
                totalDataSize += imageSize;

                // Анализ использования ресурсов (процессорное время)
                long processorTimeMilliseconds = (long)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
                Console.WriteLine($"Процессорное время: {processorTimeMilliseconds} мс");
            }

            // Вывод статистики
            Console.WriteLine($"Всего отправлено пакетов: {packetCount}");
            Console.WriteLine($"Общий размер переданных данных: {totalDataSize} байт");
        }
    }

    private static Image ProcessImage(byte[] imageData, int operation)
    {
        using (MemoryStream ms = new MemoryStream(imageData))
        {
            Image img = Image.FromStream(ms);

            switch (operation)
            {
                case 1:
                    img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 2:
                    img = ScaleImage(img, 2.5f);
                    break;
                case 3:
                    ApplyBrightnessFilter(img, 2.3f);
                    break;
                case 4:
                    ApplyNoiseEffect(img, 50);
                    break;
                case 5:
                    img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    img = ScaleImage(img, 1.7f);
                    ApplyBrightnessFilter(img, 2.0f);
                    ApplyNoiseEffect(img, 50);
                    break;
                default:
                    Console.WriteLine("Неверный номер операции");
                    break;
            }
            return img;
        }
    }

    // Метод для увеличения размера изображения
    private static Image ScaleImage(Image img, float scale)
    {
        int newWidth = (int)(img.Width * scale);
        int newHeight = (int)(img.Height * scale);
        Bitmap newImage = new Bitmap(newWidth, newHeight);
        using (Graphics g = Graphics.FromImage(newImage))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, newWidth, newHeight);
        }
        return newImage;
    }

    private static void ApplyBrightnessFilter(Image img, float brightness)
    {
        ImageAttributes attributes = new ImageAttributes();
        ColorMatrix matrix = new ColorMatrix(new float[][]
        {
            new float[] {brightness, 0, 0, 0, 0},
            new float[] {0, brightness, 0, 0, 0},
            new float[] {0, 0, brightness, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        });

        attributes.SetColorMatrix(matrix);
        using (Graphics g = Graphics.FromImage(img))
        {
            g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attributes);
        }
    }

    // Метод для добавления эффекта шума к изображению
    private static void ApplyNoiseEffect(Image img, int intensity)
    {
        Bitmap bitmap = new Bitmap(img);
        Random random = new Random();

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);

                int r = pixel.R + random.Next(-intensity, intensity + 1);
                int g = pixel.G + random.Next(-intensity, intensity + 1);
                int b = pixel.B + random.Next(-intensity, intensity + 1);

                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
            }
        }

        using (Graphics g = Graphics.FromImage(img))
        {
            g.DrawImage(bitmap, 0, 0);
        }
    }
}
