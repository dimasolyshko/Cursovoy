using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Client
{
    private const int bufferSize = 1024;

    static async Task Main(string[] args)
    {
        try
        {
            string serverIP = "ws://localhost:12345"; // Адрес WebSocket сервера

            using (ClientWebSocket clientWebSocket = new ClientWebSocket())
            {
                await clientWebSocket.ConnectAsync(new Uri(serverIP), CancellationToken.None);

                while (true)
                {
                    string imagePath = "D:\\ImagesForProgramming\\image.jpg"; // Путь к изображению

                    Console.WriteLine("Введите номер операции, где:");
                    Console.WriteLine("1. Повернуть изображение на 180 градусов");
                    Console.WriteLine("2. Увеличить изображение");
                    Console.WriteLine("3. Добавить яркость");
                    Console.WriteLine("4. Добавить шум");
                    Console.WriteLine("5. Выполнить всё");
                    int operation = Convert.ToInt32(Console.ReadLine());

                    Stopwatch mainStopwatch = Stopwatch.StartNew();

                    // Отправляем номер операции серверу
                    byte[] operationData = Encoding.UTF8.GetBytes(operation.ToString());
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(operationData), WebSocketMessageType.Text, true, CancellationToken.None);

                    // Чтение изображения в байтовый массив
                    byte[] imageData = File.ReadAllBytes(imagePath);

                    // Отправляем размер изображения
                    byte[] imageSizeData = BitConverter.GetBytes(imageData.Length);
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(imageSizeData), WebSocketMessageType.Binary, true, CancellationToken.None);

                    // Отправляем само изображение
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(imageData), WebSocketMessageType.Binary, true, CancellationToken.None);
                    Console.WriteLine("Изображение и операция отправлены на сервер.");

                    byte[] buffer = new byte[bufferSize];
                    int bytesRead = 0;
                    MemoryStream modifiedImageStream = new MemoryStream();

                    do
                    {
                        WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        bytesRead = result.Count;
                        modifiedImageStream.Write(buffer, 0, bytesRead);

                        // Проверка на получение сообщения об окончании передачи данных
                        if (result.MessageType == WebSocketMessageType.Text && Encoding.UTF8.GetString(buffer, 0, result.Count) == "END")
                        {
                            break;
                        }

                    } while (true);


                    mainStopwatch.Stop();

                    // Сохраняем обработанное изображение
                    byte[] modifiedImageData = modifiedImageStream.ToArray();
                    File.WriteAllBytes("D:\\ImagesForProgramming\\NewImageWebSocket.jpg", modifiedImageData);
                    Console.WriteLine("Получено и сохранено обработанное изображение.");

                    long mainDelayMilliseconds = mainStopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Общее время отправки и получения : {mainDelayMilliseconds} мс");
                    Console.WriteLine();
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
