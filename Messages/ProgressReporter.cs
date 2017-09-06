using System;
using System.Threading.Tasks;

namespace Messages
{
    public class ProgressReporter
    {
        Task task;
        long messageNo;

        public ProgressReporter()
        {
            task = Task.Run(async () =>
            {
                while (true)
                {
                    Console.Write($"Event handled. No: {messageNo}.");

                    Console.SetCursorPosition(0, Console.CursorTop);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        }

        public void Record(long messageNo)
        {
            this.messageNo = messageNo;
        }
    }
}