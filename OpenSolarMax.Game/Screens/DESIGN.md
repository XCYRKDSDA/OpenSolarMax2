- ViewModel 通过 Event 发出自己想要切换“界面”的意图。在 ViewModel 层面来看，切换界面指的是切换 ViewModel。至于具体用哪个 View 来绘制，不是 ViewModel 该处理的事情。

- View 接收到 ViewModel 切换界面的请求，便自行选择适用于新 ViewModel 的 View，然后进行切换。
