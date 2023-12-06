using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Server
{
    static async Task Main(string[] args)
    {
        long totalDataSize = 0; // Общий размер переданных данных

        string serverUrl = $"http://localhost:12345/";
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(serverUrl);
        listener.Start();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            Console.WriteLine("Клиент подключен.");

            HttpListenerRequest request = context.Request;

            using (Stream receiveStream = request.InputStream)
            {
                byte[] sizeBytes = new byte[4];
                await receiveStream.ReadAsync(sizeBytes, 0, sizeBytes.Length);
                int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                byte[] imageData = new byte[imageSize];
                int bytesRead = await receiveStream.ReadAsync(imageData, 0, imageData.Length);

                // Измеряем задержку приема
                Stopwatch receiveStopwatch = Stopwatch.StartNew();

                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    Image img = Image.FromStream(ms);

                    img.RotateFlip(RotateFlipType.Rotate180FlipNone);

                    // Увеличиваем размер изображения в 2 раза
                    img = ScaleImage(img, 2.5f); // Добавили эту строку

                    ApplyBrightnessFilter(img, 2.3f);//Добавление яркости

                    ApplyNoiseEffect(img, 50); // Измените интенсивность шума по вашему желанию

                    using (MemoryStream modifiedImageStream = new MemoryStream())
                    {
                        img.Save(modifiedImageStream, ImageFormat.Jpeg);
                        byte[] modifiedImageData = modifiedImageStream.ToArray();

                        // Измеряем задержку отправки
                        Stopwatch sendStopwatch = Stopwatch.StartNew();

                        HttpListenerResponse response = context.Response;
                        response.ContentLength64 = modifiedImageData.Length;

                        using (Stream output = response.OutputStream)
                        {
                            await output.WriteAsync(modifiedImageData, 0, modifiedImageData.Length);
                            Console.WriteLine("Измененное изображение отправлено клиенту.");
                        }

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

                totalDataSize += imageSize;

                // Анализ использования ресурсов (процессорное время)
                long processorTimeMilliseconds = (long)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
                Console.WriteLine($"Процессорное время: {processorTimeMilliseconds} мс");
            }

            context.Response.Close();

            // Вывод статистики
            Console.WriteLine($"Общий размер переданных данных: {totalDataSize} байт");
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
