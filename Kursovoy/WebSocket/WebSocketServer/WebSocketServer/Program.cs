using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

class Server
{
    private const int bufferSize = 1024;

    static async Task Main(string[] args)
    {
        int serverPort = 12345; // Порт сервера

        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://localhost:{serverPort}/");
        httpListener.Start();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");

        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                ProcessRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task ProcessRequest(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);

        using (var webSocket = webSocketContext.WebSocket)
        {
            byte[] buffer = new byte[bufferSize];

            while (webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    int operation = Convert.ToInt32(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));

                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    int imageSize = BitConverter.ToInt32(buffer, 0);
                    byte[] imageData = new byte[imageSize];

                    int totalBytesReceived = 0;
                    while (totalBytesReceived < imageSize)
                    {
                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        Buffer.BlockCopy(buffer, 0, imageData, totalBytesReceived, receiveResult.Count);
                        totalBytesReceived += receiveResult.Count;
                    }

                    // Измеряем задержку приема
                    Stopwatch sendStopwatch = Stopwatch.StartNew();

                    Stopwatch ImageProccesingStopwatch = Stopwatch.StartNew();

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
                                img = ScaleImage(img, 2.5f);
                                ApplyBrightnessFilter(img, 2.3f);
                                ApplyNoiseEffect(img, 50);
                                break;
                            default:
                                Console.WriteLine("Неверный номер операции");
                                break;
                        }
                        ImageProccesingStopwatch.Stop();

                        sendStopwatch.Restart();

                        using (MemoryStream modifiedImageStream = new MemoryStream())
                        {
                            img.Save(modifiedImageStream, ImageFormat.Jpeg);
                            byte[] modifiedImageData = modifiedImageStream.ToArray();

                            await webSocket.SendAsync(new ArraySegment<byte>(modifiedImageData), WebSocketMessageType.Binary, true, CancellationToken.None);

                            // Отправка сообщения об окончании передачи данных
                            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("END")), WebSocketMessageType.Text, true, CancellationToken.None);

                            Console.WriteLine("Измененное изображение отправлено клиенту.");
                        }
                    }
                    // Измеряем задержку приема
                    sendStopwatch.Stop();
                    long sendDelayMilliseconds = sendStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Задержка отправки: {sendDelayMilliseconds} мс");

                    long ImageProccesingDelayMilliseconds = ImageProccesingStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Время обработки изображения: {ImageProccesingDelayMilliseconds} мс");

                    // Анализ использования ресурсов (процессорное время)
                    long processorTimeMilliseconds = (long)Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
                    Console.WriteLine($"Процессорное время: {processorTimeMilliseconds} мс");
                    Console.WriteLine();
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
            }
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
