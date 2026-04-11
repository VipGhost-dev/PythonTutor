using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class PythonBridge : MonoBehaviour
{
    [Header("Connection")]
    public string serverAddress = "localhost";
    public int serverPort = 9999;
    public float connectionTimeout = 5f;
    
    public async Task<string> ExecutePythonCode(string code)
    {
        return await SendCommand("execute", code);
    }
    
    public async Task<string> TestConnection()
    {
        return await SendCommand("test", "");
    }
    
    private async Task<string> SendCommand(string command, string data)
    {
        TcpClient client = null;
        
        try
        {
            // Создаём новое соединение для каждого запроса
            client = new TcpClient();
            
            // Подключаемся с таймаутом
            var connectTask = client.ConnectAsync(serverAddress, serverPort);
            var timeoutTask = Task.Delay((int)(connectionTimeout * 1000));
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask || !client.Connected)
            {
                throw new Exception("Connection timeout");
            }
            
            // Кодируем данные в Base64 чтобы избежать проблем с JSON
            string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
            
            // Создаём простой JSON без сложного экранирования
            string jsonRequest = $"{{\"command\":\"{command}\",\"data\":\"{encodedData}\"}}";
            Debug.Log($"Sending command: {command}");
            
            byte[] bytes = Encoding.UTF8.GetBytes(jsonRequest);
            
            // Отправляем запрос
            var stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
            
            // Читаем ответ
            byte[] buffer = new byte[65536];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead == 0)
            {
                throw new Exception("No response from server");
            }
            
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log($"Received response");
            
            return response;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending command: {e.Message}");
            return $"{{\"success\": false, \"error\": \"{e.Message}\"}}";
        }
        finally
        {
            client?.Close();
        }
    }
}