/*==============================================================================
    文件名称：Console.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：控制台输出工具
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;

namespace Wlniao.Runtime
{
    /// <summary>
    /// 
    /// </summary>
    public class Console
    {
        private int outType = 0;    //输出模式：1,行内输出 2,新行输出
        private int startTop = 0;   //输出行位置
        private int startLeft = 0;  //输出列位置
        private double TotalCount = 0;  //进度总值
        private int loading = 0;
        private DateTime endTime = DateTime.Now;
        /// <summary>
        /// 
        /// </summary>
        public Console()
        {
            endTime = DateTime.Now.AddSeconds(120);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        public Console(DateTime timeout)
        {
            endTime = timeout;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public Console(double TotalCount)
        {
            this.TotalCount = TotalCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public Console(int TotalCount)
        {
            this.TotalCount = TotalCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public Console(long TotalCount)
        {
            this.TotalCount = TotalCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TotalCount"></param>
        public Console(float TotalCount)
        {
            this.TotalCount = TotalCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FinishedCount"></param>
        public void Write(double FinishedCount)
        {
            if (outType == 0)
            {
                outType = 1;
                startTop = System.Console.CursorTop;
                startLeft = System.Console.CursorLeft;
            }
            System.Console.SetCursorPosition(startLeft, startTop);
            if (FinishedCount >= TotalCount)
            {
                System.Console.CursorVisible = true;
                System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;
                System.Console.Write("Finished.");
                System.Console.ForegroundColor = System.ConsoleColor.Gray;
            }
            else
            {
                var finishedPercentage = System.Convert.ToDecimal(FinishedCount) / System.Convert.ToDecimal(TotalCount);
                System.Console.CursorVisible = false;
                System.Console.ForegroundColor = System.ConsoleColor.DarkYellow;
                System.Console.Write((finishedPercentage * 100).ToString("f0") + "%");
                System.Console.ForegroundColor = System.ConsoleColor.Gray;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FinishedCount"></param>
        public void WriteLine(double FinishedCount)
        {
            if (outType == 0)
            {
                outType = 2;
                startTop = System.Console.CursorTop;
                startLeft = 0;
            }
            System.Console.SetCursorPosition(startLeft, startTop);
            if (FinishedCount >= TotalCount)
            {
                System.Console.CursorVisible = true;
                System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;
                System.Console.WriteLine("Finished.");
                System.Console.ForegroundColor = System.ConsoleColor.Gray;
            }
            else
            {
                var finishedPercentage = System.Convert.ToDecimal(FinishedCount) / System.Convert.ToDecimal(TotalCount);
                System.Console.CursorVisible = false;
                System.Console.ForegroundColor = System.ConsoleColor.DarkYellow;
                System.Console.WriteLine((finishedPercentage * 100).ToString("f0") + "%");
                System.Console.ForegroundColor = System.ConsoleColor.Gray;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void WriteFaild(string message = "failed!")
        {
            System.Console.SetCursorPosition(startLeft, startTop);
            System.Console.ForegroundColor = System.ConsoleColor.Red;
            if (outType == 2)
            {
                System.Console.WriteLine(message);
            }
            else
            {
                System.Console.Write(message);
            }
            System.Console.ForegroundColor = System.ConsoleColor.Gray;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tips"></param>
        /// <param name="newline"></param>
        public void Loading(string tips = "", bool newline = true)
        {
            if (newline)
            {
                outType = 2;
                if (!string.IsNullOrEmpty(tips))
                {
                    System.Console.WriteLine(tips);
                }
            }
            else
            {
                outType = 1;
                if (!string.IsNullOrEmpty(tips))
                {
                    System.Console.Write(tips);
                }
            }
            startTop = System.Console.CursorTop;
            startLeft = System.Console.CursorLeft;
            new System.Threading.Thread(new System.Threading.ThreadStart(ConsoleLoading)).Start();
        }
        /// <summary>
        /// 
        /// </summary>
        private void ConsoleLoading()
        {
            while (DateTime.Now < endTime)
            {
                System.Console.CursorVisible = false;
                System.Console.SetCursorPosition(startLeft, startTop);
                switch (loading % 4)
                {
                    case 0: System.Console.Write("|"); break;
                    case 1: System.Console.Write("/"); break;
                    case 2: System.Console.Write("-"); break;
                    case 3: System.Console.Write("\\"); break;
                }
                loading++;
                System.Threading.Thread.Sleep(50);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void LoadingFinish()
        {
            endTime = DateTime.MinValue;
            System.Console.SetCursorPosition(startLeft, startTop);
            System.Console.CursorVisible = true;
            System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;
            System.Console.Write("Finished.");
            System.Console.ForegroundColor = System.ConsoleColor.Gray;
            if (outType == 1)
            {
                System.Console.WriteLine();
            }
        }
    }
}