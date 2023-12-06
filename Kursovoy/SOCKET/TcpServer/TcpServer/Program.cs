using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

class Server
{
    static void Main(string[] args)
    {
        int serverPort = 12345; // Порт сервера
        int packetCount = 0; // Счетчик отправленных пакетов
        long totalDataSize = 0; // Общий размер переданных данных

        // Создаем серверный сокет и начинаем прослушивание
        TcpListener server = new TcpListener(IPAddress.Any, serverPort);
        server.Start();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");

        using (TcpClient client = server.AcceptTcpClient())
        {
            Console.WriteLine("Клиент подключен.");

            using (NetworkStream stream = client.GetStream())
            {
                // Получаем размер изображения
                byte[] sizeBytes = new byte[4];
                stream.Read(sizeBytes, 0, sizeBytes.Length);
                int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                // Получаем изображение
                byte[] imageData = new byte[imageSize];
                int bytesRead = stream.Read(imageData, 0, imageData.Length);

                // Измеряем задержку приема
                Stopwatch receiveStopwatch = Stopwatch.StartNew();

                // Обрабатываем изображение
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    Image img = Image.FromStream(ms);

                    img.RotateFlip(RotateFlipType.Rotate180FlipNone);

                    // Увеличиваем размер изображения в 2 раза
                    img = ScaleImage(img, 2.5f); // Добавили эту строку

                    ApplyBrightnessFilter(img, 2.3f);//Добавление яркости

                    ApplyNoiseEffect(img, 50); // Измените интенсивность шума по вашему желанию

                    // Сохраняем перевернутое и увеличенное изображение в массив байт
                    using (MemoryStream modifiedImageStream = new MemoryStream())
                    {
                        img.Save(modifiedImageStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byte[] modifiedImageData = modifiedImageStream.ToArray();

                        // Измеряем задержку отправки
                        Stopwatch sendStopwatch = Stopwatch.StartNew();

                        // Отправляем размер измененного изображения клиенту
                        byte[] modifiedSizeBytes = BitConverter.GetBytes(modifiedImageData.Length);
                        stream.Write(modifiedSizeBytes, 0, modifiedSizeBytes.Length);

                        // Отправляем измененное изображение клиенту
                        stream.Write(modifiedImageData, 0, modifiedImageData.Length);
                        Console.WriteLine("Измененное изображение отправлено клиенту.");

                        // Измеряем задержку отправки
                        sendStopwatch.Stop();

                        long sendDelayMilliseconds = sendStopwatch.ElapsedMilliseconds;
                        Console.WriteLine($"Задержка отправки: {sendDelayMilliseconds} мс");
                    }

                }
                // Измеряем задержку приема
                receiveStopwatch.Stop();
                long receiveDelayMilliseconds = receiveStopwatch.ElapsedMilliseconds;
                Console.WriteLine($"Задержка приема: {receiveDelayMilliseconds} мс");

                // Обновляем статистику
                packetCount++;
                totalDataSize += imageSize;

                // Анализ использования ресурсов (процессорное время)
                long processorTimeMilliseconds = (long)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
                Console.WriteLine($"Процессорное время: {processorTimeMilliseconds} мс");
            }
        }
        // Завершение
        server.Stop();

        // Вывод статистики
        Console.WriteLine($"Всего отправлено пакетов: {packetCount}");
        Console.WriteLine($"Общий размер переданных данных: {totalDataSize} байт");

        Console.WriteLine("Завершение сервера.");
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
