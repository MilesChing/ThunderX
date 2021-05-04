using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Controls
{
    public class StartUpActionManager
    {
        public void Register(Action action) =>
            actionQueue.Enqueue(action);

        public void Do()
        {
            while (actionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    D($"Startup action executing failed: {e.Message}");
                }
            }

            D("Startup actions executed");
        }

        protected void D(string message) => Debug.WriteLine($"[StartUpActionManager] {message}");

        private readonly Queue<Action> actionQueue = new Queue<Action>();
    }
}
