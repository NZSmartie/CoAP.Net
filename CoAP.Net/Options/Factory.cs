using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace CoAP.Net.Options
{
    public static class Factory
    {
        static Dictionary<int, Type> _options = new Dictionary<int, Type>();

        static Factory()
        {
            AddOption<UriHost>();
            AddOption<UriPort>();
            AddOption<UriPath>();
            AddOption<UriQuery>();

            AddOption<ProxyUri>();
            AddOption<ProxyScheme>();

            AddOption<LocationPath>();
            AddOption<LocationQuery>();

            AddOption<ContentFormat>();
            AddOption<Accept>();
            AddOption<MaxAge>();
            AddOption<ETag>();
            AddOption<Size1>();

            AddOption<IfMatch>();
            AddOption<IfNoneMatch>();
        }

        public static void AddOption<T>()
        {
            Type type = typeof(T);
            AddOption(type);
        }

        public static void AddOption(Type type)
        {
            if(!type.GetTypeInfo().IsSubclassOf(typeof(Option)))
                throw new ArgumentException(string.Format("Type must be a subclass of {0}", typeof(Option).FullName));

            _options.Add(Construct(type).OptionNumber, type);
        }

        public static Option CreateFromOptionNumber(int number, byte[] data = null)
        {
            // Let the exception get thrown if index is out of range
            var option = Construct(_options[number]);
            if (data != null)
                option.FromBytes(data);

            return option;
        }

        private static Option Construct(Type type) {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new InvalidOperationException(string.Format("Please provide a default constructor that accepts no parameters for {0}", type.FullName));

            var obj = constructor.Invoke(null);

            Option option = obj as Option;
            if (option == null)
                throw new InvalidOperationException();
            return option;
        }

    }
}
