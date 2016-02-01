﻿// TailCoordinatorActor.cs
using System;
using System.Diagnostics;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message types
        /// <summary>
        /// Start tailing the file at user-specified path.
        /// </summary>
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }

            public string FilePath { get; private set; }

            public IActorRef ReporterActor { get; private set; }
        }

        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; private set; }
        }

        #endregion

        private int _ChildCounter = 0;

        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;

                // here we are creating our first parent/child relationship!
                // the TailActor instance created here is a child
                // of this instance of TailCoordinatorActor
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)), "TailActor"+ _ChildCounter++);
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                5, // maxNumberOfRetries
                TimeSpan.FromSeconds(30), // withinTimeRange
                x => // localOnlyDecider
                    {
                        //Maybe we consider ArithmeticException to not be application critical
                        //so we just ignore the error and keep going.
                        if (x is ArithmeticException) return Directive.Resume;

                        //Error that we cannot recover from, stop the failing actor
                        else if (x is NotSupportedException) return Directive.Stop;

                        //In all other cases, just restart the failing actor
                        else return Directive.Restart;
                    });
        }
    }
}