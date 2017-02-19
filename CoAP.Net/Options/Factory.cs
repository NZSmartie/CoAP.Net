using System;
using System.Reflection;
using System.Collections.Generic;

namespace CoAP.Net.Options
{
    public static class Factory
    {
        static Dictionary<int, Type> _options = new Dictionary<int, Type>();

        static Factory()
        {
            Register<UriHost>();
            Register<UriPort>();
            Register<UriPath>();
            Register<UriQuery>();

            Register<ProxyUri>();
            Register<ProxyScheme>();

            Register<LocationPath>();
            Register<LocationQuery>();

            Register<ContentFormat>();
            Register<Accept>();
            Register<MaxAge>();
            Register<ETag>();
            Register<Size1>();

            Register<IfMatch>();
            Register<IfNoneMatch>();
        }

        public static void Register<T>() where T : Option
        {
            Type type = typeof(T);
            Register(type);
        }

        public static void Register(Type type)
        {
            if(!type.GetTypeInfo().IsSubclassOf(typeof(Option)))
                throw new ArgumentException(string.Format("Type must be a subclass of {0}", typeof(Option).FullName));

            var option = (Option)Activator.CreateInstance(type);
            _options.Add(option.OptionNumber, type);
        }

        public static Option Create(int number, byte[] data = null)
        {
            // Let the exception get thrown if index is out of range
            Type type = null;
            if (!_options.TryGetValue(number, out type))
                throw new ArgumentException(string.Format("Unsupported option number {0}", number));

            var option = (Option)Activator.CreateInstance(type);
            if (data != null)
                option.FromBytes(data);

            return option;
        }
    }
}
