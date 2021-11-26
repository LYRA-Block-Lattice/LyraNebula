using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UserLibrary.Data
{
    public class RxComponentBase : ComponentBase, IDisposable
    {
        Subject<bool> _disposed = new Subject<bool>();
        ObservableParameters _observableParameters = new ObservableParameters();

        public IObservable<bool> Disposed => _disposed.AsObservable();

        public IObservable<T> ObserveParameter<T>(Expression<Func<T>> parameterSelector)
        {
            MemberInfo parameterInfo = ((MemberExpression)parameterSelector.Body).Member;
            if (parameterInfo.GetCustomAttribute<ParameterAttribute>() == null)
            {
                throw new ArgumentException("Member is not a parameter. It must be public property annotated with ParameterAttribute", nameof(parameterInfo));
            }
            return this._observableParameters.Observe<T>(parameterInfo.Name);
        }
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var paramsDict = parameters.ToDictionary();
            await base.SetParametersAsync(parameters);
            _observableParameters.OnNext(paramsDict);
        }

        public void Dispose()
        {
            _disposed.OnNext(true);
        }
    }

    /// <summary>
    /// Turns component parameters properties into observable.
    /// You can observe values on specific property using <see cref="Observe"/>("MyProperty");
    /// Values are emmited when <see cref="OnNext"/>() is called which typically happends in SetParametersAsync()
    /// </summary>
    /// <example>
    /// </example>
    public class ObservableParameters
    {
        private ConcurrentDictionary<string, Subject<object>>? _paramsObservables;

        public IObservable<TValue> Observe<TValue>(string parameterName)
        {
            if (_paramsObservables == null)
            {
                _paramsObservables = new ConcurrentDictionary<string, Subject<object>>();
            }

            IObservable<object> observable = _paramsObservables.GetOrAdd(parameterName, (_) => new Subject<object>());
            return observable.Cast<TValue>();
        }

        /// <summary>
        /// This is supposed to be called from SetParametersAsync();
        /// </summary>
        public void OnNext(IReadOnlyDictionary<string, object> parameters)
        {
            if (_paramsObservables != null)
            {
                foreach (var param in parameters)
                {
                    if (_paramsObservables.TryGetValue(param.Key, out var observable))
                    {
                        observable.OnNext(param.Value);
                    }
                }
            }
        }
    }
}
