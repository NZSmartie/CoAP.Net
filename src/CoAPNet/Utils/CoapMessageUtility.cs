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

namespace CoAPNet.Utils
{
    /// <summary>
    /// See <see cref="CoapMessage"/>
    /// </summary>
    [Obsolete]
    public static class CoapMessageUtility
    {
        /// <summary>
        /// Use <see cref="CoapMessage.Create(CoapMessageCode, string, CoapMessageType)"/> instead
        /// </summary>
        [Obsolete]
        public static CoapMessage CreateMessage(CoapMessageCode code, string message, CoapMessageType type = CoapMessageType.Confirmable)
            => CoapMessage.Create(code, message, type);

        /// <summary>
        /// Use <see cref="CoapMessage.FromException(Exception)"/> instead
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        [Obsolete]
        public static CoapMessage FromException(Exception exception)
            => CoapMessage.FromException(exception);
    }
}