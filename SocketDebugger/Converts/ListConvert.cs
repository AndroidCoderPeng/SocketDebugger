using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SocketDebugger.Converts
{
    public static class ListConvert<T>
    {
        public static ObservableCollection<T> ToObservableCollection(List<T> list)
        {
            var collection = new ObservableCollection<T>();
            foreach (var model in list)
            {
                collection.Add(model);
            }

            return collection;
        }
    }
}