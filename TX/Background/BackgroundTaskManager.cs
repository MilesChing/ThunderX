using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace TX.Background
{
    /// <summary>
    /// BackgroundTaskManager manages the activation, registration 
    /// and unregistration of background tasks of Thunder X.
    /// </summary>
    static class BackgroundTaskManager
    {
        /// <summary>
        /// Register background tasks.
        /// </summary>
        public static void RegisterTasks()
        {
            var targetTasks = tasks.Where(
                task => !BackgroundTaskRegistration.AllTasks.Values.Any(
                    registration => string.Equals(registration.Name, task.Name)
                )
            );

            foreach (var task in targetTasks)
                task.Build().Register();
        }

        /// <summary>
        /// Unregister background tasks.
        /// </summary>
        public static void UnregisterTasks()
        {
            var taskNames = tasks.Select(task => task.Name);
            var registrations = BackgroundTaskRegistration.AllTasks.Values.Where(
                registration => taskNames.Contains(registration.Name));
            foreach (var registration in registrations)
                registration.Unregister(false);
        }

        /// <summary>
        /// Activate registered background task whose name is
        /// equals to instance.Task.Name
        /// </summary>
        /// <param name="instance">Input background task instance.</param>
        public static void ActivateTask(IBackgroundTaskInstance instance)
        {
            var target = tasks.FirstOrDefault(
                task => string.Equals(task.Name, instance.Task.Name)
            );

            target?.Run(instance);
        }

        private static readonly AbstractBackgroundTask[] tasks =
            new AbstractBackgroundTask[]
            {
                new CoreBackgroundTask(),
            };
    }
}
