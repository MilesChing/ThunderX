using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Utils;

namespace TX.Core.Models.Sources
{
    /// <summary>
    /// Abstract downloading source with an URI. The URI might contains one or more tasks to be downloaded.
    /// </summary>
    public abstract class AbstractSource
    {
        /// <summary>
        /// Initialize an AbstractSource with a given URI.
        /// </summary>
        /// <param name="uri">URI of the source, unchanged once set.</param>
        public AbstractSource(Uri uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// URI of the source. 
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Construct a source for given URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The source. Returns null if the URI is invalid.</returns>
        public static AbstractSource CreateSource(Uri uri)
        {
            var core = ((App)App.Current).Core;
            var settingEntries = new Settings();

            if (settingEntries.IsTorrentEnabled && TorrentSource.IsValid(uri))
                return new TorrentSource(uri);

            if (settingEntries.IsTorrentEnabled && MagnetSource.IsValid(uri))
                return new MagnetSource(uri, 
                    core.TorrentEngine, 
                    core.CustomAnnounceURLs);

            if (HttpSource.IsValid(uri))
            {
                if (settingEntries.IsYouTubeURLEnabled &&
                    uri.Host.Equals("www.youtube.com") &&
                    uri.LocalPath.Equals("/watch"))
                    return new YouTubeSource(uri);
                return new HttpSource(uri);
            }

            if (settingEntries.IsThunderURLEnabled && ThunderSource.IsValid(uri))
                return new ThunderSource(uri);

            return null;
        }
    }
}
