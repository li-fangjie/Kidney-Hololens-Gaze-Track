using System;
using System.IO;
using UnityEngine;

public class FileLogger : MonoBehaviour
{
    private string _logFilePath;
    private StreamWriter _writer;
    private readonly object _fileLock = new object();


    void Awake()
    {
        string startupTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"HoloLogs_{startupTimestamp}.txt";

        _logFilePath = Path.Combine(Application.persistentDataPath, fileName);
        _writer = new StreamWriter(_logFilePath, true) { AutoFlush = true };

        //Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLog;

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            HandleLog(ex?.Message ?? "Unknown", ex?.StackTrace ?? "", LogType.Exception);
        };
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string entry = $"{time} [{type}] {logString}";
        //_writer.WriteLine($"{time} [{type}] {logString}");
        //if (type == LogType.Exception)
        //    _writer.WriteLine(stackTrace);

        lock (_fileLock)
        {
            _writer.WriteLine(entry);
            if (type == LogType.Exception)
                _writer.WriteLine(stackTrace);
        }
    }

    void OnDestroy()
    {
        //Application.logMessageReceived -= HandleLog;
        Application.logMessageReceivedThreaded -= HandleLog;
        //_writer?.Close();
        lock (_fileLock)
        {
            _writer?.Close();
        }
    }
}
