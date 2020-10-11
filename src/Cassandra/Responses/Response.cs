//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections.Generic;
using Cassandra.Serialization;

namespace Cassandra.Responses
{
    internal class Response
    {
        /// <summary>
        /// Big-endian binary reader of the response frame
        /// </summary>
        protected FrameReader Reader { get; }

        /// <summary>
        /// Identifier of the Cassandra trace 
        /// </summary>
        protected internal Guid? TraceId { get; }

        /// <summary>
        /// Warnings from the server which were generated by the server to go along with this response.
        /// </summary>
        public string[] Warnings { get; }

        /// <summary>
        /// Gets the incoming custom payload.
        /// </summary>
        public IDictionary<string, byte[]> CustomPayload { get; }

        internal Response(Frame frame)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            if (frame.Body == null) throw new InvalidOperationException("Response body of the received frame was null");
            if (!frame.Header.Flags.HasFlag(HeaderFlags.Compression) && frame.Header.BodyLength > frame.Body.Length - frame.Body.Position)
            {
                throw new DriverInternalError(string.Format(
                    "Response body length should be contained in stream: Expected {0} but was {1} (position {2})",
                    frame.Header.BodyLength, frame.Body.Length - frame.Body.Position, frame.Body.Position));
            }

            Reader = new FrameReader(frame.Body, frame.Serializer);

            if (frame.Header.Flags.HasFlag(HeaderFlags.Tracing))
            {
                //If a response frame has the tracing flag set, the first item in its body is the trace id
                var buffer = new byte[16];
                Reader.Read(buffer, 0, 16);
                TraceId = new Guid(TypeSerializer.GuidShuffle(buffer));
            }
            
            if (frame.Header.Flags.HasFlag(HeaderFlags.Warning))
            {
                Warnings = Reader.ReadStringList();
            }
            
            if (frame.Header.Flags.HasFlag(HeaderFlags.CustomPayload))
            {
                CustomPayload = Reader.ReadBytesMap();
            }
        }

        /// <summary>
        /// Testing purposes only
        /// </summary>
        protected Response()
        {
        }
    }
}
