using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Pointy.Core
{
    class Scheduler
    {
        class WorkerSyncContext : SynchronizationContext
        {
            Scheduler Master;
            CancellationTokenSource CancelTokenSource;

            public WorkerSyncContext(Scheduler master)
            {
                Master = master;
                
            }

            public int Backlog
            {
                get { return Master.Work.Count + Master.Requests.Count; }
            }
            public void Run()
            {
                // Set the state for this run cycle appropriately
                CancelTokenSource = new CancellationTokenSource();

                // Build the list of queues to fetch from.  Note that the queues earlier
                // in the list will be prioritized, which is exactly what we want.  So
                // all is well!
                var queues = new BlockingCollection<Action>[] { Master.Work, Master.Requests };

                // And do the run loop
                while (Master.Running)
                {
                    // Do a blocking dequeue and allow cancellation.  Mono has a bug that requires us
                    // to specific that tiny timeout.
                    Action action = null;
					BlockingCollection<Action>.TryTakeFromAny(queues, out action, 10, CancelTokenSource.Token);

                    // Actually do the thing
                    if (action != null)
                        action();
                }
            }
            public void Stop()
            {
                CancelTokenSource.Cancel();
            }
            public override void Send(SendOrPostCallback d, object state)
            {
                // Ehhh... not really going to bother with this one, since I'll
                // never use it.
                throw new NotImplementedException();
            }
            public override void Post(SendOrPostCallback d, object state)
            {
                // Just queue it up and be on our merry way
                Master.Work.Add(delegate
                {
                    try
                    {
                        d(state);
                    }
                    catch (Exception err)
                    {
                        Master.ErrorHandler(err);
                    }
                });
            }
        }

        volatile bool Running = false;
        WorkerSyncContext[] Workers;
        BlockingCollection<Action> Work = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
        BlockingCollection<Action> Requests = new BlockingCollection<Action>(new ConcurrentQueue<Action>());

        public Action<Exception> ErrorHandler
        {
            set;
            get;
        }

        public Scheduler(int threads)
        {
            // Choose a nice, reasonable default for worker threads if the
            // specified threadcount is 0.
            if (threads == 0)
                threads = Environment.ProcessorCount * 2 + 1;

            // Prep the thread storage
            Workers = new WorkerSyncContext[threads];

            // Default error handler
            ErrorHandler = delegate(Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(err.StackTrace);
            };
        }

        public void Run()
        {
            if (Running)
                throw new InvalidOperationException("Already running");

            Running = true;

            // Boot up the threads
            for (var i = 0; i < Workers.Length; i++)
            {
                Workers[i] = new WorkerSyncContext(this);
                new Thread(new ParameterizedThreadStart((syncContext) =>
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext as WorkerSyncContext);
                    (syncContext as WorkerSyncContext).Run();
                })).Start(Workers[i]);
            }
        }
        public void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("Not running");

            Running = false;
            for (var i = 0; i < Workers.Length; i++)
            {
                Workers[i].Stop();
                Workers[i] = null;
            }   
        }
        public void Queue(Action request)
        {
            Requests.Add(request);
        }
    }
}
