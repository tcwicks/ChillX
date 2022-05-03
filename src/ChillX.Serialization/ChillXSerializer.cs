/*
ChillX Framework Library
Copyright (C) 2022  Tikiri Chintana Wickramasingha 

Contact Details: (info at chillx dot com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using ChillX.Core.Structures;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Serialization
{
    public static class ChillXSerializer<TObject>
    {
        private static readonly TypedSerializer<TObject> m_Instance = TypedSerializer<TObject>.Create();

        public static byte[] Read(TObject target, bool headerExplicit = true)
        {
            return m_Instance.Read(target, headerExplicit);
        }

        public static byte[] Read(TObject target, byte[] buffer, bool headerExplicit = true)
        {
            return m_Instance.Read(target, buffer, headerExplicit);
        }

        public static RentedBuffer<byte> ReadToRentedBuffer(TObject target, bool headerExplicit = true)
        {
            return m_Instance.ReadToRentedBuffer(target, headerExplicit);
        }

        public static bool Write(TObject target, byte[] buffer, out int bytesConsumed)
        {
            return m_Instance.Write(target, buffer, out bytesConsumed);
        }
        public static bool Write(TObject target, byte[] buffer)
        {
            int bytesConsumed;
            return m_Instance.Write(target, buffer, out bytesConsumed);
        }

        public static bool Write(TObject target, byte[] buffer, int startIndex, out int bytesConsumed)
        {
            return m_Instance.Write(target, buffer, out bytesConsumed, startIndex);
        }
        public static bool Write(TObject target, byte[] buffer, int startIndex)
        {
            int bytesConsumed;
            return m_Instance.Write(target, buffer, out bytesConsumed, startIndex);
        }
    }
}
