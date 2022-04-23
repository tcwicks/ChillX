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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Core.Structures
{
    /// <summary>
    /// Convenience wrapper around ArrayPool<byte>
    /// Implements IDisposable for returning rented buffers back to the pool
    /// </summary>
    public class RentedBuffer : IDisposable
    {
        public RentedBuffer(int size)
        {
            buffer = ArrayPool<byte>.Shared.Rent(size);
        }
        public byte[] buffer { get; private set; }
        private bool m_isDisposed = false;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_isDisposed = true;
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    //Return(buffer);
                    buffer = null;
                }
                GC.SuppressFinalize(this);
            }
        }

        public void ReturnBuffer()
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = null;
            }
        }
    }

}
