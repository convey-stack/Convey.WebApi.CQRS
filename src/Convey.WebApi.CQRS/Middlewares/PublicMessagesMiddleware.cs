using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Convey.CQRS.Commands;
using Convey.CQRS.Events;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Convey.WebApi.CQRS.Middlewares
{
    public class PublicMessagesMiddleware
    {
        private const string ContentType = "application/json";
        private readonly RequestDelegate _next;
        private readonly string _endpoint;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            }
        };

        private static readonly MessageTypes Messages = new MessageTypes();
        private static int _initialized;
        private static string _serializedMessages = "{}";

        public PublicMessagesMiddleware(RequestDelegate next, string endpoint, IEnumerable<Type> assemblyTypes,
            bool attributeRequired, Type attributeType)
        {
            _next = next;
            _endpoint = endpoint;
            if (_initialized == 1)
            {
                return;
            }

            Load(attributeRequired, attributeType, assemblyTypes);
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != _endpoint)
            {
                return _next(context);
            }

            context.Response.ContentType = ContentType;
            context.Response.WriteAsync(_serializedMessages);

            return Task.CompletedTask;
        }

        private static void Load(bool attributeRequired, Type attributeType, IEnumerable<Type> assemblyTypes)
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                return;
            }

            var assemblies = new HashSet<Assembly>
            {
                Assembly.GetEntryAssembly()
            };
            foreach (var assemblyType in assemblyTypes ?? Enumerable.Empty<Type>())
            {
                assemblies.Add(Assembly.GetAssembly(assemblyType));
            }

            var messages = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => (!attributeRequired || !(t.GetCustomAttribute(attributeType) is null)) &&
                            !t.IsInterface).ToArray();

            foreach (var command in messages.Where(t => typeof(ICommand).IsAssignableFrom(t)))
            {
                var instance = FormatterServices.GetUninitializedObject(command);
                var name = instance.GetType().Name;
                if (Messages.Commands.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Command: '{name}' already exists.");
                }

                SetInstanceProperties(instance);
                Messages.Commands[name] = instance;
            }

            foreach (var @event in messages.Where(t => typeof(IEvent).IsAssignableFrom(t)))
            {
                var instance = FormatterServices.GetUninitializedObject(@event);
                var name = instance.GetType().Name;
                if (Messages.Events.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Event: '{name}' already exists.");
                }

                SetInstanceProperties(instance);
                Messages.Events[name] = instance;
            }

            _serializedMessages = JsonConvert.SerializeObject(Messages, SerializerSettings);
        }

        private static void SetInstanceProperties(object instance)
        {
            var type = instance.GetType();
            foreach (var propertyInfo in type.GetProperties())
            {
                if (propertyInfo.PropertyType == typeof(string))
                {
                    SetValue(propertyInfo, instance, string.Empty);
                    continue;
                }

                if (!propertyInfo.PropertyType.IsClass)
                {
                    continue;
                }

                var propertyInstance = FormatterServices.GetUninitializedObject(propertyInfo.PropertyType);
                SetValue(propertyInfo, instance, propertyInstance);
                SetInstanceProperties(propertyInstance);
            }
        }

        private static void SetValue(PropertyInfo propertyInfo, object instance, object value)
        {
            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(instance, value);
                return;
            }

            var propertyName = propertyInfo.Name;
            var field = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(x => x.Name.StartsWith($"<{propertyName}>"));
            field?.SetValue(instance, value);
        }

        private class MessageTypes
        {
            public Dictionary<string, object> Commands { get; } = new Dictionary<string, object>();
            public Dictionary<string, object> Events { get; } = new Dictionary<string, object>();
        }
    }
}