using console_lib;
using log_lib;
using Newtonsoft.Json;
using rabbit_lib;
using service_lib.Exceptions;
using service_lib.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using trade_contracts.Messages;

namespace service_lib
{
    public abstract class ServiceApp : IServiceApp
    {
        public event Action OnStarted;

        private IRabbitConnection _rabbit;
        private readonly IRabbitConnectionFactory _rabbitConnectionFactory;
        private readonly ILogRepo _log;

        private readonly PingHandler _pingHandler;

        public ServiceApp(
            IRabbitConnectionFactory rabbitConnectionFactory,
            ILogRepo log)
        {
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _log = log;

            _pingHandler = new PingHandler(ApplicationName);
        }

        public abstract string ApplicationName { get; }

        protected abstract List<IHandler> Handlers { get; }

        protected abstract string BaseQueueName { get; }

        protected abstract int MaxQueueVersion { get; }

        public virtual void Run()
        {
            try
            {
                _log.Info($"{ApplicationName} is going online.");
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine(exception);
            }

            ConsoleWrapper.WriteLine($"-- {ApplicationName} --");
            ConsoleWrapper.WriteLine();

            ConsoleWrapper.WriteLine("Connecting to rabbit...");

            try
            {
                _rabbit = _rabbitConnectionFactory.Connect();
                var queues = new List<string>();

                if (!string.IsNullOrWhiteSpace(_overriddenQueueName))
                {
                    queues.Add(_overriddenQueueName);
                }
                else
                {
                    queues.Add(BaseQueueName);
                    queues.AddRange(Enumerable.Range(1, MaxQueueVersion).Select(queryVersion => $"{BaseQueueName}-v{queryVersion}"));
                }

                foreach (var queue in queues)
                {
                    _rabbit.Listen(queue, OnMessageReceived);
                    ConsoleWrapper.WriteLine($"Connected using queue {queue}!");
                }

                try
                {
                    _log.Info($"{ApplicationName} has connected to rabbit.");
                }
                catch (Exception exception)
                {
                    ConsoleWrapper.WriteLine(exception);
                }

                OnStarted?.Invoke();
            }
            catch (Exception exception)
            {
                ConsoleWrapper.WriteLine(exception);
                throw;
            }

            while (true)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }
        }

        private void OnMessageReceived(string message)
        {
            try
            {
                ConsoleWrapper.WriteLine($"Received a message!");

                if (string.IsNullOrWhiteSpace(message)) { return; }
                var messageLines = message.Replace("\r\n", "\r").Replace("\n", "\r").Split('\r').Where(queryLine => !string.IsNullOrWhiteSpace(queryLine)).ToList();
                if (messageLines.Count <= 0) { return; }

                var contractName = messageLines[0].Trim();
                var contractClassName = new Func<string>(() => {
                    var pieces = contractName.Split('.');
                    return pieces != null && pieces.Any() ? pieces[pieces.Length - 1] : null;
                })();

                ConsoleWrapper.WriteLine(contractName);
                var remainingText = string.Join(Environment.NewLine, messageLines.Skip(1));

                var effectiveHandlers = new List<IHandler>();
                if (Handlers != null) { effectiveHandlers.AddRange(Handlers); }
                effectiveHandlers.Add(_pingHandler);

                foreach (var handler in effectiveHandlers)
                {
                    var handlerType = handler.GetType();
                    var handlerMessageTypes = GetHandlerMessageTypes(handler);
                    if (handlerMessageTypes == null) { continue; }

                    foreach (var handlerMessageType in handlerMessageTypes)
                    {
                        if (handlerMessageType.MessageType == Type.GetType(contractName, false, false)
                            || string.Equals(handlerMessageType.MessageType.FullName, contractName, StringComparison.Ordinal)
                            || (!string.IsNullOrWhiteSpace(contractClassName) && string.Equals(handlerMessageType.MessageType.Name, contractClassName, StringComparison.Ordinal))
                            )
                        {
                            var deserializedMessage = JsonConvert.DeserializeObject(remainingText, handlerMessageType.MessageType);

                            if (handlerMessageType.HandlerMethod != null)
                            {
                                handlerMessageType.HandlerMethod.Invoke(handler, new object[2] { _rabbit, deserializedMessage });
                                return;
                            }

                            if (handlerMessageType.RequestResponseHandlerMethod != null)
                            {
                                IResponseMessage response;

                                try
                                {
                                    response = (IResponseMessage)handlerMessageType.RequestResponseHandlerMethod.Invoke(handler, new object[1] { deserializedMessage });
                                    response.WasSuccessful = true;
                                    response.FailureReason = null;
                                }
                                catch (Exception exception)
                                {
                                    _log.Error(exception);

                                    string failureReason = null;
                                    if (exception is HandlerException handlerException)
                                    {
                                        failureReason = handlerException.FailureReason;
                                    }
                                    else if (exception is TargetInvocationException targetInvokationException)
                                    {
                                        failureReason = targetInvokationException.InnerException != null
                                            && !string.IsNullOrWhiteSpace(targetInvokationException.InnerException.Message)
                                            ? targetInvokationException.InnerException.Message
                                            : targetInvokationException.Message;
                                    }                                    

                                    if (string.IsNullOrWhiteSpace(failureReason))
                                    {
                                        failureReason = exception.Message;
                                    }

                                    response = (IResponseMessage)Activator.CreateInstance(handlerMessageType.ResponseType);
                                    response.WasSuccessful = false;
                                    response.FailureReason = failureReason;
                                }

                                if (response != null
                                    && deserializedMessage is RequestMessage req 
                                    && !string.IsNullOrWhiteSpace(req.ResponseQueue))
                                {
                                    _rabbit.PublishContract(req.ResponseQueue, response);
                                }

                                return;
                            }
                        }
                    }
                }

                ConsoleWrapper.WriteLine("No matching message handlers found.");
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        private class MessageTypeAndMethod
        {
            public Type MessageType { get; set; }
            public Type ResponseType { get; set; }
            public MethodInfo HandlerMethod { get; set; }
            public MethodInfo RequestResponseHandlerMethod { get; set; }
        }

        private List<MessageTypeAndMethod> GetHandlerMessageTypes(IHandler handler)
        {
            var messageTypes = new List<MessageTypeAndMethod>();

            var handlerType = handler.GetType();
            var candidateMethods = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(queryMethod => string.Equals(queryMethod.Name, "Handle", StringComparison.Ordinal)).ToList();
            foreach (var method in candidateMethods)
            {
                var methodParams = method.GetParameters();
                if (methodParams.Count() == 2)
                {
                    var methodParam = methodParams[1];

                    var typeText = $"{typeof(IMessageHandler).FullName}`1[[{methodParam.ParameterType.AssemblyQualifiedName}]]";

                    var reconstructed = Type.GetType(typeText, false, false);
                    if (reconstructed == null) { continue; }

                    if (!reconstructed.IsAssignableFrom(handlerType)) { continue; }

                    messageTypes.Add(
                        new MessageTypeAndMethod
                        {
                            MessageType = methodParam.ParameterType,
                            HandlerMethod = method
                        });
                }
                else if (methodParams.Count() == 1)
                {
                    var methodParam = methodParams[0];
                    var responseType = method.ReturnType;

                    var typeText = $"{typeof(IRequestResponseHandler).FullName}`2[[{methodParam.ParameterType.AssemblyQualifiedName}],[{responseType.AssemblyQualifiedName}]]";

                    var reconstructed = Type.GetType(typeText, false, false);
                    if (reconstructed == null) { continue; }

                    if (!reconstructed.IsAssignableFrom(handlerType)) { continue; }

                    messageTypes.Add(new MessageTypeAndMethod
                    {
                        MessageType = methodParam.ParameterType,
                        ResponseType = responseType,
                        RequestResponseHandlerMethod = method
                    });
                }
            }

            return messageTypes;
        }       

        public virtual void Dispose()
        {
            if (_rabbit != null)
            {
                _rabbit.Dispose();
            }
        }

        private string _overriddenQueueName = null;
        public void OverrideQueue(string queue)
        {
            _overriddenQueueName = queue;
        }
    }
}
