using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace CoAPNet.Options
{
    public class OptionFactory
    {
        private Dictionary<int, Type> _options = new Dictionary<int, Type>();

        private static OptionFactory _instance;

        /// <summary>
        /// A <see cref="OptionFactory"/> with all the supported options registered.
        /// </summary>
        public static OptionFactory Default => _instance ?? (_instance = new OptionFactory());

        /// <summary>
        /// Creates a new Options Factory with all the suported options registered.
        /// </summary>
        public OptionFactory()
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
            Register<Size2>();

            Register<IfMatch>();
            Register<IfNoneMatch>();

            Register<Block1>();
            Register<Block2>();
        }

        /// <summary>
        /// Creates a new Options Factory with all the suported options registered and registers addional <see cref="CoapOption"/>.
        /// </summary>
        /// <param name="addtionalOptions"></param>
        public OptionFactory(params Type[] addtionalOptions)
            : this()
        {
            Register(addtionalOptions);
        }

        /// <summary>
        /// Register an additional <see cref="CoapOption"/>
        /// </summary>
        /// <typeparam name="T">Must subclass <see cref="CoapOption"/></typeparam>
        public void Register<T>() where T : CoapOption
        {
            Register(typeof(T));
        }

        /// <summary>
        /// Regisrers additional <see cref="CoapOption"/>s
        /// </summary>
        /// <param name="addtionalOptions"></param>
        public void Register(params Type[] addtionalOptions)
        {
            foreach (var type in addtionalOptions)
            {
                if (!type.GetTypeInfo().IsSubclassOf(typeof(CoapOption)))
                    throw new ArgumentException($"Type must be a subclass of {nameof(CoapOption)}");

                var option = (CoapOption)Activator.CreateInstance(type);
                _options.Add(option.OptionNumber, type);
            }
        }

        /// <summary>
        /// Try to create an <see cref="CoapOption"/> from the option number and data. 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="data"></param>
        /// <returns><value>null</value> if the option is unsupported.</returns>
        /// <exception cref="CoapOptionException">If the option number is unsuppported and is critical (See RFC 7252 Section 5.4.1)</exception>
        public CoapOption Create(int number, byte[] data = null)
        {
            CoapOption option;
            // Let the exception get thrown if index is out of range
            if (_options.TryGetValue(number, out var type))
            {
                option = (CoapOption)Activator.CreateInstance(type);
            }
            else
            {
                // Critial option must be registered as they're esssential for understanding the message
                if (number % 2 == 1)
                    throw new CoapOptionException($"Unsupported critical option ({number})", new ArgumentOutOfRangeException(nameof(number)));

                // Return a placeholder option to give the application chance at reading them
                option = new CoapOption(number, type: OptionType.Opaque);
            }

            if (data != null)
                option.FromBytes(data);

            return option;
        }

        public CoapOption Create(int number, Stream stream, int length)
        {
            CoapOption option;
            // Let the exception get thrown if index is out of range
            if (_options.TryGetValue(number, out var type))
            {
                option = (CoapOption)Activator.CreateInstance(type);
            }
            else
            {
                // Critial option must be registered as they're esssential for understanding the message
                if (number % 2 == 1)
                    throw new CoapOptionException($"Unsupported critical option ({number})", new ArgumentOutOfRangeException(nameof(number)));

                // Return a placeholder option to give the application chance at reading them
                option = new CoapOption(number, type: OptionType.Opaque);
            }

            option.Decode(stream, length);

            return option;
        }
    }
}
