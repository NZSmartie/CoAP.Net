#region License
// Copyright 2017 Roman Vaughan (NZSmartie)
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace CoAPNet
{
    /// <summary>
    /// Represents CoAP-Option specific errors that occur during application execution.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CoapOptionException : CoapException
    {
        /// <inheritdoc/>
        public CoapOptionException() 
            : base()
        { }

        /// <inheritdoc/>
        public CoapOptionException(string message) 
            : base(message, CoapMessageCode.BadOption)
        { }

        /// <inheritdoc/>
        public CoapOptionException(string message, Exception innerException) 
            : base(message, innerException, CoapMessageCode.BadOption)
        { }

        /// <inheritdoc/>
        public CoapOptionException(string message, CoapMessageCode responseCode) 
            : base(message, responseCode)
        { }

        /// <inheritdoc/>
        public CoapOptionException(string message, Exception innerException, CoapMessageCode responseCode) 
            : base(message, innerException, responseCode)
        { }
    }

    /// <summary>
    /// Represents the Value type for <see cref="CoapOption"/> that dictates use of <see cref="CoapOption.ValueOpaque"/>, <see cref="CoapOption.ValueString"/> or <see cref="CoapOption.ValueUInt"/>.
    /// </summary>
    public enum OptionType
    {
        /// <summary>
        /// Defines the <see cref="CoapOption"/> to have no content
        /// </summary>
        Empty,
        /// <summary>
        /// Defines the <see cref="CoapOption"/>'s data is an <c><see cref="byte"/>[]</c>
        /// </summary>
        Opaque,
        /// <summary>
        /// Defines the <see cref="CoapOption"/>'s value is an <see cref="uint"/>
        /// </summary>
        UInt,
        /// <summary>
        /// Defines the <see cref="CoapOption"/>'s data is a human readable <see cref="String"/>.
        /// </summary>
        String
    }

    /// <summary>
    /// Helper extension methods for handling collections of <see cref="CoapOption"/>s
    /// </summary>
    public static class CoapOptionExtensions
    {
        /// <summary>
        /// Gets the first <typeparamref name="OptionType"/> in <paramref name="collection"/> that matches the class type. 
        /// </summary>
        /// <typeparam name="OptionType"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static OptionType Get<OptionType>(this ICollection<CoapOption> collection) where OptionType : CoapOption
        {
            return (OptionType)collection.FirstOrDefault(o => o is OptionType);
        }

        /// <summary>
        /// Gets the first <typeparamref name="OptionType"/> in <paramref name="collection"/> that matches the class type. 
        /// </summary>
        /// <typeparam name="OptionType"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IEnumerable<OptionType> GetAll<OptionType>(this ICollection<CoapOption> collection) where OptionType : CoapOption
        {
            return collection.Where(o => o is OptionType).Select(o => (OptionType)o);
        }

        /// <summary>
        /// Checks if the <typeparamref name="OptionType"/> exists in <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="OptionType"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool Contains<OptionType>(this ICollection<CoapOption> collection) where OptionType : CoapOption
        {
            return collection.Any(o => o is OptionType);
        }

        /// <summary>
        /// Gets the first <see cref="CoapOption"/> in <paramref name="collection"/> that matches the <paramref name="optionNumber"/>
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="optionNumber"></param>
        /// <returns></returns>
        public static bool Contains(this ICollection<CoapOption> collection, int optionNumber)
        {
            return collection.Any(o => o.OptionNumber == optionNumber);
        }
    }

    // TODO: Caching (Section 5.6 of [RFC7252])
    // TODO: Proxying (Section 5.7 of [RFC7252])
    /// <summary>
    /// Repressents a CoAP-Option (Much like HTTP Headers) that may be present in a <see cref="CoapMessage"/>.
    /// </summary>
    public class CoapOption
    {
        private readonly int _optionNumber;
        /// <summary>
        /// Gets whether the option should fail if not supported by the CoAP endpoint
        /// <para>See Section 5.4.1 of [RFC7252]</para>
        /// </summary>
        public bool IsCritical
        {
            get
            {
                return (_optionNumber & 0x01) > 0;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_type == OptionType.Empty)
                return $"<{GetType().Name}:{OptionNumber}> (empty)";

            if (_type == OptionType.Opaque)
                return $"<{GetType().Name}:{OptionNumber}> ({Length} bytes)";

            if (_type == OptionType.String)
                return $"<{GetType().Name}:{OptionNumber}> \"{(string)_value}\"";

            if (_type == OptionType.UInt && _value != null)
                return $"<{GetType().Name}:{OptionNumber}> {(uint)_value}";

            return $"{GetType().Name}";
        }

        /// <summary>
        /// Gets if this resource is unsafe to proxy through the CoAP endpoint
        /// <para>See Section 5.4.2 of [RFC7252]</para>
        /// </summary>
        public bool IsUnsafe
        {
            get
            {
                return (_optionNumber & 0x02) > 0;
            }
        }

        /// <summary>
        /// Gets whether the option is allowed to have a chache key?
        /// </summary>
        public bool NoCacheKey
        {
            get
            {
                return (_optionNumber & 0x1e) == 0x1c;
            }
        }

        /// <summary>
        /// Gets the unique option number as defined in the CoAP Option Numbers Registry
        /// <para>See section 12.2 of [RFC7252]</para>
        /// </summary>
        public int OptionNumber { get => _optionNumber; }

        private readonly int _minLength;

        /// <summary>
        /// Gets the minimum length supported by this option. 
        /// </summary>
        public int MinLength { get => _minLength; }

        private readonly int _maxLength;

        /// <summary>
        /// Gets the maximum length supported by this option. 
        /// </summary>
        public int MaxLength { get => _maxLength; }

        private bool _isRepeatable;

        /// <summary>
        /// Gets whether this option is allowed to be sent multiple times in a single CoAP message
        /// </summary>
        public bool IsRepeatable { get => _isRepeatable; }

        private readonly OptionType _type;

        /// <summary>
        /// Gets the <see cref="OptionType"/> of this option.
        /// </summary>
        public OptionType Type { get => _type; }

        private readonly object _default;

        private object _value = null;

        /// <summary>
        /// Gets the default value for this <see cref="CoapOption"/>
        /// </summary>
        public uint DefaultUInt
        {
            get
            {
                if (_type != OptionType.UInt)
                    throw new InvalidCastException();
                return _default == null ? 0 : (uint)_default;
            }
        }

        /// <summary>
        /// Gets or sets the unsigned integer value for this <see cref="CoapOption"/>
        /// </summary>
        public virtual uint ValueUInt
        {
            get
            {
                if (_type != OptionType.UInt)
                    throw new InvalidCastException();
                return (uint)_value;
            }
            set
            {
                if (_type != OptionType.UInt)
                    throw new InvalidCastException();
                _value = value;
                if (value > 0xFFFFFFu)
                    Length = 4;
                else if (value > 0xFFFFu)
                    Length = 3;
                else if (value > 0xFFu)
                    Length = 2;
                else if (value > 0u)
                    Length = 1;
                else
                    Length = 0;
            }
        }

        /// <summary>
        /// Gets the default opaque value for this <see cref="CoapOption"/>
        /// </summary>
        public byte[] DefaultOpaque
        {
            get
            {
                if (_type != OptionType.Opaque)
                    throw new InvalidCastException();
                return (byte[])_default;
            }
        }

        /// <summary>
        /// Gets or sets the opaque value for this <see cref="CoapOption"/>
        /// </summary>
        public virtual byte[] ValueOpaque
        {
            get
            {
                if (_type != OptionType.Opaque)
                    throw new InvalidCastException();
                return (byte[])_value;
            }
            set
            {
                if (_type != OptionType.Opaque)
                    throw new InvalidCastException();
                _value = value;
                Length = value == null ? 0 : value.Length;
            }
        }

        /// <summary>
        /// Gets the default string value for this <see cref="CoapOption"/>
        /// </summary>
        public string DefaultString
        {
            get
            {
                if (_type != OptionType.String)
                    throw new InvalidCastException();
                return (string)_default;
            }
        }

        /// <summary>
        /// Gets or sets the string value for this <see cref="CoapOption"/>
        /// </summary>
        public virtual string ValueString
        {
            get
            {
                if (_type != OptionType.String)
                    throw new InvalidCastException();
                return (string)_value;
            }
            set
            {
                if (_type != OptionType.String)
                    throw new InvalidCastException();
                _value = value;
                Length = value == null ? 0 : Encoding.UTF8.GetByteCount(value);
            }
        }

        /// <summary>
        /// Gets the length of the CoAP Option data in bytes
        /// </summary>
        public virtual int Length { get; protected set; }

        /// <summary>
        /// Gets the <see cref="byte"/>[] representation of this <see cref="CoapOption"/>
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public virtual byte[] GetBytes()
        {
            if (_type == OptionType.Empty)
                return new byte[0];

            if (Length < _minLength || Length > _maxLength)
                throw new CoapOptionException($"Invalid option length ({Length}). Must be between {_minLength} and {_maxLength} bytes");

            if (_type == OptionType.Opaque)
                return (byte[])_value;

            if (_type == OptionType.String)
                return Encoding.UTF8.GetBytes((string)_value);

            var data = new byte[Length];
            uint i = 0, value = (uint)_value;
            if (Length == 4)
                data[i++] = (byte)((value & 0xFF000000u) >> 24);
            if (Length >= 3)
                data[i++] = (byte)((value & 0xFF0000u) >> 16);
            if (Length >= 2)
                data[i++] = (byte)((value & 0xFF00u) >> 8);
            if (Length >= 1)
                data[i++] = (byte)(value & 0xFFu);
            return data;
        }

        public virtual void Encode(Stream stream)
        {
            if (_type == OptionType.Empty)
                return;

            if (Length < _minLength || Length > _maxLength)
                throw new CoapOptionException($"Invalid option length ({Length}). Must be between {_minLength} and {_maxLength} bytes");

            if (_type == OptionType.Opaque)
            {
                stream.Write((byte[])_value, 0, ((byte[])_value).Length);
            }
            else if (_type == OptionType.String)
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                    writer.Write((string)_value);
            }
            else
            {
                uint value = (uint)_value;
                if (Length == 4)
                    stream.WriteByte((byte)((value & 0xFF000000u) >> 24));
                if (Length >= 3)
                    stream.WriteByte((byte)((value & 0xFF0000u) >> 16));
                if (Length >= 2)
                    stream.WriteByte((byte)((value & 0xFF00u) >> 8));
                if (Length >= 1)
                    stream.WriteByte((byte)(value & 0xFFu));
            }
        }

        /// <summary>
        /// Decodes a <see cref="byte"/>[] into this <see cref="CoapOption"/>
        /// </summary>
        /// <param name="data"></param>
        [Obsolete]
        public virtual void FromBytes(byte[] data)
        {
            if (_type == OptionType.Empty)
            {
                if ((data?.Length ?? 0) > 0)
                    throw new InvalidCastException("Empty option does not accept any data");
                return;
            }

            if(data.Length < _minLength || data.Length > _maxLength)
                throw new CoapOptionException($"Invalid option length ({data.Length}). Must be between {_minLength} and {_maxLength} bytes");

            if (_type == OptionType.Opaque)
            {
                ValueOpaque = data;
                return;
            }

            if (_type == OptionType.String)
            {
                ValueString = Encoding.UTF8.GetString(data);
                return;
            }

            
            uint i=0, value = 0;
            if (data.Length == 4)
                value = (uint)(data[i++] << 24);
            if (data.Length >= 3)
                value |= (uint)(data[i++] << 16);
            if (data.Length >= 2)
                value |= (uint)(data[i++] << 8);
            if (data.Length >= 1)
                value |= data[i++];
            ValueUInt = value;
        }

        public virtual void Decode(Stream stream, int length)
        {
            if (_type == OptionType.Empty)
            {
                if (length > 0)
                    throw new InvalidCastException("Empty option does not accept any data");
                return;
            }

            if (length < _minLength || length > _maxLength)
                throw new CoapOptionException($"Invalid option length ({length}). Must be between {_minLength} and {_maxLength} bytes");

            if (_type == OptionType.Opaque)
            {
                ValueOpaque = new byte[length];
                stream.Read(ValueOpaque, 0, length);
                return;
            }

            if (_type == OptionType.String)
            {
                // TODO: Figure out how to avoid allocating a byte array when reading a string from the stream.
                var data = new byte[length];
                stream.Read(data, 0, length);
                ValueString = Encoding.UTF8.GetString(data);
                return;
            }

            uint value = 0;
            if (length == 4)
                value = (uint)(stream.ReadByte() << 24);
            if (length >= 3)
                value |= (uint)(stream.ReadByte() << 16);
            if (length >= 2)
                value |= (uint)(stream.ReadByte() << 8);
            if (length >= 1)
                value += (uint)stream.ReadByte();

            ValueUInt = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionNumber"></param>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        /// <param name="isRepeatable"></param>
        /// <param name="type"></param>
        /// <param name="defaultValue"></param>
        protected internal CoapOption(int optionNumber, int minLength = 0, int maxLength = 0, bool isRepeatable = false, OptionType type = OptionType.Empty, object defaultValue = null)
        {
            if (optionNumber > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(optionNumber), $"option number can not be greater than a 16bit unsigned value ({ushort.MaxValue})");

            if (minLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minLength), $"option length can not be less than 0");

            if (maxLength > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxLength), $"option length can not be greater than a 16bit unsigned value ({ushort.MaxValue})");

            if(maxLength < minLength)
                throw new ArgumentException(nameof(maxLength), $"option max length can not be less than the min length");


            _optionNumber = optionNumber;
            _type = type;
            _minLength = minLength;
            _maxLength = maxLength;
            _isRepeatable = isRepeatable;
            _default = defaultValue;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var option = obj as CoapOption;
            if (option == null)
                return base.Equals(obj);

            if (option._optionNumber != _optionNumber)
                return false;
            // Asume all other parameters of the option match; Only focus on the value

            switch (_type)
            {
                case OptionType.Empty:
                    return true;
                case OptionType.UInt:
                    if (_value is null)
                        return option._value is null;

                    if (option._value is null)
                        return false;

                    return (uint)_value == (uint)option._value;
                case OptionType.Opaque:
                    if (_value is null)
                        return option._value is null;

                    if (option._value is null)
                        return false;

                    return ((byte[])_value).SequenceEqual((byte[])option._value);
                case OptionType.String:
                    if (_value is null)
                        return option._value is null;

                    return ((string)_value).Equals((string)option._value, StringComparison.Ordinal);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static Dictionary<Type, int> _hashCode = new Dictionary<System.Type, int>();
        
        /// <summary>
        /// Gets a hashcode unique to the <see cref="CoapOption"/> sub class. 
        /// </summary>
        /// <remarks>
        /// This will generate and store the hashcode based on the subclass's full name. 
        /// </remarks>
        public override int GetHashCode()
        {
            if (_hashCode.TryGetValue(GetType(), out int hashcode) == false)
            {
                hashcode = GetType().FullName.GetHashCode();
                _hashCode.Add(GetType(), hashcode);
            }

            return hashcode;
        }
    }

    public class CoapStringOption : CoapOption
    {
        protected internal CoapStringOption(int optionNumber, int minLength = 0, int maxLength = 0, bool isRepeatable = false, object defaultValue = null)
            : base(optionNumber, minLength, maxLength, isRepeatable, OptionType.String, defaultValue)
        { }

        public string Value { get => ValueString; set => ValueString = value; }
    }

    public class CoapOpaqueOption : CoapOption
    {
        protected internal CoapOpaqueOption(int optionNumber, int minLength = 0, int maxLength = 0, bool isRepeatable = false, object defaultValue = null)
            : base(optionNumber, minLength, maxLength, isRepeatable, OptionType.Opaque, defaultValue)
        { }

        public byte[] Value { get => ValueOpaque; set => ValueOpaque = value; }
    }

    public class CoapUintOption : CoapOption
    {
        protected internal CoapUintOption(int optionNumber, int minLength = 0, int maxLength = 0, bool isRepeatable = false, object defaultValue = null)
            : base(optionNumber, minLength, maxLength, isRepeatable: isRepeatable, type: OptionType.UInt, defaultValue: defaultValue)
        { }

        public uint Value { get => ValueUInt; set => ValueUInt = value; }

    }

    public class CoapEmptyOption : CoapOption
    {
        protected internal CoapEmptyOption(int optionNumber, bool isRepeatable = false)
            : base(optionNumber, isRepeatable: isRepeatable, type: OptionType.Empty)
        { }
    }
}
