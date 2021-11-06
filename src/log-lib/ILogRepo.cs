using log_lib.Models;
using System;
using System.Collections.Generic;
namespace log_lib
{
    public interface ILogRepo
    {
        void Verbose(string message);

        void Verbose(EventType eventType = null, Guid? correlationId = null);

        void Timing(string message);

        void Info(EventType eventType = null, Guid? correlationId = null);

        void Info(string message, EventType eventType = null, Guid? correlationId = null);

        void Debug(string message, EventType eventType = null, Guid? correlationId = null);

        void Error(string message, Exception exception, EventType eventType = null, Guid? correlationId = null);

        void Error(Exception exception, EventType eventType = null, Guid? correlationId = null);

        void Error(string message, EventType eventType = null, Guid? correlationId = null);

        void Warn(string message, EventType eventType = null, Guid? correlationId = null);

        void Log(string message, LogLevel level, EventType eventType = null, Guid? correlationId = null);

        void Log(LogItem logItem);

        List<LogItem> Get(int max);

        List<LogItem> GetExcluding(int max, List<LogLevel> exclusions = null);

        List<LogItem> GetForEventTypes(List<EventType> eventTypes, int max);

        List<LogItem> GetErrorLogs(int max);

        List<LogItem> GetForLevel(int max, LogLevel level);

        void EnableConsole();

        void DisableConsole();
    }
}
