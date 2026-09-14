using Nine.Screens;

namespace OpenSolarMax.Game.Screens;

public class AsyncScreen<T>(IScreenFactory screenFactory, Task<object?> contextTask) : ITaskLike<T>
    where T : class, IScreen
{
    public AggregateException? Exception => contextTask.Exception;
    public bool IsCompleted => contextTask.IsCompleted;
    public bool IsCanceled => contextTask.IsCanceled;
    public bool IsCompletedSuccessfully => contextTask.IsCompletedSuccessfully;
    public bool IsFaulted => contextTask.IsFaulted;
    public TaskStatus Status => contextTask.Status;

    private T? _screen = null;

    public T Result
    {
        get
        {
            if (_screen is not null)
                return _screen;

            var ctx = contextTask.Result;
            _screen = (T)screenFactory.CreateScreen(typeof(T), ctx);
            return _screen;
        }
    }
}
