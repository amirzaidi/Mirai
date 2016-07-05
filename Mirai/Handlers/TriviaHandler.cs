using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirai.Handlers
{
    class TriviaHandler : IHandler
    {
        private FeedContext Feed;

        public TriviaHandler(FeedContext Feed)
        {
            this.Feed = Feed;
        }

        public async Task Tick()
        {

        }

        public async Task Save()
        {
            
        }
    }
}
