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

namespace ChillX.Core.Structures
{
    public class RunningAverage
    {
        public int WindowSize { get; private set; } = 10;
        private double m_WindowSizeDouble = 10d;
        private Queue<double> ResultList { get; } = new Queue<double>();
        public RunningAverage(int _windowSize)
        {
            WindowSize = Math.Max(_windowSize, 10);
            m_WindowSizeDouble = WindowSize;
        }
        public void AddResult(int value)
        {
            ResultList.Enqueue(value);
            if (ResultList.Count > WindowSize)
            {
                ResultList.Dequeue();
            }
            else
            {
                m_WindowSizeDouble = (double)ResultList.Count;
            }
        }
        public void AddResult(float value)
        {
            ResultList.Enqueue(value);
            while (ResultList.Count > WindowSize)
            {
                ResultList.Dequeue();
            }
        }
        public void AddResult(long value)
        {
            ResultList.Enqueue(value);
            while (ResultList.Count > WindowSize)
            {
                ResultList.Dequeue();
            }
        }
        public void AddResult(double value)
        {
            ResultList.Enqueue(value);
            while (ResultList.Count > WindowSize)
            {
                ResultList.Dequeue();
            }
        }
        public double ComputeAverage()
        {
            double Total = 0d;
            foreach(double value in ResultList)
            {
                Total += value;
            }
            return Total / m_WindowSizeDouble;
        }
        public int ComputeAverageAsInt()
        {
            double Total = 0d;
            foreach (double value in ResultList)
            {
                Total += value;
            }
            Total = Total / m_WindowSizeDouble;
            return Convert.ToInt32(Total);
        }
        public long ComputeAverageAsLong()
        {
            double Total = 0d;
            foreach (double value in ResultList)
            {
                Total += value;
            }
            Total = Total / m_WindowSizeDouble;
            return Convert.ToInt64(Total);
        }
    }
}
