# ChatRoom
[Course Assignment] SignalR、轮询、长轮询实现的简易聊天室 Simple Chat Room(SingalR、Polling、Long Polling)

[TOC]

## 项目基础：创建服务端项目

在 VS for Mac 中新建解决方案 "ChatRoom" ，选择`.NET Core应用 API`。



## WebSocket(SignalR)

#### 1. 创建 SignalR 中心

在项目中创建 Hubs 文件夹。在 Hubs 文件夹中，使用以下代码创建 ChatHub.cs 文件。

```c#
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatRoom.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
```



#### 2. 配置 SignalR

配置 SignalR 服务器。在 Startup.cs 文件中添加代码。

添加命名空间：

```C#
using ChatRoom.Hubs;
```

在方法 `public void ConfigureServices(IServiceCollection services)` 中添加代码：

```C#
services.AddSignalR();
```

在方法 `public void Configure(IApplicationBuilder app, IHostingEnvironment env)` 中添加代码：

```C#
						app.UseSignalR(routes =>
            {
                routes.MapHub<ChatHub>("/chatHub");
            });
```



#### 3. 安装 SignalR 客户端包

在终端中输入命令

```sh
npm init -y
npm install @aspnet/signalr
```

需要在使用 SignalR 的前端文件中引入 `signalr.js` ，把该文件复制到当前路径

```shell
cp node_modules/\@aspnet/signalr/dist/browser/signalr.js .
```

然后其实其他文件都可以删掉了，只保留 `signalr.js` 就可以。



#### 4. 编写客户端

只写一个最简单的 `signalr.html` 文件。使用了 [Vue.js](https://cn.vuejs.org/index.html) 和 [Element-UI](https://element.eleme.cn/#/zh-CN) 组件库。

```html
<!DOCTYPE html>
<html>
  <head>
    <meta charset="UTF-8" />
    <!-- import Element-ui CSS -->
    <link
      rel="stylesheet"
      href="https://unpkg.com/element-ui/lib/theme-chalk/index.css"
    />
  </head>
  
  <body style="background-color: whitesmoke">
    <div id="app" style="margin: 5% 0 0 30%;">
      <div>
        用户名：<br />
        <el-input
          placeholder="请输入昵称"
          v-model="user"
          clearable
          id="userInput"
          style="width: 30%"
        >
        </el-input>
        <br />
        <br />
        信息内容：<br />
        <el-input
          type="textarea"
          autosize
          placeholder="请输入内容"
          v-model="msg"
          :autosize="{ minRows: 2}"
          clearable
          id="messageInput"
          style="width: 50%"
        >
        </el-input>
        <el-button type="primary" plain id="sendButton" value="Send Message"
          >发送</el-button
        >
      </div>
      <div>
        <br />
        <p>消息记录：</p>
        <ul id="messagesList"></ul>
      </div>
    </div>
  </body>
  
  <!-- import Vue before Element -->
  <script src="https://unpkg.com/vue/dist/vue.js"></script>
  <!-- import Element-ui JavaScript -->
  <script src="https://unpkg.com/element-ui/lib/index.js"></script>
  <!-- import signalr JavaScript 这里引入了 signalr.js -->
  <script src="./signalr.js"></script>
  
  <script>
    new Vue({
      el: "#app",
      data: function() {
        return { msg: "", user: "" };
      }
    });
  </script>
</html>
```



#### 5. 在客户端使用 SignalR

以下是使用 signalr 所需要的 JavaScript 代码，可以直接添加在 `signalr.html` 文件后面，也可以写在一个单独的.js文件中，再在 `signalr.html` 文件中引入。

```javascript
<script>
  "use strict";
  var connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chatHub")
    .build();

  //Disable send button until connection is established
  document.getElementById("sendButton").disabled = true;

  connection.on("ReceiveMessage", function(user, message) {
    var msg = message
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;");
    var msgEle = document.createElement("span");
    msgEle.textContent = msg;
    msgEle.style =
      "background-color: #e6e6e6; border-radius: 5rem; padding: .25rem 1rem; width: auto";
    var userEle = document.createElement("span");
    userEle.textContent = user + " :  ";
    var li = document.createElement("li");
    li.appendChild(userEle);
    li.appendChild(msgEle);
    li.style = "list-style: none; margin: 1rem 0; ";
    document.getElementById("messagesList").appendChild(li);
  });

  connection
    .start()
    .then(function() {
      document.getElementById("sendButton").disabled = false;
    })
    .catch(function(err) {
      return console.error(err.toString());
    });

  document
    .getElementById("sendButton")
    .addEventListener("click", function(event) {
      var user = document.getElementById("userInput").value;
      var message = document.getElementById("messageInput").value;
      if (user == "") {
        alert("用户名不能为空");
      } else if (message == "") {
        alert("不能发送空内容");
      } else {
        connection.invoke("SendMessage", user, message).catch(function(err) {
          return console.error(err.toString());
        });
      }
      event.preventDefault();
    });
</script>
```



#### 6. 启动与测试

启动服务端程序，用浏览器打开 `signalr.html` 前端文件，打开两个标签页进行测试。



### 遇到的问题

#### 1. 跨域访问

本地用html文件直接向服务端发送请求，origin为null，导致请求失败。

**解决方案**

在服务端中配置跨域访问：

```C#
// 在方法 public void Configure(IApplicationBuilder app, IHostingEnvironment env) 中
						app.UseCors(builder =>
            {
                builder.WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
```



#### 2. Negotiate 请求失败 (failed)

在signalr创建websocket连接时，会有一个negotiate请求("http://localhost:5000/chatHub/negotiate")，遇到问题是显示请求 failed ，连状态码都没有。我找不到代码的错误，也没有在网上找到解决方案。但是发现这一步是可以跳过的，就简单粗暴地解决了。

**解决方案**

在客户端的请求中，更改建立 signalr 连接的代码如下。

```javascript
  var connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chatHub", {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    })
    .build();
```



> 参考：
>
> [MS文档·教程：ASP.NET Core SignalR 入门](https://docs.microsoft.com/zh-cn/aspnet/core/tutorials/signalr?view=aspnetcore-2.2&tabs=visual-studio)
>
> [MS文档·ASP.NET Core SignalR JavaScript 客户端](https://docs.microsoft.com/zh-cn/aspnet/core/signalr/javascript-client?view=aspnetcore-2.2)



## 轮询

#### 1. 服务端创建控制器controller

(项目中原有的 ValuesController.cs 可以删除) 

在项目中创建 Controllers 文件夹。在 Controllers 文件夹中，使用以下代码创建 PollingController.cs 文件。

- 因为没有数据库，所以直接用静态变量来保存数据。Messages保存聊天室的所有消息记录，Subscribers保存聊天室内所有成员的阅读进度，从而在每个请求到来时，可以根据不同用户的情况返回新消息。
- 包含三个方法：
  - 订阅，每个进入聊天室的用户将被服务端记录，每个用户用唯一的id标识。
  - 发送消息，用户发出发送消息的请求，消息将被服务端保存在静态变量中。
  - 获取消息，用户轮询使用的请求，每次只返回新消息，即用户没阅读/获取过的消息。

```C#
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

		// 可以创建一个 Msg.cs 文件来放这个类，并且可以放在一个 Models 文件夹中
		// 此处为了方便就直接放在 ChatRoom.Controllers 命名空间下了
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
```



#### 2. 编写客户端

复制 `signalr.html` 文件，并重命名该副本为 `polling.html` 。上面 SignalR 部分第5步的所有 JavaScript 代码可以全部删除。

**更改发送按钮**

```javascript
				<el-button
          type="primary"
          plain
          id="sendButton"
          value="Send Message"
          @click="send_msg"
          >发送</el-button
        >
```

**更改创建 Vue 实例的代码**

原代码为：

```javascript
  <script>
    new Vue({
      el: "#app",
      data: function() {
        return { msg: "", user: "" };
      }
    });
  </script>
```

更改为：

```javascript
<script>
    new Vue({
      el: "#app",
      data: function() {
        return { msg: "", user: "", id: "", timer: 0 };
      },
      mounted() {
        this.id = Math.random()
          .toString(36)
          .substr(2);
        this.subscribe();
        var self = this;
        this.timer = setInterval(() => {
          self.polling();
        }, 500);
      },
      methods: {
        subscribe() {
          this.$http
            .put("http://localhost:5000/api/polling/subscribe/" + this.id)
            .catch(error => {
              alert("请求失败");
            });
        },
        polling() {
          this.$http
            .get("http://localhost:5000/api/polling/" + this.id)
            .then(res => {
              var data = res.body;
              for (var i = 0; i < data.length; i++) {
                this.add_msg(data[i].user, data[i].message);
              }
            });
        },
        add_msg(user, message) {
          var msg = message
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
          var msgEle = document.createElement("span");
          msgEle.textContent = msg;
          msgEle.style =
            "background-color: #e6e6e6; border-radius: 5rem; padding: .25rem 1rem; width: auto";
          var userEle = document.createElement("span");
          userEle.textContent = user + " :  ";
          var li = document.createElement("li");
          li.appendChild(userEle);
          li.appendChild(msgEle);
          li.style = "list-style: none; margin: 1rem 0; ";
          document.getElementById("messagesList").appendChild(li);
        },
        send_msg() {
          this.$http
            .post("http://localhost:5000/api/polling", {
              user: this.user,
              message: this.msg
            })
            .catch(error => {
              alert("发送失败");
            });
        }
      }
    });
</script>
```



##### 关键代码说明

```javascript
        this.timer = setInterval(() => {
          self.polling();
        }, 500);
```

设置一个定时器，每500ms执行一次 polling() 方法，在该方法中向服务端发送请求获取新消息。



## 长轮询

#### 1. 服务端创建控制器controller

在 Controllers 文件夹中，使用以下代码创建 LongPollingController.cs 文件。

- 包含三个方法：
  - 订阅，每个进入聊天室的用户将被服务端记录，每个用户用唯一的id标识。
  - 发送消息，用户发出发送消息的请求，消息将被服务端保存在静态变量中。
  - 获取消息，用户轮询使用的请求，每次只返回新消息，即用户没阅读/获取过的消息。
- 与 PollingController 的区别：
  - 增加一个 ManualResetEvent 静态对象来控制线程。
  - 当有获取新消息的请求到达，如果该用户没有新消息需要获取，该线程会被 ManualResetEvent 对象阻塞。
  - 当有新消息发送/提交，会释放所有被阻塞的线程。

```C#
using System.Collections.Generic;
using System.Threading;
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
```



#### 2. 编写客户端

复制 `polling.html` 文件，并重命名该副本为 `longpolling.html` 。

**更改创建 Vue 实例的代码**

```C#
	<script>
    new Vue({
      el: "#app",
      data: function() {
        return { msg: "", user: "", id: "" };
      },
      mounted() {
        this.id = Math.random()
          .toString(36)
          .substr(2);
        this.subscribe();
        setTimeout(this.polling, 3000);
      },
      methods: {
        subscribe() {
          this.$http
            .put("http://localhost:5000/api/longPolling/subscribe/" + this.id)
            .catch(error => {
              alert("请求失败");
            });
        },
        polling() {
          this.$http
            .get("http://localhost:5000/api/longPolling/" + this.id)
            .then(res => {
              var data = res.body;
              for (var i = 0; i < data.length; i++) {
                this.add_msg(data[i].user, data[i].message);
              }
              this.polling();
            });
        },
        add_msg(user, message) {
          var msg = message
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
          var msgEle = document.createElement("span");
          msgEle.textContent = msg;
          msgEle.style =
            "background-color: #e6e6e6; border-radius: 5rem; padding: .25rem 1rem; width: auto";
          var userEle = document.createElement("span");
          userEle.textContent = user + " :  ";
          var li = document.createElement("li");
          li.appendChild(userEle);
          li.appendChild(msgEle);
          li.style = "list-style: none; margin: 1rem 0; ";
          document.getElementById("messagesList").appendChild(li);
        },
        send_msg() {
          this.$http
            .post("http://localhost:5000/api/longPolling", {
              user: this.user,
              message: this.msg
            })
            .catch(error => {
              alert("发送失败");
            });
        }
      }
    });
  </script>
```



##### 关键代码说明

设置 3s 的等待时间，待页面加载完后，客户端自动(不需要用户操作触发)发出第一次获取消息的请求。

```javascript
setTimeout(this.polling, 3000);
```

与轮询的 polling() 方法的区别在于，在 polling() 请求成功后，要回调 polling() 方法。一定要写在 .then() 方法里，表示请求成功才执行。

```javascript
        polling() {
          this.$http
            .get("http://localhost:5000/api/longPolling/" + this.id)
            .then(res => {
              var data = res.body;
              for (var i = 0; i < data.length; i++) {
                this.add_msg(data[i].user, data[i].message);
              }
              this.polling();		// 回调 polling() 方法
            });
          // this.polling(); 不可以写在这里
        },
```



#### 其他方法 - 使用异步控制器

还可以使用异步控制器来实现长轮询，这使得服务端在向客户端回应之前可以进行其他操作。

[教程：基于长轮询的 Comet](https://blog.csdn.net/ddxkjddx/article/details/7531117)

[MS文档·Asynchronous Controller](https://docs.microsoft.com/zh-cn/previous-versions/aspnet/ee728598(v=vs.100))


