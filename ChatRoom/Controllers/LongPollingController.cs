using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ChatRoom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LongPollingController : ControllerBase
    {
        private static List<Msg> Messages = new List<Msg>();
        private static Dictionary<string, int> Subscribers = new Dictionary<string, int>();
        private static ManualResetEvent mre = new ManualResetEvent(false);

        // POST api/longPolling/subscribe
        [HttpPut("subscribe/{id}")]
        public void Subscribe(string id)
        {
            if (!Subscribers.ContainsKey(id))
            {
                Subscribers.Add(id, 0);
            }
        }

        // POST api/longPolling
        [HttpPost]
        public void Post([FromBody]Msg msg)
        {
            Messages.Add(msg);
            mre.Set();
        }

        // GET api/longPolling/[id]
        [HttpGet("{id}")]
        public ActionResult<IEnumerable<Msg>> Get(string id)
        {
            if (!Subscribers.ContainsKey(id))
            {
                return null;
            }
            if (Subscribers[id] >= Messages.Count)
            {
                mre.Reset();
                mre.WaitOne();
            }
            
            var index = Subscribers[id];
            Subscribers[id] = Messages.Count;
            return Messages.GetRange(index, Messages.Count - index);
        }
    }
}
