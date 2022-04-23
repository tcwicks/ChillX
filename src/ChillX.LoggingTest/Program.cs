/*
ChillX Framework Test Application
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

using ChillX.Logging;
using System.Diagnostics;
using System.Text;

namespace ChillX.Logging.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TestLogger();
        }

        private static void TestLogger()
        {
            ChillX.Logging.Handlers.LogHandlerFile FileLogHandler_RolloverByCount;
            FileLogHandler_RolloverByCount = new ChillX.Logging.Handlers.LogHandlerFile(@"C:\Temp\LogTestByCount",
                _fileNamePrepend: @"ByCount_", _fileExtension: @".txt", _fileRollOverPerEntries: 10000, _fileRollOverDays: 99, _fileRollOverHours: 1, _fileRollOverMinutes: 1);

            ChillX.Logging.Handlers.LogHandlerFile FileLogHandler_RolloverByTime;
            FileLogHandler_RolloverByTime = new ChillX.Logging.Handlers.LogHandlerFile(@"C:\Temp\LogTestByTime",
                _fileNamePrepend: @"ByCount_", _fileExtension: @".txt", _fileRollOverPerEntries: int.MaxValue, _fileRollOverDays: 0, _fileRollOverHours: 0, _fileRollOverMinutes: 1);

            Logger.BatchSize = 100;
            Logger.RegisterHandler(@"RolloverByCount", FileLogHandler_RolloverByCount);
            Logger.RegisterHandler(@"RolloverByTime", FileLogHandler_RolloverByTime);

            ////Example usage
            //Logger.LogMessage(LogSeverity.info, @"Some message text");
            //Logger.LogMessage(LogSeverity.info, @"Some message text", _ex: new Exception(@""), DateTime.Now);
            //@"This is a log message".Log(LogSeverity.info);
            //LogSeverity.debug.Log(@"This is another log entry");
            //LogSeverity.error.Log(@"Lets log a post dated exception", new Exception(@"Some message here"), DateTime.Now.AddHours(8));
            //try
            //{
            //    throw new InvalidOperationException(@"test exception");
            //}
            //catch (Exception ex)
            //{
            //    ex.Log(@"This is a test exception", LogSeverity.debug);
            //    //Supports chaining
            //    throw ex.Log(@"This is a test exception", LogSeverity.debug).MessageException;
            //}


            Stopwatch SW = new Stopwatch();
            SW.Start();
            //Burst test
            Console.WriteLine(@"Running Burst");
            for (int I = 0; I < 1000000; I++)
            {
                switch (Convert.ToInt32(Math.Floor(6 * random.NextDouble())))
                {
                    case 0:
                        RandomText().Log(LogSeverity.debug).MessageText.Log(LogSeverity.debug, new Exception(RandomText()));
                        break;
                    case 1:
                        RandomText().Log(LogSeverity.warning).MessageText.Log(LogSeverity.warning, new Exception(RandomText()));
                        break;
                    case 2:
                        RandomText().Log(LogSeverity.error).MessageText.Log(LogSeverity.error, new Exception(RandomText()));
                        break;
                    case 3:
                        RandomText().Log(LogSeverity.unhandled).MessageText.Log(LogSeverity.unhandled, new Exception(RandomText()));
                        break;
                    case 4:
                        RandomText().Log(LogSeverity.fatal).MessageText.Log(LogSeverity.fatal, new Exception(RandomText()));
                        break;
                    default:
                        new Exception(RandomText()).Log(RandomText(), LogSeverity.info).Severity.Log(RandomText()).MessageText.Log(LogSeverity.info, new Exception(RandomText()));
                        break;
                }
                if (SW.ElapsedMilliseconds > 1000)
                {
                    SW.Restart();
                    Console.WriteLine(@"Pending Log Item Count: {0}", Logger.Instance.PendingLogItemCount());
                }
            }


            // Sensible rate test
            Console.WriteLine(@"Running sensible rate test");
            int Counter;
            Counter = 0;
            SW.Restart();
            for (int I = 0; I < 1000000; I++)
            {
                Counter++;
                if (Counter >= 10)
                {
                    System.Threading.Thread.Sleep(1);
                    Counter = 0;
                }
                switch (Convert.ToInt32(Math.Floor(6 * random.NextDouble())))
                {
                    case 0:
                        RandomText().Log(LogSeverity.debug).MessageText.Log(LogSeverity.debug, new Exception(RandomText()));
                        break;
                    case 1:
                        RandomText().Log(LogSeverity.warning).MessageText.Log(LogSeverity.warning, new Exception(RandomText()));
                        break;
                    case 2:
                        RandomText().Log(LogSeverity.error).MessageText.Log(LogSeverity.error, new Exception(RandomText()));
                        break;
                    case 3:
                        RandomText().Log(LogSeverity.unhandled).MessageText.Log(LogSeverity.unhandled, new Exception(RandomText()));
                        break;
                    case 4:
                        RandomText().Log(LogSeverity.fatal).MessageText.Log(LogSeverity.fatal, new Exception(RandomText()));
                        break;
                    default:
                        new Exception(RandomText()).Log(RandomText(), LogSeverity.info).Severity.Log(RandomText()).MessageText.Log(LogSeverity.info, new Exception(RandomText()));
                        break;
                }
                if (SW.ElapsedMilliseconds > 1000)
                {
                    SW.Restart();
                    Console.WriteLine(@"Pending Log Item Count: {0}", Logger.Instance.PendingLogItemCount());
                }
            }


            Logger.ShutDown();
        }

        private static Random random = new Random();

        private static string RandomText(int length = 25)
        {
            // creating a StringBuilder object()
            StringBuilder sb = new StringBuilder();

            char letter;

            for (int i = 0; i < length; i++)
            {
                double flt = random.NextDouble();
                int shift = Convert.ToInt32(Math.Floor(25 * flt));
                if (random.NextDouble() > 0.5d)
                {
                    letter = Convert.ToChar(shift + 65);
                }
                else
                {
                    letter = Convert.ToChar(shift + 97);

                }
                sb.Append(letter);
            }
            return sb.ToString();
        }
    }
}