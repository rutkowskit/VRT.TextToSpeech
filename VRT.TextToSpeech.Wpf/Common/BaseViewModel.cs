using System.Reactive.Disposables;

namespace VRT.TextToSpeech.Wpf.Common;

public abstract class BaseViewModel : ReactiveObject, IReactiveObject, IDisposable
{
    private bool _disposedValue;
    protected CompositeDisposable Disposables { get; }

    protected BaseViewModel()
    {
        Disposables = new CompositeDisposable();
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                Disposables.Dispose();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method            
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
