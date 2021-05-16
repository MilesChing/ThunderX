using EnsureThat;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Collections
{
    /// <summary>
    /// CollectiondBind bind modifications or changes from one collection 
    /// to another. It resyncs and matches each object between both collections
    /// once enabled, to ensure that collection source and destination are same.
    /// Then it listens to each collection change of source and apply the same
    /// change to destination.
    /// </summary>
    /// <typeparam name="S">The type of items in the source collection.</typeparam>
    /// <typeparam name="T">The type of items in the destination collection.</typeparam>
    public class CollectionBind<S, T>
    {
        /// <summary>
        /// Construct a CollectionBind from a source collection which 
        /// should be an ISyncableCollection to a destination collection.
        /// It uses the caster when new object is required to be created
        /// and uses the comparer to match existence objects in two 
        /// collections when resyncing.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <param name="destination">The destination collection.</param>
        /// <param name="caster">The caster creates an object of type 
        /// <typeparamref name="T"/> from an object of type <typeparamref name="S"/>.
        /// </param>
        /// <param name="comparer">The comparer compares an object of type 
        /// <typeparamref name="T"/> with an object of type <typeparamref name="S"/> 
        /// and returns true if they are corresponding.</param>
        public CollectionBind(
            ISyncableEnumerable<S> source, 
            ICollection<T> destination,
            Func<S, T> caster,
            Func<S, T, bool> comparer)
        {
            Ensure.That(source, nameof(source)).IsNotNull();
            Ensure.That(destination, nameof(destination)).IsNotNull();
            Ensure.That(caster, nameof(caster)).IsNotNull();
            Ensure.That(comparer, nameof(comparer)).IsNotNull();
            Source = source;
            Destination = destination;
            Caster = caster;
            Comparer = comparer;
        }

        /// <summary>
        /// Controls whether the bind is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _is_enbaled;
            set
            {
                if (value != _is_enbaled)
                {
                    _is_enbaled = value;
                    if (_is_enbaled)
                    {
                        Source.CollectionChanged += SourceCollectionChanged;
                        Resync();
                    }
                    else Source.CollectionChanged -= SourceCollectionChanged;
                }
            }
        }
        private bool _is_enbaled = false;

        /// <summary>
        /// The source collection.
        /// </summary>
        public ISyncableEnumerable<S> Source { get; private set; }

        /// <summary>
        /// The destination collection.
        /// </summary>
        public ICollection<T> Destination { get; private set; }

        /// <summary>
        /// The caster creates an object of type <typeparamref name="T"/> 
        /// from an object of type <typeparamref name="S"/>.
        /// </summary>
        public Func<S, T> Caster { get; private set; }

        /// <summary>
        /// The comparer compares an object of type <typeparamref name="T"/> 
        /// with an object of type <typeparamref name="S"/> 
        /// and returns true if they are corresponding.
        /// </summary>
        public Func<S, T, bool> Comparer { get; private set; }
    
        private void Resync()
        {
            List<T> toBeDeleted = new List<T>();
            List<S> toBeCreated = new List<S>();
            foreach (var s in Source)
            {
                int createdCount = Math.Max(0,
                    Source.Count(item => item.Equals(s)) -
                    Destination.Where(t => Comparer(s, t)).Count());
                for (int i = 0; i < createdCount; ++i)
                    toBeCreated.Add(s);
            }

            foreach (var t in Destination)
                if (!Source.Any(s => Comparer(s, t)))
                    toBeDeleted.Add(t);

            foreach (var deleted in toBeDeleted)
                Destination.Remove(deleted);
            foreach (var created in toBeCreated)
                Destination.Add(Caster(created));
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Destination.Clear();
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                        foreach (S toBeRemovedS in e.OldItems)
                        {
                            var toBeRemovedD = Destination.First(
                                t => Comparer(toBeRemovedS, t));
                            Destination.Remove(toBeRemovedD);
                        }
                    if (e.NewItems != null)
                        foreach (S toBeCreatedS in e.NewItems)
                            Destination.Add(Caster(toBeCreatedS));
                    break;
            }
        }
    }
}
