using config_client_lib;
using config_connection_string_lib;
using log_lib.Models;
using mongo_lib;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace log_lib
{
    public class LogRepo : ILogRepo
    {
        private const string DatabaseName = "log";
        private const string LogCollectionName = "coin-log";

        private IMongoCollectionContext _collectionContext;

        private readonly object Locker = new object();
        private IMongoCollectionContext GetLogContext()
        {
            if (_collectionContext != null) { return _collectionContext; }

            lock (Locker)
            {
                if (_collectionContext != null) { return _collectionContext; }
                _collectionContext = new MongoCollectionContext
                {
                    DatabaseName = DatabaseName,
                    CollectionName = LogCollectionName,
                    ConnectionString = GetConnectionString()
                }; ;
            }

            return _collectionContext;
        }

        private readonly IGetConnectionString _cfgGetConnectionString;
        private readonly Func<string> _funcGetConnectionString;
        private string GetConnectionString()
        {
            if (_funcGetConnectionString != null) { return _funcGetConnectionString(); }
            if (_cfgGetConnectionString != null) { return _cfgGetConnectionString.GetConnectionString(); }

            return null;
        }

        private readonly bool _logToConsoleOnly = false;

        public LogRepo(bool logToConsoleOnly)
        {
            _logToConsoleOnly = true;
        }

        // I don't like this dependency...
        public LogRepo() : this(new ConfigClient())
        {
        }

        public LogRepo(Func<string> getConnectionString)
        {
            _funcGetConnectionString = getConnectionString;
        }

        public LogRepo(IGetConnectionString getConnectionString)
        {
            _cfgGetConnectionString = getConnectionString;
        }
        
        public void Info(EventType eventType = null, Guid? correlationId = null)
        {
            Log(null, LogLevel.Info, eventType, correlationId);
        }

        public void Info(string message, EventType eventType = null, Guid? correlationId = null)
        {
            Log(message, LogLevel.Info, eventType, correlationId);
        }

        public void Error(string message, Exception exception, EventType eventType = null, Guid? correlationId = null)
        {
            var fullMessageBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message))
            {
                fullMessageBuilder.AppendLine(message);
            }

            if (exception != null && !string.IsNullOrWhiteSpace(exception.Message))
            {
                fullMessageBuilder.AppendLine(exception.Message);
            }

            var logItem = new LogItem
            {
                CorrelationId = correlationId,
                Message = fullMessageBuilder.ToString(),
                StackTrace = exception?.StackTrace,
                ExceptionType = exception?.GetType()?.FullName,
                EventType = eventType,
                Application = AppDomain.CurrentDomain.FriendlyName,
                Machine = Environment.MachineName,
                Level = LogLevel.Error
            };

            Log(logItem);
        }

        public void Error(Exception exception, EventType eventType = null, Guid? correlationId = null)
        {
            var logItem = new LogItem
            {
                CorrelationId = correlationId,
                Message = exception?.Message,
                StackTrace = exception?.StackTrace,
                ExceptionType = exception?.GetType()?.FullName,
                EventType = eventType,
                Application = AppDomain.CurrentDomain.FriendlyName,
                Machine = Environment.MachineName,
                Level = LogLevel.Error
            };

            Log(logItem);
        }

        public void Error(string message, EventType eventType = null, Guid? correlationId = null)
        {
            Log(message, LogLevel.Error, eventType, correlationId);
        }

        public void Warn(string message, EventType eventType = null, Guid? correlationId = null)
        {
            Log(message, LogLevel.Warning, eventType, correlationId);
        }

        public void Log(string message, LogLevel level, EventType eventType = null, Guid? correlationId = null)
        {
            var logItem = new LogItem
            {
                Message = message,
                Level = level,
                EventType = eventType,
                CorrelationId = correlationId,
                Application = AppDomain.CurrentDomain.FriendlyName,
                Machine = Environment.MachineName
            };

            Log(logItem);

        }

        public void Log(LogItem logItem)
        {
            if (logItem == null) { return; }

            try
            {
                if (!_logToConsoleOnly)
                {
                    GetLogContext().Insert(logItem);
                }
            }
            // It's a log. What should we do if logging fails? Log it?
            catch (Exception exception)
            {
                if (_isConsoleEnabled)
                {
                    try
                    {
                        Console.WriteLine("Failed to insert log.");
                        Console.WriteLine(exception);
                    }
                    catch { }
                }
            }
            finally
            {
                if (_isConsoleEnabled && !string.IsNullOrWhiteSpace(logItem.Message))
                {
                    Console.WriteLine(logItem.Message);
                }
            }
        }

        public List<LogItem> Get(int max)
        {
            return GetExcluding(max, new List<LogLevel>());
        }

        public List<LogItem> GetErrorLogs(int max)
        {
            return GetForLevel(max, LogLevel.Error);
        }

        public List<LogItem> GetForLevel(int max, LogLevel level)
        {
            var levelText = level.ToString();

            var query = GetLogContext().GetCollection<LogItem>().AsQueryable()
                .Where(item => item.Level == levelText)
                .OrderByDescending(item => item.Id);

            return max > 0
                ? query.Take(max).ToList()
                : query.ToList();
        }

        public List<LogItem> GetExcluding(int max, List<LogLevel> exclusions)
        {
            var query = GetLogContext().GetCollection<LogItem>().AsQueryable();
            foreach(var exclusion in exclusions ?? new List<LogLevel>())
            {
                var exclusionText = exclusion.ToString();
                query = query.Where(item => item.Level != exclusionText);
            }
            query = query.OrderByDescending(item => item.Id);

            return max > 0
                ? query.Take(max).ToList()
                : query.ToList();
        }

        public List<LogItem> GetForEventTypes(List<EventType> eventTypes, int max)
        {
            var eventTypeStrings = (eventTypes ?? new List<EventType>()).Select(item => item.ToString()).ToList();

            var query = GetLogContext().GetCollection<LogItem>().AsQueryable();
            if (eventTypes != null && eventTypes.Any())
            {
                query = query.Where(item => eventTypeStrings.Contains(item.EventType));
            }
            query = query.OrderByDescending(item => item.Id);

            return max > 0
                ? query.Take(max).ToList()
                : query.ToList();
        }

        public void Verbose(EventType eventType = null, Guid? correlationId = null)
        {
            Log(null, LogLevel.Verbose, eventType, correlationId);
        }

        public void Verbose(string message)
        {
            Log(message, LogLevel.Verbose);
        }

        public void Debug(string message, EventType eventType = null, Guid? correlationId = null)
        {
            Log(message, LogLevel.Debug, eventType, correlationId);
        }

        public void Timing(string message)
        {
            Log(message, LogLevel.Verbose, new EventType("Timing"));
        }

        private bool _isConsoleEnabled = false;

        public void EnableConsole()
        {
            _isConsoleEnabled = true;
        }

        public void DisableConsole()
        {
            _isConsoleEnabled = false;
        }
    }
}
