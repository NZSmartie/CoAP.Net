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
using System.Text;
using CoAPNet.Options;

namespace CoAPNet.Utils
{
    public static class CoapMessageUtility
    {
        public static CoapMessage CreateMessage(CoapMessageCode code, string message, CoapMessageType type = CoapMessageType.Confirmable)
        {
            return new CoapMessage
            {
                Code = code,
                Type = type,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes(message)
            };
        }

        public static CoapMessage FromException(Exception exception)
        {
            var result = new CoapMessage
            {
                Type = CoapMessageType.Reset,
                Code = CoapMessageCode.InternalServerError,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes(exception.Message)
            };

            switch (exception)
            {
                case CoapOptionException _:
                    result.Code = CoapMessageCode.BadOption;
                    break;
                case NotImplementedException _:
                    result.Code = CoapMessageCode.NotImplemented;
                    break;
            }
            return result;
        }
    }
}