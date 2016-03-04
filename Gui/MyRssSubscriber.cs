using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RssReaderFs;

namespace Gui
{
    public class MyRssSubscriber
        : Domain.RssSubscriber
    {
        Action<Domain.RssItem[]> f_;

        public MyRssSubscriber(Action<Domain.RssItem[]> f)
        {
            f_ = f;
        }

        public void OnNewItems(Domain.RssItem[] rssItems)
        {
            f_(rssItems);
        }
    }
}
