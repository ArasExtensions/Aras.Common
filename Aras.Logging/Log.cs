/*  
  Copyright 2017 Processwall Limited

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
 
  Company: Processwall Limited
  Address: The Winnowing House, Mill Lane, Askham Richard, York, YO23 3NW, United Kingdom
  Tel:     +44 113 815 3440
  Web:     http://www.processwall.com
  Email:   support@processwall.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Aras.Logging
{
    public enum Levels { Error = 0, Warning = 1, Information = 2, Debug = 3 };

    public abstract class Log : IDisposable
    {
        const int delay = 100;

        public Levels Level { get; set; }

        public void Add(Levels Level, String Text)
        {
            if (Level <= this.Level)
            {
                this.MessageQueue.Enqueue(new Message(Level, Text));
            }
        }

        protected static String DateStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected static String ConvertLevel(Levels Level)
        {
            switch (Level)
            {
                case Levels.Error:
                    return "ERR: ";
                case Levels.Warning:
                    return "WAR: ";
                case Levels.Information:
                    return "INF: ";
                default:
                    return "DEB: ";
            }
        }

        protected abstract void WriteMessage(Message Message);

        private Thread Worker;

        private ConcurrentQueue<Message> MessageQueue;

        private volatile Boolean Process;
       
        private void ProcessMessageQueue()
        {
            Message message = null;

            while(this.Process)
            {
                while(this.MessageQueue.TryDequeue(out message))
                {
                    this.WriteMessage(message);
                }

                Thread.Sleep(delay);
            }

            // Clear remaining queue
            while (this.MessageQueue.TryDequeue(out message))
            {
                this.WriteMessage(message);
            }
        }

        public virtual void Close()
        {
            if (this.Worker != null)
            {
                this.Process = false;
                this.Worker.Join();
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        public Log()
        {
            this.Process = true;
            this.Level = Levels.Information;
            this.MessageQueue = new ConcurrentQueue<Message>();
            this.Worker = new Thread(this.ProcessMessageQueue);
            this.Worker.IsBackground = true;
            this.Worker.Start();
        }
    }
}
