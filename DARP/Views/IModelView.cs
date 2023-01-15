using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Views
{
    public interface IModelView
    {
        public object GetModelObj();
    }

    public interface IModelView<T> : IModelView
    {
        public object GetModelObject()
        {
            return GetModel();
        }

        public T GetModel();
    }
}
