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
using System.Collections.Generic;
using System.Text;

namespace ChillX.Serialization
{
    public abstract class SerializerBase
    {
        protected const int ByteSizeHeader = sizeof(int) + sizeof(bool) + sizeof(UInt16) + sizeof(UInt16);
        protected const int ByteSizeHeaderBlockAttribute = sizeof(UInt16);
        protected const int ByteSizeHeaderBlockSize = sizeof(int);
        protected const int ByteSizeHeaderBlockFlag = sizeof(bool);

        protected readonly byte DataTrueFlag = BitConverter.GetBytes(true)[0];
        protected readonly byte DataFalseFlag = BitConverter.GetBytes(false)[0];

    }
}
