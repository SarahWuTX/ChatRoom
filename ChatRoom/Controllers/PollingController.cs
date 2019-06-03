using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace ChatRoom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollingController : ControllerBase
    {
        static private List<Msg> Messages = new List<Msg>();
        static private Dictionary<string, int> Subscribers = new Dictionary<string, int>();

        // POST api/polling/subscribe
        [HttpPut("subscribe/{id}")]
        public void Subscribe(string id)
        {
            if (!Subscribers.ContainsKey(id))
            {
                Subscribers.Add(id, 0);
            }
        }

        // POST api/polling
        [HttpPost]
        public void Post([FromBody]Msg msg)
        {
            Messages.Add(msg);
        }

        // GET api/polling/[id]
        [HttpGet("{id}")]
        public ActionResult<IEnumerable<Msg>> Get(string id)
        {
            if (Subscribers[id] == Messages.Count)
            {
                return null;
            }
            var index = Subscribers[id];
            Subscribers[id] = Messages.Count;
            return Messages.GetRange(index, Messages.Count - index);
        }
    }

    public class Msg
    {
        public string user { get; set; }
        public string message { get; set; }

        public Msg(string user, string message)
        {
            this.user = user;
            this.message = message;
        }
    }
}
